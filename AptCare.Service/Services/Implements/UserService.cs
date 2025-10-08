using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;
using System.Text;

namespace AptCare.Service.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly IMailSenderService _mailSender;
        public UserService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, IMailSenderService mailSenderService, ILogger<UserService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _mailSender = mailSenderService;
        }


        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var userRepo = _unitOfWork.GetRepository<User>();
                if (await userRepo.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    throw new InvalidOperationException($"Email \'{createUserDto.Email}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.PhoneNumber == createUserDto.PhoneNumber))
                {
                    throw new InvalidOperationException($"Số điện thoại \'{createUserDto.PhoneNumber}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.CitizenshipIdentity == createUserDto.CitizenshipIdentity))
                {
                    throw new InvalidOperationException($"CCCD \'{createUserDto.CitizenshipIdentity}\' đã tồn tại trong hệ thống.");
                }
                var user = _mapper.Map<User>(createUserDto);
                user.Status = ActiveStatus.Active;
                if (createUserDto.Apartments != null)
                {
                    user.UserApartments = new List<UserApartment>();
                    foreach (var aptDto in createUserDto.Apartments)
                    {
                        if (!Enum.TryParse<RoleInApartmentType>(aptDto.RoleInApartment, true, out var roleEnum))
                        {
                            throw new Exception($"Vai trò '{aptDto.RoleInApartment}'của user trong '{aptDto.RoomNumber}' không hợp lệ.");
                        }
                        var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                            predicate: a => a.RoomNumber == aptDto.RoomNumber && a.Status == ApartmentStatus.Active);
                        if (apartment != null)
                        {
                            var userApartment = new UserApartment
                            {
                                ApartmentId = apartment.ApartmentId,
                                RoleInApartment = roleEnum,
                                RelationshipToOwner = aptDto.RelationshipToOwner,
                                Status = ActiveStatus.Active
                            };
                            user.UserApartments.Add(userApartment);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Apartment with RoomNumber '{aptDto.RoomNumber}' not found.");
                        }
                    }
                }
                await _unitOfWork.GetRepository<User>().InsertAsync(user);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi bắt đầu giao dịch: " + ex.Message);
            }
        }
        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == userId,
                include: i => i
                    .Include(u => u.Account)
                    .Include(u => u.UserApartments)
                    .ThenInclude(ua => ua.Apartment),
                selector: u => _mapper.Map<UserDto>(u)
            );
            if (user == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }
            return _mapper.Map<UserDto>(user);
        }
        public async Task<IPaginate<UserDto>> GetReSidentDataPageAsync(string searchQuery, string status, int page, int pageSize)
        {

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
                        (u.Account == null || u.Account.Role == AccountRole.Resident) &&
                    (string.IsNullOrEmpty(status) || u.Status == statusEnum.Value),
                include: source => source
                    .Include(u => u.Account)
                    .Include(u => u.UserApartments)
                    .ThenInclude(ua => ua.Apartment),
                orderBy: users => users.OrderBy(u => u.UserId),
                page: page,
                size: pageSize
            );
            return users;
        }
        public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.UserId == userId,
                include: i => i
                    .Include(u => u.Account)
                    .Include(u => u.UserApartments)
                    .ThenInclude(ua => ua.Apartment));
            if (user == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName;
                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName;
                if (updateUserDto.CitizenshipIdentity != null)
                    user.CitizenshipIdentity = updateUserDto.CitizenshipIdentity;
                if (updateUserDto.Birthday.HasValue)
                    user.Birthday = updateUserDto.Birthday.Value;
                if (!string.IsNullOrEmpty(updateUserDto.Email))
                {
                    user.Email = updateUserDto.Email;
                    if (user.Account != null)
                    {
                        user.Account.Username = updateUserDto.Email;
                    }
                }
                if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                    user.PhoneNumber = updateUserDto.PhoneNumber;
                if (!string.IsNullOrEmpty(updateUserDto.AccountRole) && user.Account != null)
                {
                    if (Enum.TryParse<AccountRole>(updateUserDto.AccountRole, true, out var roleEnum))
                    {
                        user.Account.Role = roleEnum;
                    }
                    else
                    {
                        throw new Exception($"Vai trò '{updateUserDto.AccountRole}' không hợp lệ.");
                    }
                }

                if (!string.IsNullOrEmpty(updateUserDto.Status) && Enum.TryParse<ActiveStatus>(updateUserDto.Status, true, out var statusEnum))
                {
                    user.Status = statusEnum;
                }

                if (updateUserDto.Apartments != null)
                {
                    await SyncUserApartments(user, updateUserDto.Apartments);
                }
                else
                {
                    if (user.UserApartments != null && user.UserApartments.Any())
                    {
                        foreach (var rel in user.UserApartments)
                        {
                            rel.Status = ActiveStatus.Inactive;
                            _unitOfWork.GetRepository<UserApartment>().UpdateAsync(rel);
                        }
                        await _unitOfWork.CommitAsync();
                    }
                }
                _unitOfWork.GetRepository<User>().UpdateAsync(user);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi bắt đầu giao dịch: " + ex.Message);
            }

        }
        private async Task SyncUserApartments(User user, List<ApartmentForUserDto> newApartmentsDto)
        {
            var newApartmentsMap = newApartmentsDto.ToDictionary(dto => dto.RoomNumber, dto => dto);
            var currentRoomNumbers = user.UserApartments.Select(ua => ua.Apartment.RoomNumber).ToHashSet();
            var relationsToRemove = await _unitOfWork.GetRepository<UserApartment>().GetListAsync(
                predicate: ua => ua.UserId == user.UserId && !newApartmentsMap.ContainsKey(ua.Apartment.RoomNumber),
                include: source => source.Include(ua => ua.Apartment)
            );
            if (relationsToRemove.Any())
            {
                foreach (var rel in relationsToRemove)
                {
                    rel.Status = ActiveStatus.Inactive;
                }
            }
            foreach (var dto in newApartmentsDto)
            {
                if (!Enum.TryParse<RoleInApartmentType>(dto.RoleInApartment, true, out var roleEnum))
                {
                    throw new Exception($"Vai trò '{dto.RoleInApartment}'của user trong '{dto.RoomNumber}' không hợp lệ.");
                }

                var existingRelation = user.UserApartments
                    .FirstOrDefault(ua => ua.Apartment.RoomNumber == dto.RoomNumber);
                if (existingRelation != null)
                {
                    if (existingRelation.RoleInApartment != roleEnum || existingRelation.RelationshipToOwner != dto.RelationshipToOwner)
                    {
                        existingRelation.Status = ActiveStatus.Active;
                        existingRelation.RelationshipToOwner = dto.RelationshipToOwner;
                        existingRelation.RoleInApartment = roleEnum;
                        _unitOfWork.GetRepository<UserApartment>().UpdateAsync(existingRelation);
                        await _unitOfWork.CommitAsync();
                    }
                }
                else
                {
                    var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                        predicate: a => a.RoomNumber == dto.RoomNumber && a.Status == ApartmentStatus.Active);
                    if (apartment != null)
                    {
                        var newRelation = new UserApartment
                        {
                            UserId = user.UserId,
                            ApartmentId = apartment.ApartmentId,
                            RoleInApartment = roleEnum,
                            RelationshipToOwner = dto.RelationshipToOwner,
                            Status = ActiveStatus.Active
                        };
                        await _unitOfWork.GetRepository<UserApartment>().InsertAsync(newRelation);
                        await _unitOfWork.CommitAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Apartment with RoomNumber '{dto.RoomNumber}' not found.");
                    }
                }
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
            return users;
        }
        public async Task<ImportResultDto> ImportResidentsFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResultDto();
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                ExcelPackage.License.SetNonCommercialPersonal("Internal project");
                using (var package = new ExcelPackage(fileStream))
                {
                    var usersSheet = package.Workbook.Worksheets["Users"];
                    if (usersSheet == null) throw new InvalidOperationException("Không tìm thấy sheet 'Users' trong file Excel.");

                    var userMap = await ProcessUsersSheet(usersSheet, result);

                    var relationsSheet = package.Workbook.Worksheets["UserApartments"];
                    if (relationsSheet == null) throw new InvalidOperationException("Không tìm thấy sheet 'UserApartments' trong file Excel.");

                    await ProcessUserApartmentsSheet(relationsSheet, userMap, result);

                    if (result.IsSuccess)
                    {
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    else
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                result.Errors.Add($"Lỗi hệ thống nghiêm trọng: {ex.Message}");
            }
            return result;

        }
        private async Task<Dictionary<string, User>> ProcessUsersSheet(ExcelWorksheet worksheet, ImportResultDto result)
        {
            var userMap = new Dictionary<string, User>();
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var phoneNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var email = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var citizenshipIdentity = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                DateTime? ReadUtc(int row, int col)
                {
                    var v = worksheet.Cells[row, col].Value;
                    if (v == null) return null;
                    DateTime dt;
                    if (v is DateTime d) dt = d;
                    else if (double.TryParse(v.ToString(), out var oa)) dt = DateTime.FromOADate(oa);
                    else if (!DateTime.TryParse(worksheet.Cells[row, col].Text, out dt)) return null;
                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
                    }
                    return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                }

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    result.Errors.Add($"[Users] Dòng {row}: PhoneNumber là bắt buộc.");
                    continue;
                }
                if (string.IsNullOrEmpty(email))
                {
                    result.Errors.Add($"[Users] Dòng {row}: Email là bắt buộc.");
                    continue;
                }
                if (string.IsNullOrEmpty(citizenshipIdentity))
                {
                    result.Errors.Add($"[Users] Dòng {row}: CitizenshipIdentity là bắt buộc.");
                    continue;
                }
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.PhoneNumber == phoneNumber && u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        PhoneNumber = phoneNumber,
                        FirstName = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "",
                        LastName = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "",
                        Email = email,
                        CitizenshipIdentity = citizenshipIdentity,
                        Birthday = ReadUtc(row, 6),
                        Status = ActiveStatus.Active
                    };
                    await _unitOfWork.GetRepository<User>().InsertAsync(user);
                }

                await _unitOfWork.CommitAsync();
                userMap[phoneNumber] = user;
            }
            return userMap;
        }
        private async Task ProcessUserApartmentsSheet(ExcelWorksheet worksheet, Dictionary<string, User> userMap, ImportResultDto result)
        {
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var userPhoneNumber = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var apartmentCode = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var roleStr = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var relationshipToOwner = worksheet.Cells[row, 4].Value?.ToString()?.Trim();

                // Validation
                if (!userMap.TryGetValue(userPhoneNumber, out var user))
                {
                    result.Errors.Add($"[UserApartments] Dòng {row}: Không tìm thấy người dùng với SĐT '{userPhoneNumber}' trong Users.");
                    continue;
                }
                var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(predicate: a => a.RoomNumber == apartmentCode);
                if (apartment == null)
                {
                    result.Errors.Add($"[UserApartments] Dòng {row}: Không tìm thấy căn hộ với mã '{apartmentCode}'.");
                    continue;
                }
                if (!Enum.TryParse<RoleInApartmentType>(roleStr, true, out var roleEnum))
                {
                    result.Errors.Add($"[UserApartments] Dòng {row}: Vai trò '{roleStr}' không hợp lệ.");
                    continue;
                }

                var relationExists = await _unitOfWork.GetRepository<UserApartment>().AnyAsync(ua => ua.UserId == user.UserId && ua.ApartmentId == apartment.ApartmentId);
                if (!relationExists)
                {
                    var newRelation = new UserApartment
                    {
                        UserId = user.UserId,
                        ApartmentId = apartment.ApartmentId,
                        RoleInApartment = roleEnum,
                        RelationshipToOwner = relationshipToOwner,

                    };
                    await _unitOfWork.GetRepository<UserApartment>().InsertAsync(newRelation);
                }
            }
            // Lưu tất cả các mối quan hệ mới
            await _unitOfWork.CommitAsync();
        }

        public async Task<UserDto> CreateAccountForNewUserAsync(CreateAccountForNewUserDto createAccountDto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (string.IsNullOrEmpty(createAccountDto.UserData.Email) || string.IsNullOrEmpty(createAccountDto.UserData.PhoneNumber) || string.IsNullOrEmpty(createAccountDto.UserData.CitizenshipIdentity))
                {
                    throw new ArgumentException("Email,Số điện thoại,CCCN là bắt buộc.");
                }
                if (await _unitOfWork.GetRepository<User>().AnyAsync(u => u.Email == createAccountDto.UserData.Email))
                {
                    throw new InvalidOperationException($"Email \'{createAccountDto.UserData.Email}\' đã tồn tại trong hệ thống.");
                }
                if (await _unitOfWork.GetRepository<User>().AnyAsync(u => u.PhoneNumber == createAccountDto.UserData.PhoneNumber))
                {
                    throw new InvalidOperationException($"Số điện thoại \'{createAccountDto.UserData.PhoneNumber}\' đã tồn tại trong hệ thống.");
                }
                if (await _unitOfWork.GetRepository<User>().AnyAsync(u => u.CitizenshipIdentity == createAccountDto.UserData.CitizenshipIdentity))
                {
                    throw new InvalidOperationException($"Mã định danh \'{createAccountDto.UserData.CitizenshipIdentity}\' đã tồn tại trong hệ thống.");
                }

                var user = _mapper.Map<User>(createAccountDto.UserData);
                user.Status = ActiveStatus.Active;
                await _unitOfWork.GetRepository<User>().InsertAsync(user);
                await _unitOfWork.CommitAsync();

                if (!Enum.TryParse<AccountRole>(createAccountDto.AccountRole, true, out var roleEnum))
                {
                    throw new Exception($"Vai trò '{createAccountDto.AccountRole}'của user không hợp lệ.");
                }
                if (roleEnum != AccountRole.Resident)
                {

                    if (createAccountDto.UserData.Apartments != null)
                    {
                        user.UserApartments = new List<UserApartment>();
                        foreach (var aptDto in createAccountDto.UserData.Apartments)
                        {
                            if (!Enum.TryParse<RoleInApartmentType>(aptDto.RoleInApartment, true, out var roleInAptEnum))
                            {
                                throw new Exception($"Vai trò '{aptDto.RoleInApartment}'của user trong '{aptDto.RoomNumber}' không hợp lệ.");
                            }
                            var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                                predicate: a => a.RoomNumber == aptDto.RoomNumber && a.Status == ApartmentStatus.Active);
                            if (apartment != null)
                            {
                                var userApartment = new UserApartment
                                {
                                    ApartmentId = apartment.ApartmentId,
                                    RoleInApartment = roleInAptEnum,
                                    RelationshipToOwner = aptDto.RelationshipToOwner,
                                    Status = ActiveStatus.Active
                                };
                                user.UserApartments.Add(userApartment);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Căn hộ với mã '{aptDto.RoomNumber}' không tồn tại.");
                            }
                        }
                        await _unitOfWork.GetRepository<UserApartment>().InsertRangeAsync(user.UserApartments);
                        await _unitOfWork.CommitAsync();
                    }
                }

                if (string.IsNullOrEmpty(createAccountDto.Password))
                {
                    createAccountDto.Password = GenerateRandomPassword();
                }

                user.Account = new Account
                {
                    AccountId = user.UserId,
                    Username = createAccountDto.UserData.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(createAccountDto.Password),
                    Role = roleEnum,
                    LockoutEnabled = false,
                    EmailConfirmed = false
                };

                await _unitOfWork.GetRepository<Account>().InsertAsync(user.Account);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var replacements = new Dictionary<string, string>
                {
                    ["SystemName"] = "AptCare",
                    ["FullName"] = "Nguyễn Đức A",
                    ["Username"] = user.Email,
                    ["TemporaryPassword"] = "X7!a9pQ2",
                    ["LoginUrl"] = "https://app.aptcare.vn/login",
                    ["ExpireAt"] = "23:59 15/10/2025",
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

                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
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
    }

}
