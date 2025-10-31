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
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Org.BouncyCastle.Crypto.Generators;
using System.Text;

namespace AptCare.Service.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly IMailSenderService _mailSender;
        private readonly IPasswordHasher<Account> _pwdHasher;
        private readonly ICloudinaryService _cloudinaryService;
        public UserService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, IPasswordHasher<Account> pwdHasher, IMailSenderService mailSenderService, ILogger<UserService> logger, ICloudinaryService cloudinaryService, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _mailSender = mailSenderService;
            _pwdHasher = pwdHasher;
            _cloudinaryService = cloudinaryService;
        }


        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();


            try
            {
                await _unitOfWork.BeginTransactionAsync();
                if (await userRepo.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    throw new AppValidationException($"Email \'{createUserDto.Email}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.PhoneNumber == createUserDto.PhoneNumber))
                {
                    throw new AppValidationException($"Số điện thoại \'{createUserDto.PhoneNumber}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.CitizenshipIdentity == createUserDto.CitizenshipIdentity))
                {
                    throw new AppValidationException($"CCCD \'{createUserDto.CitizenshipIdentity}\' đã tồn tại trong hệ thống.");
                }
                var user = _mapper.Map<User>(createUserDto);
                user.Status = ActiveStatus.Active;
                await _unitOfWork.GetRepository<User>().InsertAsync(user);
                await _unitOfWork.CommitAsync();
                //if (createUserDto.Apartments != null)
                //{
                //    user.UserApartments = new List<UserApartment>();
                //    await SyncUserApartments(user, createUserDto.Apartments);
                //    userRepo.UpdateAsync(user);
                //    await _unitOfWork.CommitAsync();
                //}

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
            var media = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                predicate: m => m.EntityId == userId && m.Entity == nameof(User) && m.Status == ActiveStatus.Active);
            if (media != null)
                user.ProfileImageUrl = media.FilePath;
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IPaginate<UserGetAllDto>> GetReSidentDataPageAsync(string searchQuery, string status, int page, int pageSize)
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
                    return new Paginate<UserGetAllDto>();
                }
            }
            var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(
                selector: u => _mapper.Map<UserGetAllDto>(u),
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

        public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();
            var mediaRepo = _unitOfWork.GetRepository<Media>();

            var user = await userRepo.SingleOrDefaultAsync(
                predicate: u => u.UserId == userId,
                include: i => i
                    .Include(u => u.Account)
                //.Include(u => u.UserApartments)
                //    .ThenInclude(ua => ua.Apartment)
                );

            if (user == null) throw new KeyNotFoundException("Người dùng không tồn tại.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(updateUserDto.FirstName)) user.FirstName = updateUserDto.FirstName;
                if (!string.IsNullOrWhiteSpace(updateUserDto.LastName)) user.LastName = updateUserDto.LastName;
                if (updateUserDto.CitizenshipIdentity != null) user.CitizenshipIdentity = updateUserDto.CitizenshipIdentity;
                if (updateUserDto.Birthday.HasValue) user.Birthday = updateUserDto.Birthday.Value;

                if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
                {
                    user.Email = updateUserDto.Email;
                    if (user.Account != null) user.Account.Username = updateUserDto.Email;
                }
                if (!string.IsNullOrWhiteSpace(updateUserDto.PhoneNumber))
                    user.PhoneNumber = updateUserDto.PhoneNumber;

                if (!string.IsNullOrWhiteSpace(updateUserDto.Status))
                {
                    if (!Enum.TryParse<ActiveStatus>(updateUserDto.Status, true, out var statusEnum))
                        throw new AppValidationException($"Trạng thái '{updateUserDto.Status}' không hợp lệ.");
                    user.Status = statusEnum;
                }

                //await SyncUserApartments(user, updateUserDto.Apartments);

                userRepo.UpdateAsync(user);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var resultUser = _mapper.Map<UserDto>(user);
                var media = await mediaRepo.SingleOrDefaultAsync(
                    predicate: m => m.EntityId == userId && m.Entity == nameof(User) && m.Status == ActiveStatus.Active);
                if (media != null) resultUser.ProfileImageUrl = media.FilePath;
                return resultUser;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }


        //private async Task SyncUserApartments(User user, List<ApartmentForUserDto>? newApartmentsDto)
        //{
        //    var uaRepo = _unitOfWork.GetRepository<UserApartment>();
        //    var aptRepo = _unitOfWork.GetRepository<Apartment>();

        //    var current = (user.UserApartments ?? Enumerable.Empty<UserApartment>())
        //        .Where(ua => ua.Apartment != null)
        //        .ToList();

        //    if (newApartmentsDto == null)
        //    {
        //        foreach (var g in current.GroupBy(x => x.ApartmentId))
        //        {
        //            var owners = g.Where(x => x.Status == ActiveStatus.Active
        //                                   && x.RoleInApartment == RoleInApartmentType.Owner).ToList();
        //            if (owners.Count == 1 && owners[0].UserId == user.UserId)
        //                throw new AppValidationException($"Không thể gỡ Owner khỏi căn '{owners[0].Apartment!.Room}' vì sẽ khiến căn không có chủ sở hữu.");
        //        }

        //        foreach (var ua in current)
        //        {
        //            if (ua.Status != ActiveStatus.Inactive)
        //            {
        //                ua.Status = ActiveStatus.Inactive;
        //                uaRepo.UpdateAsync(ua);
        //            }
        //        }
        //        return;
        //    }
        //    var wantedByRoom = new Dictionary<string, (ApartmentForUserDto Dto, RoleInApartmentType Role)>(StringComparer.OrdinalIgnoreCase);
        //    foreach (var dto in newApartmentsDto)
        //    {
        //        if (!Enum.TryParse<RoleInApartmentType>(dto.RoleInApartment, true, out var roleEnum))
        //            throw new AppValidationException($"Vai trò '{dto.RoleInApartment}' của user trong căn '{dto.Room}' không hợp lệ.");
        //        wantedByRoom[dto.Room] = (dto, roleEnum);
        //    }

        //    var allRooms = current.Select(x => x.Apartment!.Room)
        //                          .Concat(wantedByRoom.Keys)
        //                          .Distinct(StringComparer.OrdinalIgnoreCase)
        //                          .ToList();
        //    var apartments = await aptRepo.GetListAsync(predicate: a => allRooms.Contains(a.Room) && a.Status == ApartmentStatus.Active);
        //    var aptByRoom = apartments.ToDictionary(a => a.Room, a => a, StringComparer.OrdinalIgnoreCase);

        //    var missing = wantedByRoom.Keys.Where(r => !aptByRoom.ContainsKey(r)).ToList();
        //    if (missing.Any())
        //        throw new AppValidationException($"Không tìm thấy các căn hộ: {string.Join(", ", missing)}.");

        //    var aptIds = apartments.Select(a => a.ApartmentId).ToList();
        //    var activeAll = await uaRepo.GetListAsync(predicate: ua => aptIds.Contains(ua.ApartmentId) && ua.Status == ActiveStatus.Active);
        //    var snap = new Dictionary<int, AptSnap>();
        //    foreach (var a in apartments)
        //        snap[a.ApartmentId] = new AptSnap { Limit = a.Limit };

        //    foreach (var ua in activeAll)
        //    {
        //        var s = snap[ua.ApartmentId];
        //        s.ActiveCount += 1;
        //        if (ua.UserId != user.UserId)
        //        {
        //            s.ActiveUsers.Add(ua.UserId);
        //            if (ua.RoleInApartment == RoleInApartmentType.Owner)
        //                s.HasOwner = true;
        //        }
        //    }

        //    var toInsert = new List<UserApartment>();
        //    var toUpdate = new List<UserApartment>();

        //    foreach (var ua in current)
        //    {
        //        var room = ua.Apartment!.Room;
        //        if (wantedByRoom.ContainsKey(room)) continue;

        //        if (ua.Status == ActiveStatus.Active && ua.RoleInApartment == RoleInApartmentType.Owner && !snap[ua.ApartmentId].HasOwner)
        //            throw new AppValidationException($"Không thể gỡ Owner khỏi căn '{room}' vì sẽ khiến căn không có chủ sở hữu.");

        //        if (ua.Status != ActiveStatus.Inactive)
        //        {
        //            ua.Status = ActiveStatus.Inactive;
        //            toUpdate.Add(ua);

        //            var s = snap[ua.ApartmentId];
        //            s.ActiveCount = Math.Max(0, s.ActiveCount - 1);
        //        }
        //    }

        //    foreach (var (room, pack) in wantedByRoom)
        //    {
        //        var (dto, roleEnum) = pack;
        //        var apt = aptByRoom[room];
        //        var aptId = apt.ApartmentId;
        //        var s = snap[aptId];

        //        var currentRel = current.FirstOrDefault(x => x.ApartmentId == aptId);

        //        bool isCurrentlyActive = currentRel != null && currentRel.Status == ActiveStatus.Active;
        //        bool isCurrentlyOwner = isCurrentlyActive && currentRel!.RoleInApartment == RoleInApartmentType.Owner;

        //        if (roleEnum == RoleInApartmentType.Owner && !isCurrentlyOwner && s.HasOwner)
        //            throw new AppValidationException($"Căn '{room}' đã có chủ sở hữu; không thể thêm Owner thứ hai.");

        //        if (isCurrentlyOwner && roleEnum != RoleInApartmentType.Owner && !s.HasOwner)
        //            throw new AppValidationException($"Không thể đổi vai trò Owner của '{room}' vì sẽ khiến căn không có chủ sở hữu.");

        //        bool willIncreaseActive =
        //            (currentRel == null) || (currentRel.Status == ActiveStatus.Inactive);

        //        int after = s.ActiveCount + (willIncreaseActive ? 1 : 0);
        //        if (after > s.Limit)
        //            throw new AppValidationException($"Vượt Limit ({s.Limit}) cho căn '{room}'. Hiện có {s.ActiveCount}, không thể thêm/active quan hệ.");

        //        if (currentRel == null)
        //        {
        //            toInsert.Add(new UserApartment
        //            {
        //                UserId = user.UserId,
        //                ApartmentId = aptId,
        //                RoleInApartment = roleEnum,
        //                RelationshipToOwner = dto.RelationshipToOwner,
        //                Status = ActiveStatus.Active
        //            });

        //            if (willIncreaseActive) s.ActiveCount++;
        //            if (roleEnum == RoleInApartmentType.Owner) s.HasOwner = true;
        //        }
        //        else if (currentRel.Status == ActiveStatus.Inactive)
        //        {
        //            currentRel.Status = ActiveStatus.Active;
        //            currentRel.RoleInApartment = roleEnum;
        //            currentRel.RelationshipToOwner = dto.RelationshipToOwner;
        //            toUpdate.Add(currentRel);

        //            if (willIncreaseActive) s.ActiveCount++;
        //            if (roleEnum == RoleInApartmentType.Owner) s.HasOwner = true;
        //        }
        //        else
        //        {
        //            if (currentRel.RoleInApartment != roleEnum || currentRel.RelationshipToOwner != dto.RelationshipToOwner)
        //            {
        //                currentRel.RoleInApartment = roleEnum;
        //                currentRel.RelationshipToOwner = dto.RelationshipToOwner;
        //                toUpdate.Add(currentRel);
        //                if (roleEnum == RoleInApartmentType.Owner) s.HasOwner = true;
        //                else if (isCurrentlyOwner) s.HasOwner = s.HasOwner;
        //            }
        //        }
        //    }

        //    foreach (var x in toUpdate) uaRepo.UpdateAsync(x);
        //    foreach (var x in toInsert) await uaRepo.InsertAsync(x);
        //}

        public async Task<ImportResultDto> ImportResidentsFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResultDto();
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                ExcelPackage.License.SetNonCommercialPersonal("Internal project");
                using (var package = new ExcelPackage(fileStream))
                {
                    var usersSheet = package.Workbook.Worksheets["ResidentsProfile"];
                    if (usersSheet == null) throw new AppValidationException("Không tìm thấy sheet 'ResidentsProfile' trong file Excel.");

                    await ProcessUsersSheet(usersSheet, result);

                    var relationsSheet = package.Workbook.Worksheets["ApartmentRelation"];
                    if (relationsSheet == null) throw new AppValidationException("Không tìm thấy sheet 'ApartmentRelation' trong file Excel.");

                    await ProcessUserApartmentsSheet(relationsSheet, result);

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

        public async Task<UserDto> CreateAccountForNewUserAsync(CreateInforWithAccount createAccountDto)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var accountRepo = _unitOfWork.GetRepository<Account>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (string.IsNullOrEmpty(createAccountDto.UserData.Email) || string.IsNullOrEmpty(createAccountDto.UserData.PhoneNumber) || string.IsNullOrEmpty(createAccountDto.UserData.CitizenshipIdentity))
                {
                    throw new ArgumentException("Email,Số điện thoại,CCCN là bắt buộc.");
                }
                if (await userRepo.AnyAsync(u => u.Email == createAccountDto.UserData.Email))
                {
                    throw new AppValidationException($"Email \'{createAccountDto.UserData.Email}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.PhoneNumber == createAccountDto.UserData.PhoneNumber))
                {
                    throw new AppValidationException($"Số điện thoại \'{createAccountDto.UserData.PhoneNumber}\' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.CitizenshipIdentity == createAccountDto.UserData.CitizenshipIdentity))
                {
                    throw new AppValidationException($"Mã định danh \'{createAccountDto.UserData.CitizenshipIdentity}\' đã tồn tại trong hệ thống.");
                }

                var user = _mapper.Map<User>(createAccountDto.UserData);
                user.Status = ActiveStatus.Active;
                await userRepo.InsertAsync(user);
                await _unitOfWork.CommitAsync();

                if (!Enum.TryParse<AccountRole>(createAccountDto.AccountRole, true, out var roleEnum))
                {
                    throw new Exception($"Vai trò '{createAccountDto.AccountRole}'của userExist không hợp lệ.");
                }
                //if (roleEnum != AccountRole.Resident)
                //{


                //    if (createAccountDto.UserData.Apartments != null)
                //    {
                //        user.UserApartments = new List<UserApartment>();
                //        await SyncUserApartments(user, createAccountDto.UserData.Apartments);
                //        userRepo.UpdateAsync(user);
                //        await _unitOfWork.CommitAsync();
                //    }
                //}

                if (string.IsNullOrEmpty(createAccountDto.Password))
                {
                    createAccountDto.Password = GenerateRandomPassword();
                }

                user.Account = new Account
                {
                    AccountId = user.UserId,
                    Username = createAccountDto.UserData.Email,
                    PasswordHash = string.Empty,
                    Role = roleEnum,
                    LockoutEnabled = false,
                    EmailConfirmed = false,
                    MustChangePassword = true
                };
                user.Account.PasswordHash = _pwdHasher.HashPassword(user.Account, createAccountDto.Password);

                await _unitOfWork.GetRepository<Account>().InsertAsync(user.Account);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                var replacements = new Dictionary<string, string>
                {
                    ["SystemName"] = "AptCare",
                    ["FullName"] = user.FirstName + " " + user.LastName,
                    ["Username"] = user.Email,
                    ["TemporaryPassword"] = createAccountDto.Password,
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

                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi tạo tài khoản: " + ex.Message);
            }
        }

        public async Task UpdateUserProfileImageAsync(UpdateUserImageProfileDto dto)
        {
            try
            {

                var userRepo = _unitOfWork.GetRepository<User>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();
                var imagePath = await _cloudinaryService.UploadImageAsync(dto.ImageProfileUrl);
                var userOldMedia = await mediaRepo.SingleOrDefaultAsync(predicate: m => m.EntityId == dto.UserId && m.Entity == nameof(User) && m.Status == ActiveStatus.Active);
                await _unitOfWork.BeginTransactionAsync();
                if (userOldMedia != null)
                {
                    userOldMedia.Status = ActiveStatus.Inactive;
                    mediaRepo.UpdateAsync(userOldMedia);
                    await _unitOfWork.CommitAsync();
                }
                var newMedia = new Media
                {
                    EntityId = dto.UserId,
                    Entity = nameof(User),
                    FilePath = imagePath,
                    FileName = "Ảnh đại diện của userExist" + dto.UserId,
                    ContentType = dto.ImageProfileUrl.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    Status = ActiveStatus.Active
                };
                await mediaRepo.InsertAsync(newMedia);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi cập nhật ảnh đại diện: " + ex.Message);
            }
        }

        private async Task ProcessUsersSheet(ExcelWorksheet worksheet, ImportResultDto result)
        {
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
            }

        }

        private async Task ProcessUserApartmentsSheet(ExcelWorksheet worksheet, ImportResultDto result)
        {
            var rowCount = worksheet.Dimension.Rows;
            var userRepo = _unitOfWork.GetRepository<User>();
            var aptRepo = _unitOfWork.GetRepository<Apartment>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();

            var validRows = new List<UserApartmentRow>();

            for (int row = 2; row <= rowCount; row++)
            {
                var userPhoneNumber = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var apartmentCode = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var roleStr = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var relationshipToOwn = worksheet.Cells[row, 4].Value?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(userPhoneNumber) || string.IsNullOrWhiteSpace(apartmentCode) || string.IsNullOrWhiteSpace(roleStr))
                { result.Errors.Add($"[UserApartments] Dòng {row}: Thiếu dữ liệu bắt buộc (SĐT/ Mã căn/ Vai trò)."); continue; }

                var user = await userRepo.SingleOrDefaultAsync(predicate: u => u.PhoneNumber == userPhoneNumber);
                if (user == null)
                { result.Errors.Add($"[UserApartments] Dòng {row}: Không tìm thấy người dùng với SĐT '{userPhoneNumber}'."); continue; }

                var apartment = await aptRepo.SingleOrDefaultAsync(predicate: a => a.Room == apartmentCode);
                if (apartment == null)
                { result.Errors.Add($"[UserApartments] Dòng {row}: Không tìm thấy căn hộ với mã '{apartmentCode}'."); continue; }

                if (!Enum.TryParse<RoleInApartmentType>(roleStr, true, out var roleEnum))
                { result.Errors.Add($"[UserApartments] Dòng {row}: Vai trò '{roleStr}' không hợp lệ."); continue; }

                validRows.Add(new UserApartmentRow
                {
                    RowIndex = row,
                    UserId = user.UserId,
                    ApartmentId = apartment.ApartmentId,
                    Role = roleEnum,
                    RelationshipToOwner = relationshipToOwn
                });
            }
            if (!validRows.Any()) return;

            var apartmentIds = validRows.Select(v => v.ApartmentId).Distinct().ToList();

            //var aptList = await aptRepo.GetListAsync(predicate: a => apartmentIds.Contains(a.ApartmentId));
            //var aptLimitMap = aptList.ToDictionary(a => a.ApartmentId, a => a.Limit);

            var uaList = await uaRepo.GetListAsync(predicate: ua => apartmentIds.Contains(ua.ApartmentId));

            var hasOwnerInDbMap = uaList
                .GroupBy(x => x.ApartmentId)
                .ToDictionary(g => g.Key, g => g.Any(x => x.RoleInApartment == RoleInApartmentType.Owner));

            var existingUsersMap = uaList
                .GroupBy(x => x.ApartmentId)
                .ToDictionary(g => g.Key, g => new HashSet<int>(g.Select(x => x.UserId)));

            var existingCountMap = uaList
                .GroupBy(x => x.ApartmentId)
                .ToDictionary(g => g.Key, g => g.Count());

            var rejectedRowIndexes = new HashSet<int>();

            foreach (var group in validRows.GroupBy(v => v.ApartmentId))
            {
                var aptId = group.Key;
                var rowsForApt = group.ToList();
                var ownersInBatch = rowsForApt.Where(r => r.Role == RoleInApartmentType.Owner).ToList();

                var hasOwnerInApt = hasOwnerInDbMap.TryGetValue(aptId, out var yes) && yes;

                if (hasOwnerInApt)
                {
                    if (ownersInBatch.Any())
                    {
                        foreach (var r in ownersInBatch)
                        {
                            rejectedRowIndexes.Add(r.RowIndex);
                            result.Errors.Add($"[UserApartments] Dòng {r.RowIndex}: Căn hộ đã có chủ sở hữu; không thể thêm thêm Owner.");
                        }
                    }
                }
                else
                {
                    if (ownersInBatch.Count != 1)
                    {
                        foreach (var r in rowsForApt) rejectedRowIndexes.Add(r.RowIndex);

                        var lineList = string.Join(", ", rowsForApt.Select(r => r.RowIndex));
                        var msg = ownersInBatch.Count == 0
                            ? $"[UserApartments] Các dòng {lineList}: Căn hộ chưa có Owner trong hệ thống và batch không cung cấp Owner."
                            : $"[UserApartments] Các dòng {lineList}: Batch cung cấp {ownersInBatch.Count} Owner cho cùng một căn (yêu cầu đúng 1).";
                        result.Errors.Add(msg);
                        continue;
                    }
                }

                //var limit = aptLimitMap.TryGetValue(aptId, out var lm) ? lm : int.MaxValue;

                var existingUsers = existingUsersMap.TryGetValue(aptId, out var set)
                    ? set
                    : (existingUsersMap[aptId] = new HashSet<int>());

                var existingCount = existingCountMap.TryGetValue(aptId, out var cnt) ? cnt : 0;
                //var freeSlots = Math.Max(0, limit - existingCount);

                var survivors = rowsForApt.Where(r => !rejectedRowIndexes.Contains(r.RowIndex)).ToList();

                var newAdds = survivors.Where(r => !existingUsers.Contains(r.UserId)).ToList();

                IEnumerable<UserApartmentRow> ordered;
                if (!hasOwnerInApt)
                    ordered = newAdds.OrderByDescending(r => r.Role == RoleInApartmentType.Owner).ThenBy(r => r.RowIndex);
                else
                    ordered = newAdds.OrderBy(r => r.RowIndex);

                //var toKeep = ordered.Take(freeSlots).ToList();
                //var toReject = ordered.Skip(freeSlots).ToList();

                //if (toReject.Count > 0)
                //{
                //    foreach (var r in toReject) rejectedRowIndexes.Add(r.RowIndex);

                //    var lines = string.Join(", ", toReject.Select(r => r.RowIndex).OrderBy(i => i));
                //    result.Errors.Add(
                //        $"[UserApartments] Các dòng {lines}: Vượt giới hạn thành viên (Limit={limit}) của căn hộ. " +
                //        $"Hiện có {existingCount}, thêm tối đa {freeSlots} trong batch này."
                //    );
                //}

                //foreach (var r in toKeep)
                //{
                //    if (existingUsers.Add(r.UserId))
                //        existingCount++;
                //}
                existingCountMap[aptId] = existingCount;
            }

            // ---------- Chèn các dòng hợp lệ còn lại ----------
            foreach (var row in validRows.Where(r => !rejectedRowIndexes.Contains(r.RowIndex)))
            {
                var exists = await uaRepo.AnyAsync(ua => ua.UserId == row.UserId && ua.ApartmentId == row.ApartmentId);
                if (exists)
                { result.Errors.Add($"[UserApartments] Dòng {row.RowIndex}: Quan hệ đã tồn tại, bỏ qua."); continue; }

                if (row.Role == RoleInApartmentType.Owner)
                {
                    var ownerDup = await uaRepo.AnyAsync(ua => ua.ApartmentId == row.ApartmentId && ua.RoleInApartment == RoleInApartmentType.Owner);
                    if (ownerDup)
                    { result.Errors.Add($"[UserApartments] Dòng {row.RowIndex}: Căn hộ đã có Owner khác (xảy ra do đồng thời)."); continue; }
                }

                await uaRepo.InsertAsync(new UserApartment
                {
                    UserId = row.UserId,
                    ApartmentId = row.ApartmentId,
                    RoleInApartment = row.Role,
                    RelationshipToOwner = row.RelationshipToOwner
                });
            }

            await _unitOfWork.CommitAsync();
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

        private sealed class UserApartmentRow
        {
            public int RowIndex { get; init; }
            public int UserId { get; init; }
            public int ApartmentId { get; init; }
            public RoleInApartmentType Role { get; init; }
            public string? RelationshipToOwner { get; init; }
        }
        private sealed class AptSnap
        {
            public int Limit { get; set; }
            public int ActiveCount { get; set; }
            public bool HasOwner { get; set; }
            public HashSet<int> ActiveUsers { get; } = new();
        }
    }

}
