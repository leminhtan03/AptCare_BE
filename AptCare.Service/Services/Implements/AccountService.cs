using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<string> CreateAccountForUserAsync(CreateAccountForUserDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();


                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.UserId == dto.UserId && u.Status == ActiveStatus.Active,
                    include: source => source.Include(u => u.UserApartments)
                );
                if (user == null)
                {
                    throw new AppValidationException($"Người dùng với ID '{dto.UserId}' không tồn tại.");
                }
                if (!Enum.TryParse<AccountRole>(dto.Role, true, out var roleEnum))
                {
                    throw new Exception($"Vai trò '{dto.Role}'của userExist không hợp lệ.");
                }
                if (roleEnum != AccountRole.Resident && user.UserApartments != null)
                {
                    throw new AppValidationException($"Chỉ có Resident mới được phép tạo tài khoản với User đã có căn hộ.");
                }

                var Password = GenerateRandomPassword();

                user.Account = new Account
                {
                    AccountId = user.UserId,
                    Username = user.Email,
                    PasswordHash = string.Empty,
                    Role = roleEnum,
                    LockoutEnabled = false,
                    EmailConfirmed = false,
                    MustChangePassword = true
                };
                user.Account.PasswordHash = _pwdHasher.HashPassword(user.Account, Password);

                await _unitOfWork.GetRepository<Account>().InsertAsync(user.Account);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var replacements = new Dictionary<string, string>
                {
                    ["SystemName"] = "AptCare",
                    ["FullName"] = user.FirstName + " " + user.LastName,
                    ["Username"] = user.Email,
                    ["TemporaryPassword"] = Password,
                    ["LoginUrl"] = "https://app.aptcare.vn/login",
                    ["ExpireAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    ["SupportEmail"] = "support@aptcare.vn",
                    ["SupportPhoneSuffix"] = " • Hotline: 1900-xxxx",
                    ["Year"] = DateTime.UtcNow.Year.ToString()
                };

                await _mailSender.SendEmailWithTemplateAsync(
                    toEmail: user.Email,
                    subject: "[AptCare] Thông tin đăng nhập của bạn",
                    templateName: "AccountCredentials",
                    replacements: replacements
                );

                return "Đã khởi tạo Account thành công!!";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi tạo tài khoản: " + ex.Message);
            }
        }

        public async Task<IPaginate<UserDto>> GetSystemUserPageAsync(string searchQuery, string role, string status, int page, int pageSize)
        {
            AccountRole? roleEnum = null;
            if (!string.IsNullOrEmpty(role))
            {
                if (Enum.TryParse<AccountRole>(role, true, out AccountRole parsedRole))
                {
                    roleEnum = parsedRole;
                }
                else
                {
                    return new Paginate<UserDto>();
                }
            }
            ActiveStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ActiveStatus>(status, true, out ActiveStatus parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
                else
                {
                    return new Paginate<UserDto>();
                }
            }
            var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(
               selector: u => _mapper.Map<UserDto>(u),
               predicate: u =>
                   (string.IsNullOrEmpty(searchQuery) ||
                       ((u.FirstName + " " + u.LastName).ToLower().Contains(searchQuery) ||
                       u.Email.ToLower().Contains(searchQuery) ||
                       u.PhoneNumber.ToLower().Contains(searchQuery) ||
                       u.CitizenshipIdentity.ToLower().Contains(searchQuery))) &&
                       (u.Account != null) &&
                   (string.IsNullOrEmpty(role) || u.Account.Role == roleEnum.Value) &&
                   (string.IsNullOrEmpty(status) || u.Status == statusEnum.Value),
               include: source => source
                   .Include(u => u.Account)
                   .Include(u => u.UserApartments)
                   .ThenInclude(ua => ua.Apartment),
               orderBy: users => users.OrderBy(u => u.UserId),
               page: page,
               size: pageSize
           );
            var userIds = users.Items.Select(u => u.UserId).ToList();
            var profileImages = await _unitOfWork.GetRepository<Media>()
                .GetListAsync(
                    predicate: m => userIds.Contains(m.EntityId)
                        && m.Entity == nameof(User)
                        && m.Status == ActiveStatus.Active
                );
            var imageDict = profileImages.ToDictionary(m => m.EntityId, m => m.FilePath);
            foreach (var user in users.Items)
            {
                if (imageDict.TryGetValue(user.UserId, out var imagePath))
                {
                    user.ProfileImageUrl = imagePath;
                }
            }
            return users;
        }

        public async Task<string> TogleAccontStatus(int accountId)
        {
            try
            {
                var account = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(
                    predicate: a => a.AccountId == accountId
                );
                if (account == null)
                {
                    throw new AppValidationException($"Tài khoản với ID '{accountId}' không tồn tại.");
                }
                account.LockoutEnabled = !account.LockoutEnabled;
                _unitOfWork.GetRepository<Account>().UpdateAsync(account);
                _unitOfWork.Commit();
                return account.LockoutEnabled ? "Đã khóa tài khoản thành công." : "Đã mở khóa tài khoản thành công.";

            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thay đổi trạng thái tài khoản: " + ex.Message);
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

    }
}
