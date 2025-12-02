using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AptCare.Service.Services.Implements
{
    public class AccountService : BaseService<AccountService>, IAccountService
    {
        private readonly IPasswordHasher<Account> _pwdHasher;
        private readonly IMailSenderService _mailSender;

        public AccountService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, IMailSenderService mailSender, IPasswordHasher<Account> pwdHasher, ILogger<AccountService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _pwdHasher = pwdHasher;
            _mailSender = mailSender;
        }

        public async Task<string> CreateAccountForUserAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.UserId == id && u.Status == ActiveStatus.Active,
                    include: source => source
                        .Include(u => u.UserApartments.Where(ua => ua.Status == ActiveStatus.Active))
                        .Include(u => u.TechnicianTechniques)
                        .Include(u => u.Account)
                );

                if (user == null)
                {
                    throw new AppValidationException(
                        $"Người dùng với ID '{id}' không tồn tại hoặc đã bị vô hiệu hóa.");
                }
                if (user.Account != null)
                {
                    throw new AppValidationException(
                        $"Người dùng đã có tài khoản trong hệ thống. " +
                        $"Username: {user.Account.Username}, Role: {user.Account.Role}");
                }
                AccountRole determinedRole = DetermineUserRoleAutomatically(user);
                var password = GenerateRandomPassword();

                user.Account = new Account
                {
                    AccountId = user.UserId,
                    Username = user.Email,
                    PasswordHash = string.Empty,
                    Role = determinedRole,
                    LockoutEnabled = false,
                    EmailConfirmed = false,
                    MustChangePassword = true
                };
                user.Account.PasswordHash = _pwdHasher.HashPassword(user.Account, password);

                await _unitOfWork.GetRepository<Account>().InsertAsync(user.Account);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                await SendAccountCredentialsEmail(user, password, determinedRole);

                return $"Đã khởi tạo tài khoản thành công với vai trò {determinedRole}!";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating account for user ID {UserId}", id);
                throw new Exception("Lỗi khi tạo tài khoản: " + ex.Message);
            }
        }
        private string GenerateRandomPassword(int length = 12)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*";

            var allChars = upperCase + lowerCase + numbers + specialChars;
            var random = new Random();
            var password = new StringBuilder();

            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }
            var passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }
        private AccountRole DetermineUserRoleAutomatically(User user)
        {
            var hasActiveApartments = user.UserApartments?.Any(ua => ua.Status == ActiveStatus.Active) ?? false;
            if (hasActiveApartments)
            {
                _logger.LogInformation(
                    "User {UserId} has {Count} active apartments → Role: Resident",
                    user.UserId,
                    user.UserApartments?.Count(ua => ua.Status == ActiveStatus.Active));
                return AccountRole.Resident;
            }
            throw new AppValidationException(
                $"Không thể tạo tài khoản cho user ID {user.UserId}. ");
        }
        private async Task SendAccountCredentialsEmail(User user, string password, AccountRole role)
        {
            var replacements = new Dictionary<string, string>
            {
                ["SystemName"] = "AptCare",
                ["FullName"] = $"{user.FirstName} {user.LastName}",
                ["Username"] = user.Email,
                ["TemporaryPassword"] = password,
                ["LoginUrl"] = "https://app.aptcare.vn/login",
                ["ExpireAt"] = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                ["SupportEmail"] = "support@aptcare.vn",
                ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                ["Year"] = DateTime.Now.Year.ToString()
            };

            await _mailSender.SendEmailWithTemplateAsync(
                toEmail: user.Email,
                subject: "[AptCare] Thông tin đăng nhập của bạn",
                templateName: "AccountCredentials",
                replacements: replacements
            );

            _logger.LogInformation(
                "Account credentials email sent to {Email} for user ID {UserId} with role {Role}",
                user.Email,
                user.UserId,
                role);
        }

    }
}
