using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
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


        public async Task<CreateUserResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var accountRepo = _unitOfWork.GetRepository<Account>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();
            var aptRepo = _unitOfWork.GetRepository<Apartment>();
            var techRepo = _unitOfWork.GetRepository<Technique>();
            var ttRepo = _unitOfWork.GetRepository<TechnicianTechnique>();

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                if (await userRepo.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    throw new AppValidationException($"Email '{createUserDto.Email}' đã tồn tại trong hệ thống.");
                }
                if (await userRepo.AnyAsync(u => u.PhoneNumber == createUserDto.PhoneNumber))
                {
                    throw new AppValidationException($"Số điện thoại '{createUserDto.PhoneNumber}' đã tồn tại trong hệ thống.");
                }
                if (!string.IsNullOrEmpty(createUserDto.CitizenshipIdentity) &&
                    await userRepo.AnyAsync(u => u.CitizenshipIdentity == createUserDto.CitizenshipIdentity))
                {
                    throw new AppValidationException($"CCCD '{createUserDto.CitizenshipIdentity}' đã tồn tại trong hệ thống.");
                }

                await ValidateUserDataByRole(createUserDto.Role, createUserDto, aptRepo, techRepo);
                var user = _mapper.Map<User>(createUserDto);
                user.Status = ActiveStatus.Active;
                await userRepo.InsertAsync(user);
                await _unitOfWork.CommitAsync();

                if (createUserDto.Role == AccountRole.Resident && createUserDto.Apartments?.Any() == true)
                {
                    await AssignUserToApartmentsAsync(user.UserId, createUserDto.Apartments, aptRepo, uaRepo);
                }

                if ((createUserDto.Role == AccountRole.Technician || createUserDto.Role == AccountRole.TechnicianLead) &&
                    createUserDto.TechniqueIds?.Any() == true)
                {
                    await AssignTechniquesToUserAsync(user.UserId, createUserDto.TechniqueIds, techRepo, ttRepo);
                }
                await _unitOfWork.CommitAsync();

                bool shouldCreateAccount = ShouldCreateAccount(createUserDto.Role, createUserDto.CreateAccount);

                if (shouldCreateAccount)
                {
                    await CreateAccountForNewUserAsync(user, createUserDto.Role, accountRepo);
                    await _unitOfWork.CommitAsync();
                }
                await UserbasicProfileImageAsync(user);
                await _unitOfWork.CommitTransactionAsync();

                var createdUser = await userRepo.SingleOrDefaultAsync(
                    predicate: u => u.UserId == user.UserId,
                    include: i => i.Include(u => u.Account)
                                  .Include(u => u.UserApartments)
                                      .ThenInclude(ua => ua.Apartment)
                                  .Include(u => u.TechnicianTechniques)
                                      .ThenInclude(tt => tt.Technique)
                );
                var userDto = _mapper.Map<UserDto>(createdUser);
                return new CreateUserResponseDto
                {
                    User = userDto,
                    AccountCreated = shouldCreateAccount,
                    EmailSentMessage = shouldCreateAccount
                        ? $"Email thông tin đăng nhập đã được gửi đến {user.Email}"
                        : null,
                    Message = shouldCreateAccount
                        ? $"Tạo người dùng và tài khoản thành công. Vui lòng kiểm tra email {user.Email} để nhận thông tin đăng nhập."
                        : "Tạo người dùng thành công. Bạn có thể tạo tài khoản sau thông qua chức năng quản lý tài khoản."
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi tạo người dùng: " + ex.Message);
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
                    .Include(u => u.Account));

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
                    CreatedAt = DateTime.Now,
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

            var aptList = await aptRepo.GetListAsync(predicate: a => apartmentIds.Contains(a.ApartmentId));
            var aptLimitMap = aptList.ToDictionary(a => a.ApartmentId, a => a.Limit);

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

                var limit = aptLimitMap.TryGetValue(aptId, out var lm) ? lm : int.MaxValue;

                var existingUsers = existingUsersMap.TryGetValue(aptId, out var set)
                    ? set
                    : (existingUsersMap[aptId] = new HashSet<int>());

                var existingCount = existingCountMap.TryGetValue(aptId, out var cnt) ? cnt : 0;
                var freeSlots = Math.Max(0, limit - existingCount);

                var survivors = rowsForApt.Where(r => !rejectedRowIndexes.Contains(r.RowIndex)).ToList();

                var newAdds = survivors.Where(r => !existingUsers.Contains(r.UserId)).ToList();

                IEnumerable<UserApartmentRow> ordered;
                if (!hasOwnerInApt)
                    ordered = newAdds.OrderByDescending(r => r.Role == RoleInApartmentType.Owner).ThenBy(r => r.RowIndex);
                else
                    ordered = newAdds.OrderBy(r => r.RowIndex);

                var toKeep = ordered.Take(freeSlots).ToList();
                var toReject = ordered.Skip(freeSlots).ToList();

                if (toReject.Count > 0)
                {
                    foreach (var r in toReject) rejectedRowIndexes.Add(r.RowIndex);

                    var lines = string.Join(", ", toReject.Select(r => r.RowIndex).OrderBy(i => i));
                    result.Errors.Add(
                        $"[UserApartments] Các dòng {lines}: Vượt giới hạn thành viên (Limit={limit}) của căn hộ. " +
                        $"Hiện có {existingCount}, thêm tối đa {freeSlots} trong batch này."
                    );
                }

                foreach (var r in toKeep)
                {
                    if (existingUsers.Add(r.UserId))
                        existingCount++;
                }
                existingCountMap[aptId] = existingCount;
            }
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
        private async Task ValidateUserDataByRole(AccountRole role, CreateUserDto dto, IGenericRepository<Apartment> aptRepo, IGenericRepository<Technique> techRepo)
        {
            switch (role)
            {
                case AccountRole.Resident:
                    if (dto.Apartments == null || !dto.Apartments.Any())
                    {
                        throw new AppValidationException(
                            "Resident phải có ít nhất một căn hộ. Vui lòng cung cấp thông tin Apartments.");
                    }

                    var IdApartmentCodes = dto.Apartments.Select(a => a.ApartmentId).ToList();
                    var existingApts = await aptRepo.GetListAsync(
                        predicate: a => IdApartmentCodes.Contains(a.ApartmentId) &&
                                       a.Status == ApartmentStatus.Active,
                        include: inc => inc.Include(a => a.UserApartments)
                    );

                    var missingRooms = IdApartmentCodes.Except(existingApts.Select(a => a.ApartmentId)).ToList();
                    if (missingRooms.Any())
                    {
                        throw new AppValidationException(
                            $"Các căn hộ sau không tồn tại hoặc không hoạt động: {string.Join(", ", missingRooms)}");
                    }
                    foreach (var apartment in existingApts)
                    {
                        var activeCount = apartment.UserApartments?
                            .Count(ua => ua.Status == ActiveStatus.Active) ?? 0;

                        if (activeCount >= apartment.Limit)
                        {
                            throw new AppValidationException(
                                $"Căn hộ '{apartment.Room}' (ID: {apartment.ApartmentId}) đã đầy " +
                                $"({activeCount}/{apartment.Limit} người). Không thể thêm cư dân mới.");
                        }
                    }

                    var hasOwner = dto.Apartments.Any(a =>
                        a.RoleInApartment == RoleInApartmentType.Owner);
                    if (!hasOwner)
                    {
                        throw new AppValidationException(
                            "Resident phải là Owner của ít nhất một căn hộ.");
                    }

                    if (dto.TechniqueIds?.Any() == true)
                    {
                        throw new AppValidationException(
                            "Resident không được có kỹ thuật (TechniqueIds). Chỉ Technician mới có.");
                    }
                    break;
                case AccountRole.Technician:
                case AccountRole.TechnicianLead:
                    if (dto.TechniqueIds == null || !dto.TechniqueIds.Any())
                    {
                        throw new AppValidationException(
                            $"{role} phải có ít nhất một kỹ thuật. Vui lòng cung cấp TechniqueIds.");
                    }
                    var existingTechs = await techRepo.GetListAsync(
                        predicate: t => dto.TechniqueIds.Contains(t.TechniqueId)
                    );

                    var missingTechIds = dto.TechniqueIds.Except(existingTechs.Select(t => t.TechniqueId)).ToList();
                    if (missingTechIds.Any())
                    {
                        throw new AppValidationException(
                            $"Các kỹ thuật sau không tồn tại: {string.Join(", ", missingTechIds)}");
                    }
                    if (dto.Apartments?.Any() == true)
                    {
                        throw new AppValidationException(
                            $"{role} không được có căn hộ (Apartments). Chỉ Resident mới có.");
                    }
                    break;
                case AccountRole.Manager:
                case AccountRole.Receptionist:
                case AccountRole.Admin:
                    if (dto.Apartments?.Any() == true)
                    {
                        throw new AppValidationException(
                            $"{role} không được có căn hộ (Apartments).");
                    }
                    if (dto.TechniqueIds?.Any() == true)
                    {
                        throw new AppValidationException(
                            $"{role} không được có kỹ thuật (TechniqueIds).");
                    }
                    break;
            }
        }

        private async Task AssignUserToApartmentsAsync(int userId, List<ApartmentForUserCreateDto> apartments, IGenericRepository<Apartment> aptRepo, IGenericRepository<UserApartment> uaRepo)
        {
            var IdApartmentCodes = apartments.Select(a => a.ApartmentId).ToList();
            var dbApartments = await aptRepo.GetListAsync(predicate: a => IdApartmentCodes.Contains(a.ApartmentId));
            var aptDict = dbApartments.ToDictionary(a => a.ApartmentId);

            foreach (var aptDto in apartments)
            {
                if (!aptDict.TryGetValue(aptDto.ApartmentId, out var apartment))
                    continue;

                if (!Enum.TryParse<RoleInApartmentType>(aptDto.RoleInApartment.ToString(), true, out var roleEnum))
                {
                    throw new AppValidationException($"Vai trò '{aptDto.RoleInApartment}' không hợp lệ.");
                }
                if (roleEnum == RoleInApartmentType.Owner)
                {
                    var hasOwner = await uaRepo.AnyAsync(ua =>
                        ua.ApartmentId == apartment.ApartmentId &&
                        ua.RoleInApartment == RoleInApartmentType.Owner &&
                        ua.Status == ActiveStatus.Active
                    );

                    if (hasOwner)
                    {
                        throw new AppValidationException(
                            $"Căn hộ có ID '{aptDto.ApartmentId}' đã có chủ sở hữu. Không thể thêm Owner thứ hai.");
                    }
                }
                var userApartment = new UserApartment
                {
                    UserId = userId,
                    ApartmentId = apartment.ApartmentId,
                    RoleInApartment = roleEnum,
                    RelationshipToOwner = aptDto.RelationshipToOwner,
                    Status = ActiveStatus.Active,
                    CreatedAt = DateTime.Now
                };

                await uaRepo.InsertAsync(userApartment);
            }
        }

        private async Task AssignTechniquesToUserAsync(int userId, List<int> techniqueIds, IGenericRepository<Technique> techRepo, IGenericRepository<TechnicianTechnique> ttRepo)
        {
            var techniques = await techRepo.GetListAsync(
                predicate: t => techniqueIds.Contains(t.TechniqueId)
            );

            foreach (var technique in techniques)
            {
                var exists = await ttRepo.AnyAsync(tt =>
                    tt.TechnicianId == userId &&
                    tt.TechniqueId == technique.TechniqueId
                );

                if (!exists)
                {
                    var technicianTechnique = new TechnicianTechnique
                    {
                        TechnicianId = userId,
                        TechniqueId = technique.TechniqueId
                    };
                    await ttRepo.InsertAsync(technicianTechnique);
                }
            }
        }
        private bool ShouldCreateAccount(AccountRole role, bool createAccountFlag)
        {
            switch (role)
            {
                case AccountRole.Resident:
                    return createAccountFlag;
                case AccountRole.Technician:
                case AccountRole.TechnicianLead:
                case AccountRole.Manager:
                case AccountRole.Receptionist:
                case AccountRole.Admin:
                    return true;

                default:
                    return false;
            }
        }
        private async Task CreateAccountForNewUserAsync(User user, AccountRole role, IGenericRepository<Account> accountRepo)
        {
            var existingAccount = await accountRepo.AnyAsync(a => a.AccountId == user.UserId);
            if (existingAccount)
            {
                throw new AppValidationException($"Tài khoản cho user ID {user.UserId} đã tồn tại.");
            }
            var password = GenerateRandomPassword();
            var account = new Account
            {
                AccountId = user.UserId,
                Username = user.Email,
                PasswordHash = string.Empty,
                Role = role,
                LockoutEnabled = false,
                EmailConfirmed = false,
                MustChangePassword = true
            };
            account.PasswordHash = _pwdHasher.HashPassword(account, password);

            await accountRepo.InsertAsync(account);
            await SendAccountCredentialsEmailAsync(user, password);
        }
        private async Task SendAccountCredentialsEmailAsync(User user, string password)
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
        }
        private async Task UserbasicProfileImageAsync(User user)
        {
            var mediaRepo = _unitOfWork.GetRepository<Media>();
            var newMedia = new Media
            {
                EntityId = user.UserId,
                Entity = nameof(User),
                FilePath = "https://res.cloudinary.com/dg9k8inku/image/authenticated/s--U3E35abm--/v1762282609/fyh4eg7lptnw17i1syha.jpg",
                FileName = "Ảnh đại diện của userExist" + user.UserId,
                ContentType = "image/png",
                CreatedAt = DateTime.Now,
                Status = ActiveStatus.Active
            };
            await mediaRepo.InsertAsync(newMedia);
        }

    }

}
