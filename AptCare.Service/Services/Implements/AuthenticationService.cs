using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.OTPEnum;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.AuthenDto;
using AptCare.Service.Dtos.EmailDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class AuthenticationService : BaseService<AuthenticationService>, IAuthenticationService
    {
        private readonly IPasswordHasher<Account> _pwdHasher;
        private readonly IOtpService _otpService;
        private readonly ITokenService _tokenService;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IUserContext _providerContext;

        public AuthenticationService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, IRabbitMQService rabbitMQService, IPasswordHasher<Account> pwdHasher, IOtpService otpService, IUserContext providerContext, ITokenService tokenService, ILogger<AuthenticationService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _pwdHasher = pwdHasher;
            _otpService = otpService;
            _tokenService = tokenService;
            _rabbitMQService = rabbitMQService;
            _providerContext = providerContext;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            var accountRepo = _unitOfWork.GetRepository<Account>();
            var userRepo = _unitOfWork.GetRepository<User>();
            var existed = await accountRepo.SingleOrDefaultAsync(predicate: a => a.Username == dto.Email);
            if (existed != null) throw new AppValidationException("Email của bạn đã đăng kí");
            var existedEmail = await userRepo.SingleOrDefaultAsync(predicate: u => u.Email == dto.Email);
            if (existedEmail == null) throw new AppValidationException("Email hồ sơ của bạn không có trong hệ thống, vui lòng liên hệ ban quản lí!");
            var account = new Account
            {
                AccountId = existedEmail.UserId,
                Username = dto.Email,
                PasswordHash = string.Empty,
                EmailConfirmed = false,
                LockoutEnabled = false,
                Role = AccountRole.Resident
            };
            account.PasswordHash = _pwdHasher.HashPassword(account, dto.Password);

            await accountRepo.InsertAsync(account);
            await _unitOfWork.CommitAsync();

            var otp = await _otpService.CreateOtpAsync(account.AccountId,
                OTPType.EmailVerification,
                ttl: TimeSpan.FromMinutes(5),
                digits: 6);

            await _rabbitMQService.PublishEmailAsync(new EmailRequestDto
            {
                ToEmail = dto.Email,
                Subject = "[AptCare] Xác minh địa chỉ email",
                TemplateName = "EmailVerification",
                Replacements = new Dictionary<string, string>
                {
                    ["SystemName"] = "AptCare System",
                    ["FullName"] = existedEmail.FirstName + " " + existedEmail.LastName,
                    ["OtpCode"] = otp,
                    ["ExpiredMinutes"] = "5",
                    ["ExpireAt"] = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    ["SupportEmail"] = "support@aptcare.vn",
                    ["VerifyUrl"] = "",
                    ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                    ["Year"] = DateTime.Now.Year.ToString()
                }
            });

            return new RegisterResponseDto
            {
                AccountId = account.AccountId,
                OtpSent = true,
                Message = "Đăng ký thành công. Vui lòng kiểm tra email/SMS để nhập OTP xác minh."
            };
        }

        public async Task ResendEmailVerificationOtpAsync(int accountId)
        {
            var otp = await _otpService.CreateOtpAsync(accountId, OTPType.EmailVerification,
                ttl: TimeSpan.FromMinutes(5), digits: 6);
            var UserRepo = _unitOfWork.GetRepository<User>();
            var existedEmail = await UserRepo.SingleOrDefaultAsync(predicate: u => u.UserId == accountId, include: u => u.Include(i => i.Account));

            await _rabbitMQService.PublishEmailAsync(new EmailRequestDto
            {
                ToEmail = existedEmail.Email,
                Subject = "[AptCare] Xác minh địa chỉ email",
                TemplateName = "EmailVerification",
                Replacements = new Dictionary<string, string>
                {
                    ["FullName"] = existedEmail.FirstName + " " + existedEmail.LastName,
                    ["OtpCode"] = otp,
                    ["ExpiredMinutes"] = "5",
                    ["SystemName"] = "AptCare System",
                    ["SupportEmail"] = "support@aptcare.vn",
                    ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                    ["Year"] = DateTime.Now.Year.ToString()
                }
            });
        }

        public async Task<bool> VerifyEmailAsync(int accountId, string otp)
        {
            var ok = await _otpService.VerifyOtpAsync(accountId, otp, OTPType.EmailVerification);
            if (!ok) return false;

            var accRepo = _unitOfWork.GetRepository<Account>();
            var acc = await accRepo.SingleOrDefaultAsync(predicate: a => a.AccountId == accountId, include: q => q.Include(x => x.User));
            if (acc == null) return false;

            if (!acc.EmailConfirmed)
            {
                acc.EmailConfirmed = true;
                accRepo.UpdateAsync(acc);
                await _unitOfWork.CommitAsync();
            }
            return true;
        }

        public async Task<TokenResponseDto> VerifyEmailAndLoginAsync(int accountId, string otp, string deviceId)
        {
            var ok = await VerifyEmailAsync(accountId, otp);
            if (!ok) throw new AppValidationException("OTP không hợp lệ hoặc đã hết hạn.");
            var userRepo = _unitOfWork.GetRepository<User>();
            var user = await userRepo.SingleOrDefaultAsync(predicate: u => u.UserId == accountId,
                            include: q => q.Include(x => x.Account));
            if (user == null) throw new AppValidationException("Không tìm thấy người dùng.");

            return await _tokenService.GenerateTokensAsync(user, deviceId);
        }

        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
                throw new AppValidationException("Thiếu thông tin đăng nhập.");
            var accRepo = _unitOfWork.GetRepository<Account>();
            var userRepo = _unitOfWork.GetRepository<User>();
            Account account = await accRepo.SingleOrDefaultAsync(predicate: a => a.Username == dto.UsernameOrEmail,
                                include: q => q.Include(x => x.User));
            if (account == null)
                throw new AppValidationException("Tài khoản hoặc mật khẩu không đúng.");
            if (account.MustChangePassword)
                throw new PasswordChangeRequiredException(account.AccountId);
            if (!account.EmailConfirmed)
                throw new AppValidationException("Email chưa xác minh. Vui lòng xác minh trước khi đăng nhập.");
            var pwdResult = _pwdHasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
            if (pwdResult == PasswordVerificationResult.Failed)
                throw new AppValidationException("Tài khoản hoặc mật khẩu không đúng.");

            if (pwdResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                account.PasswordHash = _pwdHasher.HashPassword(account, dto.Password);
                accRepo.UpdateAsync(account);
                await _unitOfWork.CommitAsync();
            }
            var user = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: u => u.UserId == account.AccountId, include: q => q.Include(x => x.Account));
            if (user == null) throw new AppValidationException("Không tìm thấy người dùng.");

            return await _tokenService.GenerateTokensAsync(user, dto.DeviceInfo);
        }

        public async Task<ResetPasswordResponseDto> PasswordResetRequestAsync(PasswordResetRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email)) throw new AppValidationException("Email là bắt buộc !!");
                var userRepo = _unitOfWork.GetRepository<User>();
                var user = await userRepo.SingleOrDefaultAsync(predicate: u => u.Email == dto.Email, include: q => q.Include(x => x.Account));
                if (user == null || user.Account == null) throw new AppValidationException("Tài khoản không tồn tại.");
                var otp = await _otpService.CreateOtpAsync(user.Account.AccountId, OTPType.PasswordReset, TimeSpan.FromMinutes(5), 6);

                await _rabbitMQService.PublishEmailAsync(new EmailRequestDto
                {
                    ToEmail = user.Email,
                    Subject = "[AptCare] Mã OTP đặt lại mật khẩu",
                    TemplateName = "PasswordResetOtp",
                    Replacements = new Dictionary<string, string>
                    {
                        ["FullName"] = user.FirstName + " " + user.LastName,
                        ["OtpCode"] = otp,
                        ["ExpiredMinutes"] = "5",
                        ["SystemName"] = "AptCare System",
                        ["SupportEmail"] = "support@aptcare.vn",
                        ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                        ["Year"] = DateTime.Now.Year.ToString()
                    }
                });

                return new ResetPasswordResponseDto
                {
                    Message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến.",
                    AccountId = user.Account.AccountId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PasswordResetRequestAsync: {Message}", ex.Message);
                throw new AppValidationException("Đã xảy ra lỗi khi xử lý yêu cầu đặt lại mật khẩu. Vui lòng thử lại sau.");
            }
        }

        public async Task<string> PasswordResetVerifyOtpAsync(PasswordResetVerifyOtpDto dto)
        {
            var ok = await _otpService.VerifyOtpAsync(dto.AccountId, dto.Otp, OTPType.PasswordReset);
            if (!ok) throw new AppValidationException("OTP không hợp lệ hoặc đã hết hạn.");
            var userexist = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: u => u.UserId == dto.AccountId, include: q => q.Include(x => x.Account));
            var resetToken = await _tokenService.CreatePasswordResetTokenAsync(userexist.UserId, TimeSpan.FromMinutes(10));
            return resetToken;
        }

        public async Task PasswordResetConfirmAsync(PasswordResetConfirmDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new AppValidationException("Mật khẩu mới không hợp lệ.");
            var ok = await _tokenService.ConsumePasswordResetTokenAsync(dto.AccountId, dto.ResetToken);
            if (!ok) throw new AppValidationException("Token đặt lại không hợp lệ hoặc đã hết hạn.");
            var accRepo = _unitOfWork.GetRepository<Account>();
            var account = await accRepo.SingleOrDefaultAsync(predicate: a => a.AccountId == dto.AccountId, include: q => q.Include(x => x.User));
            if (account == null) throw new AppValidationException("Không tìm thấy tài khoản.");

            account.PasswordHash = _pwdHasher.HashPassword(account, dto.NewPassword);
            accRepo.UpdateAsync(account);
            await _unitOfWork.CommitAsync();
            await _tokenService.RevokeAllRefreshTokensAsync(dto.AccountId);

            if (account.User?.Email != null)
            {
                await _rabbitMQService.PublishEmailAsync(new EmailRequestDto
                {
                    ToEmail = account.User.Email,
                    Subject = "[AptCare] Mật khẩu của bạn đã được thay đổi",
                    TemplateName = "SecurityNotice",
                    Replacements = new Dictionary<string, string>
                    {
                        ["FullName"] = account.User.FirstName + " " + account.User.LastName,
                        ["ChangedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                        ["SystemName"] = "AptCare System",
                        ["SupportEmail"] = "support@aptcare.vn",
                        ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                        ["Year"] = DateTime.Now.Year.ToString()
                    }
                });
            }
        }

        public async Task<GetOwnProfileDto> GetOwnProfile()
        {
            try
            {
                var userID = _providerContext.CurrentUserId;
                var existUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == userID,
                    include: e => e.Include(i => i.Account)
                               .Include(i => i.UserApartments)
                               .ThenInclude(i => i.Apartment)
                               .ThenInclude(i => i.Floor)
                               .Include(i => i.TechnicianTechniques)
                               .ThenInclude(i => i.Technique),
                    selector: e => _mapper.Map<GetOwnProfileDto>(e)
                );
                var userprofileImagepath = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    predicate: m => m.EntityId == userID && m.Entity == nameof(User),
                    selector: m => m.FilePath
                );
                existUser.profileUrl = userprofileImagepath;
                return existUser;
            }
            catch (Exception ex)
            {
                throw new AppValidationException("Xảy ra lỗi ở GetOwnProfile :" + ex.Message);
            }
        }

        public async Task<TokenResponseDto> FirstLoginChangePasswordAsync(FirstLoginChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new AppValidationException("Mật khẩu mới không hợp lệ.");

            var accRepo = _unitOfWork.GetRepository<Account>();
            var account = await accRepo.SingleOrDefaultAsync(
                predicate: a => a.AccountId == dto.AccountId,
                include: q => q.Include(x => x.User));

            if (account == null)
                throw new AppValidationException("Không tìm thấy tài khoản.");
            var verify = _pwdHasher.VerifyHashedPassword(account, account.PasswordHash, dto.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
                throw new AppValidationException("Mật khẩu hiện tại không đúng.");
            account.PasswordHash = _pwdHasher.HashPassword(account, dto.NewPassword);
            if (account.MustChangePassword) account.MustChangePassword = false;
            if (!account.EmailConfirmed) account.EmailConfirmed = true;
            accRepo.UpdateAsync(account);
            await _unitOfWork.CommitAsync();

            if (account.User?.Email != null)
            {
                await _rabbitMQService.PublishEmailAsync(new EmailRequestDto
                {
                    ToEmail = account.User.Email,
                    Subject = "[AptCare] Mật khẩu của bạn đã được thay đổi",
                    TemplateName = "SecurityNotice",
                    Replacements = new Dictionary<string, string>
                    {
                        ["FullName"] = account.User.FirstName + " " + account.User.LastName,
                        ["ChangedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                        ["SystemName"] = "AptCare System",
                        ["SupportEmail"] = "support@aptcare.vn",
                        ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                        ["Year"] = DateTime.Now.Year.ToString()
                    }
                });
            }

            var user = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: u => u.UserId == account.AccountId, include: q => q.Include(x => x.Account));
            if (user == null) throw new AppValidationException("Không tìm thấy người dùng.");

            return await _tokenService.GenerateTokensAsync(user, dto.DeviceInfo);
        }

        public async Task<TokenResponseDto> RefreshTokensAsync(RefreshRequestDto dto)
        {
            var result = await _tokenService.RefreshTokensAsync(dto.RefreshToken);
            return result;
        }

        public async Task<string> RegisterFCMTokenAsync(FCMRequestDto dto)
        {
            var userId = _providerContext.CurrentUserId;
            var isExistingToken = await _unitOfWork.GetRepository<AccountToken>().AnyAsync(
                predicate: u => u.TokenType == TokenType.FCMToken && u.Token == dto.FcmToken && u.Status == TokenStatus.Active
            );
            if (!isExistingToken)
            {
                await _unitOfWork.GetRepository<AccountToken>().InsertAsync(new AccountToken
                {
                    AccountId = userId,
                    Token = dto.FcmToken,
                    CreatedAt = DateTime.Now,
                    DeviceInfo = dto.DeviceInfo,
                    Status = TokenStatus.Active,
                    TokenType = TokenType.FCMToken
                });

                await _unitOfWork.CommitAsync();
            }
            return "Thành công";
        }

        public async Task<string> RefreshFCMTokenAsync(FCMRequestDto dto)
        {
            var userId = _providerContext.CurrentUserId;

            var tokenExpired = await _unitOfWork.GetRepository<AccountToken>().SingleOrDefaultAsync(
                predicate: u => u.TokenType == TokenType.FCMToken && u.DeviceInfo == dto.DeviceInfo && u.Status == TokenStatus.Active
            );
            if (tokenExpired == null)
            {
                throw new AppValidationException("Fcm Token cũ không tồn tại.", StatusCodes.Status404NotFound);
            }

            tokenExpired.ExpiresAt = DateTime.Now;
            tokenExpired.Status = TokenStatus.Expired;

            _unitOfWork.GetRepository<AccountToken>().UpdateAsync(tokenExpired);

            await _unitOfWork.GetRepository<AccountToken>().InsertAsync(new AccountToken
            {
                AccountId = userId,
                Token = dto.FcmToken,
                CreatedAt = DateTime.Now,
                DeviceInfo = dto.DeviceInfo,
                Status = TokenStatus.Active,
                TokenType = TokenType.FCMToken
            });

            await _unitOfWork.CommitAsync();
            return "Thành công";
        }
    }
}