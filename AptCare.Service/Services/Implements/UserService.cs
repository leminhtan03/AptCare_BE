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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        public UserService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<UserService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            throw new NotImplementedException();


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
                if (!string.IsNullOrEmpty(updateUserDto.Status) && Enum.TryParse<ActiveStatus>(updateUserDto.Status, true, out var statusEnum))
                {
                    user.Status = statusEnum;
                }
                if (updateUserDto.UserApartments != null)
                {
                    await SyncUserApartments(user, updateUserDto.UserApartments);
                }
                _unitOfWork.GetRepository<User>().UpdateAsync(user);
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

        private Func<IQueryable<User>, IOrderedQueryable<User>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return null;

            return sortBy.ToLower() switch
            {
                "email" => q => q.OrderBy(p => p.Email),
                "birthday" => q => q.OrderBy(p => p.Birthday),
                "email_desc" => q => q.OrderByDescending(p => p.Email),
                "birthday_desc" => q => q.OrderByDescending(p => p.Birthday),
                "id" => q => q.OrderBy(p => p.UserId),
                "id_desc" => q => q.OrderByDescending(p => p.UserId),
                _ => q => q.OrderByDescending(p => p.UserId)
            };
        }

        public async Task<ImportResultDto> ImportResidentsFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResultDto();
            var usersToCreate = new Dictionary<string, User>();
            var relationsToAdd = new List<UserApartment>();

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    result.Errors.Add("File Excel không có worksheet nào.");
                    throw new Exception("File Excel không có worksheet nào.");
                }
                result.TotalRows = worksheet.Dimension.Rows - 1;
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Đọc dữ liệu từ các cột
                        var firstName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var lastName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var phoneNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        var email = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        var citizenshipIdentity = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        var apartmentCode = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        var roleInApartmentStr = worksheet.Cells[row, 7].Value?.ToString()?.Trim();


                        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(apartmentCode) || string.IsNullOrEmpty(roleInApartmentStr) || (string.IsNullOrEmpty(email)) || (string.IsNullOrEmpty(lastName)))
                        {
                            throw new Exception("Các cột Họ Tên, SĐT, Mã Căn Hộ, Vai Trò không được để trống.");
                        }

                        if (!Enum.TryParse<RoleInApartmentType>(roleInApartmentStr, true, out var roleEnum))
                        {
                            throw new Exception($"Vai trò '{roleInApartmentStr}' không hợp lệ. Chỉ chấp nhận 'Owner' hoặc 'Member'.");
                        }

                        User userEntity = new User();
                        if (usersToCreate.ContainsKey(phoneNumber))
                        {
                            userEntity = usersToCreate[phoneNumber];
                        }
                        else
                        {
                            userEntity = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: u => u.PhoneNumber == phoneNumber);
                            if (userEntity == null)
                            {
                                userEntity = new User
                                {
                                    FirstName = firstName,
                                    LastName = lastName,
                                    PhoneNumber = phoneNumber,
                                    Email = email,
                                    Status = ActiveStatus.Active
                                };
                                usersToCreate.Add(phoneNumber, userEntity);
                            }
                        }
                        var apartment = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(predicate: a => a.RoomNumber == apartmentCode);
                        if (apartment == null)
                        {
                            throw new Exception($"Mã căn hộ '{apartmentCode}' không tồn tại trong hệ thống.");
                        }

                        // Thêm mối quan hệ vào danh sách chờ
                        relationsToAdd.Add(new UserApartment
                        {
                            User = userEntity,
                            Apartment = apartment,
                            RoleInApartment = roleEnum
                        });

                        result.SuccessfulRows++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Dòng {row}: {ex.Message}");
                    }
                }
            }

            if (result.IsSuccess && (usersToCreate.Any() || relationsToAdd.Any()))
            {
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Thêm các user mới
                    if (usersToCreate.Any())
                    {
                        await _unitOfWork.GetRepository<User>().InsertRangeAsync(usersToCreate.Values);
                        await _unitOfWork.CommitAsync();
                    }

                    foreach (var relation in relationsToAdd)
                    {
                        var exists = await _unitOfWork.GetRepository<UserApartment>().SingleOrDefaultAsync(predicate: u => u.UserId == relation.User.UserId && u.ApartmentId == relation.Apartment.ApartmentId);
                        if (exists == null)
                        {
                            relation.UserId = relation.User.UserId;
                            relation.ApartmentId = relation.Apartment.ApartmentId;
                            await _unitOfWork.GetRepository<UserApartment>().InsertAsync(relation);
                        }
                        else if (exists.Status == ActiveStatus.Inactive)
                        {
                            exists.Status = ActiveStatus.Active;
                            exists.RoleInApartment = relation.RoleInApartment;
                            _unitOfWork.GetRepository<UserApartment>().UpdateAsync(exists);
                        }
                    }

                    await _unitOfWork.CommitAsync();
                    await _unitOfWork.CommitTransactionAsync();

                }
                catch (Exception dbEx)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    result.Errors.Add($"Lỗi khi lưu vào cơ sở dữ liệu: {dbEx.Message}");
                }
            }
            return result;
        }
    }

}
