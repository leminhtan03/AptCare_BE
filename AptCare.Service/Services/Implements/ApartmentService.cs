using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class ApartmentService : BaseService<ApartmentService>, IApartmentService
    {
        private readonly IRedisCacheService _cacheService;

        public ApartmentService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<ApartmentService> logger, IMapper mapper, IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateApartmentAsync(ApartmentCreateDto dto)
        {
            var floor = await _unitOfWork.GetRepository<Floor>()
                .SingleOrDefaultAsync(predicate: x => x.FloorId == dto.FloorId);

            if (floor is null)
                throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);

            if (floor.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Tầng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                x => x.FloorId == dto.FloorId && x.Room == dto.Room);

            if (isDup)
                throw new AppValidationException("Số phòng đã tồn tại.", StatusCodes.Status409Conflict);

            var apartment = _mapper.Map<Apartment>(dto);

            await _unitOfWork.GetRepository<Apartment>().InsertAsync(apartment);
            await _unitOfWork.CommitAsync();

            // Clear cache after create
            await _cacheService.RemoveByPrefixAsync("apartment");

            return "Tạo căn hộ mới thành công";
        }
        public async Task<string> UpdateApartmentAsync(int id, ApartmentUpdateDto dto)
        {
            var apartment = await _unitOfWork.GetRepository<Apartment>()
                .SingleOrDefaultAsync(predicate: x => x.ApartmentId == id);

            if (apartment is null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            var floor = await _unitOfWork.GetRepository<Floor>()
                .SingleOrDefaultAsync(predicate: x => x.FloorId == dto.FloorId);

            if (floor is null)
                throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);

            if (floor.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Tầng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                x => x.ApartmentId != id &&
                     x.FloorId == dto.FloorId &&
                     x.Room == dto.Room);

            if (isDup)
                throw new AppValidationException("Số phòng đã tồn tại.", StatusCodes.Status409Conflict);

            _mapper.Map(dto, apartment);
            _unitOfWork.GetRepository<Apartment>().UpdateAsync(apartment);
            await _unitOfWork.CommitAsync();

            // Clear cache after update
            await _cacheService.RemoveAsync($"apartment:{id}");
            await _cacheService.RemoveByPrefixAsync("apartment:list");
            await _cacheService.RemoveByPrefixAsync("apartment:paginate");

            return "Cập nhật căn hộ thành công";
        }
        public async Task<string> DeleteApartmentAsync(int id)
        {
            var apartment = await _unitOfWork.GetRepository<Apartment>()
                .SingleOrDefaultAsync(predicate: x => x.ApartmentId == id);

            if (apartment is null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            _unitOfWork.GetRepository<Apartment>().DeleteAsync(apartment);
            await _unitOfWork.CommitAsync();

            // Clear cache after delete
            await _cacheService.RemoveByPrefixAsync("apartment");

            return "Xóa căn hộ thành công";
        }
        public async Task<ApartmentDto> GetApartmentByIdAsync(int id)
        {
            var cacheKey = $"apartment:{id}";

            var cachedApartment = await _cacheService.GetAsync<ApartmentDto>(cacheKey);
            if (cachedApartment != null)
            {
                return cachedApartment;
            }

            var apt = await _unitOfWork.GetRepository<Apartment>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<ApartmentDto>(x),
                predicate: p => p.ApartmentId == id,
                include: i => i.Include(x => x.UserApartments)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Account));
            if (apt == null)
                throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);

            await LoadUserProfileImagesAsync(apt);

            // Cache for 20 minutes
            await _cacheService.SetAsync(cacheKey, apt, TimeSpan.FromMinutes(20));

            return apt;
        }
        public async Task<IPaginate<ApartmentDto>> GetPaginateApartmentAsync(PaginateDto dto, int? floorId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            var cacheKey = $"technique:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{dto.sortBy}:floorId:{floorId}";

            var cachedResult = await _cacheService.GetAsync<Paginate<ApartmentDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            ApartmentStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<ApartmentStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            if (floorId != null)
            {
                var isExistingFloor = await _unitOfWork.GetRepository<Floor>().AnyAsync(predicate: x => x.FloorId == floorId);
                if (!isExistingFloor)
                    throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);
            }


            Expression<Func<Apartment, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Room.ToString().Contains(search) ||
                                         (p.Description != null && p.Description.Contains(search))) &&
                (string.IsNullOrEmpty(filter) || filterStatus == p.Status) &&
                (floorId == null || p.FloorId == floorId);

            var result = await _unitOfWork.GetRepository<Apartment>().GetPagingListAsync(
                selector: s => _mapper.Map<ApartmentDto>(s),
                predicate: predicate,
                include: i => i.Include(x => x.UserApartments!)
                                .ThenInclude(x => x.User)
                                .ThenInclude(x => x.Account),
                orderBy: BuildOrderBy(dto.sortBy ?? string.Empty),
                page: page,
                size: size
            );

            foreach (var apartment in result.Items)
                await LoadUserProfileImagesAsync(apartment);

            // Cache for 10 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            return result;
        }
        public async Task<IEnumerable<ApartmentBasicDto>> GetApartmentsByFloorAsync(int floorId)
        {
            var cacheKey = $"apartment:list:by_floor:{floorId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<ApartmentBasicDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _unitOfWork.GetRepository<Apartment>().GetListAsync(
                selector: s => _mapper.Map<ApartmentBasicDto>(s),
                predicate: p => p.FloorId == floorId,
                include: i => i.Include(x => x.Floor)
                               .Include(x => x.UserApartments),
                orderBy: o => o.OrderBy(x => x.Room)
            );

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }
        private Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.ApartmentId);

            return sortBy.ToLower() switch
            {
                "room" => q => q.OrderBy(p => p.Room),
                "room_desc" => q => q.OrderByDescending(p => p.Room),
                _ => q => q.OrderByDescending(p => p.Room) // Default sort
            };
        }
        public async Task<ApartmentDto> UpadteUserDataForAptAsync(int AptId, UpdateApartmentWithResidentDataDto dto)
        {
            var aptRepo = _unitOfWork.GetRepository<Apartment>();
            var uaRepo = _unitOfWork.GetRepository<UserApartment>();
            var floorRepo = _unitOfWork.GetRepository<Floor>();
            var userRepo = _unitOfWork.GetRepository<User>();

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var apartment = await aptRepo.SingleOrDefaultAsync(
                    predicate: a => a.ApartmentId == AptId,
                    include: inc => inc.Include(a => a.UserApartments.Where(ua => ua.Status == ActiveStatus.Active))
                                      .ThenInclude(ua => ua.User)
                );
                if (apartment == null)
                {
                    throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (dto.Residents == null || !dto.Residents.Any())
                {
                    await ClearAllApartmentMembersAsync(apartment, uaRepo);
                }
                else
                {
                    await ManageApartmentMembersAsync(apartment, dto.Residents, userRepo, uaRepo);
                }
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"apartment:{AptId}");
                await _cacheService.RemoveByPrefixAsync("apartment:list");
                await _cacheService.RemoveByPrefixAsync("apartment:paginate");

                var updatedApartment = await aptRepo.SingleOrDefaultAsync(
                    predicate: a => a.ApartmentId == AptId,
                    include: inc => inc.Include(a => a.UserApartments.Where(ua => ua.Status == ActiveStatus.Active))
                                      .ThenInclude(ua => ua.User)
                                          .ThenInclude(u => u.Account)
                );

                var updatedApartmentDto = _mapper.Map<ApartmentDto>(updatedApartment);
                await LoadUserProfileImagesAsync(updatedApartmentDto);

                return updatedApartmentDto;
            }
            catch (AppValidationException e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException(e.Message, e.StatusCode);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException("Cập nhật dữ liệu cư dân cho căn hộ thất bại: " + ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
        private async Task ClearAllApartmentMembersAsync(Apartment apartment, IGenericRepository<UserApartment> uaRepo)
        {
            var existingMembers = apartment.UserApartments?
                .Where(ua => ua.Status == ActiveStatus.Active)
                .ToList() ?? new List<UserApartment>();

            if (!existingMembers.Any())
            {
                _logger.LogInformation(
                    "Căn hộ đã làm trống trước đấy!",
                    apartment.ApartmentId);
                return;
            }
            var ownerCount = existingMembers.Count(ua => ua.RoleInApartment == RoleInApartmentType.Owner);
            var memberCount = existingMembers.Count - ownerCount;

            foreach (var member in existingMembers)
            {
                member.Status = ActiveStatus.Inactive;
                member.DisableAt = DateTime.Now;
                uaRepo.UpdateAsync(member);
            }
        }
        private async Task ManageApartmentMembersAsync(Apartment apartment, ICollection<ResidentOfApartmentDto> newResidents, IGenericRepository<User> userRepo, IGenericRepository<UserApartment> uaRepo)
        {
            var existingUserApartments = apartment.UserApartments?
                .Where(ua => ua.Status == ActiveStatus.Active)
                .ToList() ?? new List<UserApartment>();
            var existingUserIds = existingUserApartments.Select(ua => ua.UserId).ToHashSet();
            var newUserIds = newResidents.Select(r => r.UserId).ToHashSet();

            await ValidateNewResidentsAsync(newResidents, newUserIds, userRepo);
            ValidateMemberLimit(apartment, existingUserIds, newUserIds);
            await ValidateOwnerConstraintAsync(apartment, newResidents, existingUserApartments);
            var usersToRemove = existingUserIds.Except(newUserIds).ToList();
            var usersToAdd = newUserIds.Except(existingUserIds).ToList();
            var usersToUpdate = existingUserIds.Intersect(newUserIds).ToList();

            var newOwnerId = newResidents.First(r => r.RoleInApartment == RoleInApartmentType.Owner).UserId;
            var currentOwnerId = existingUserApartments.FirstOrDefault(ua => ua.RoleInApartment == RoleInApartmentType.Owner)?.UserId;

            foreach (var userId in usersToRemove)
            {
                var ua = existingUserApartments.First(x => x.UserId == userId);
                if (ua.RoleInApartment == RoleInApartmentType.Owner)
                {
                    if (newOwnerId == userId)
                    {
                        throw new AppValidationException($" Lỗi logic: Không thể xóa User {userId} vì đây là Owner mới.");
                    }
                }

                ua.Status = ActiveStatus.Inactive;
                ua.DisableAt = DateTime.Now;
                uaRepo.UpdateAsync(ua);
            }
            foreach (var userId in usersToAdd)
            {
                var residentDto = newResidents.First(r => r.UserId == userId);

                var newUserApartment = new UserApartment
                {
                    UserId = userId,
                    ApartmentId = apartment.ApartmentId,
                    RoleInApartment = residentDto.RoleInApartment,
                    RelationshipToOwner = residentDto.RelationWithOwner,
                    Status = ActiveStatus.Active,
                    CreatedAt = DateTime.Now
                };
                await uaRepo.InsertAsync(newUserApartment);
            }
            foreach (var userId in usersToUpdate)
            {
                var existingUA = existingUserApartments.First(ua => ua.UserId == userId);
                var newData = newResidents.First(r => r.UserId == userId);
                bool hasChanges = false;
                if (existingUA.RoleInApartment != newData.RoleInApartment)
                {
                    if (newData.RoleInApartment == RoleInApartmentType.Owner)
                    {
                        var otherCurrentOwner = existingUserApartments.FirstOrDefault(ua =>
                            ua.UserId != userId &&
                            ua.RoleInApartment == RoleInApartmentType.Owner);

                        if (otherCurrentOwner != null)
                        {
                            if (!usersToRemove.Contains(otherCurrentOwner.UserId))
                            {
                                throw new AppValidationException($"Không thể đổi User {userId} thành Owner. "
                                    + $"Căn hộ đã có Owner (User {otherCurrentOwner.UserId}) và Owner này không bị xóa.");
                            }
                        }
                    }

                    existingUA.RoleInApartment = newData.RoleInApartment;
                    hasChanges = true;
                }

                if (existingUA.RelationshipToOwner != newData.RelationWithOwner)
                {
                    existingUA.RelationshipToOwner = newData.RelationWithOwner;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    uaRepo.UpdateAsync(existingUA);
                }
            }
        }
        private async Task ValidateNewResidentsAsync(ICollection<ResidentOfApartmentDto> residents, HashSet<int> userIds, IGenericRepository<User> userRepo)
        {

            if (userIds.Count != residents.Count)
            {
                throw new AppValidationException("Danh sách residents có userId trùng lặp.");
            }
            var existingUsers = await userRepo.GetListAsync(
                predicate: u => userIds.Contains(u.UserId) && u.Status == ActiveStatus.Active
            );
            var missingUserIds = userIds.Except(existingUsers.Select(u => u.UserId)).ToList();
            if (missingUserIds.Any())
            {
                throw new AppValidationException(
                    $"Các User ID sau không tồn tại hoặc đã bị vô hiệu hóa: {string.Join(", ", missingUserIds)}");
            }
        }
        private void ValidateMemberLimit(Apartment apartment, HashSet<int> existingUserIds, HashSet<int> newUserIds)
        {
            var finalMemberCount = newUserIds.Count;
            if (finalMemberCount > apartment.Limit)
            {
                throw new AppValidationException($"Số lượng cư dân ({finalMemberCount}) vượt quá giới hạn của căn hộ ({apartment.Limit}). "
                    + $"Vui lòng tăng Limit hoặc giảm số lượng residents.");
            }
        }
        private async Task ValidateOwnerConstraintAsync(Apartment apartment, ICollection<ResidentOfApartmentDto> newResidents, List<UserApartment> existingUserApartments)
        {
            var newOwners = newResidents.Where(r => r.RoleInApartment == RoleInApartmentType.Owner).ToList();
            if (newOwners.Count == 0)
                throw new AppValidationException(
                    "Căn hộ phải có ít nhất một chủ sở hữu (Owner).");
            if (newOwners.Count > 1)
                throw new AppValidationException($"Căn hộ chỉ có thể có một chủ sở hữu. Danh sách có {newOwners.Count} Owners.");

            var newOwnerId = newOwners.First().UserId;
            var currentOwner = existingUserApartments.FirstOrDefault(ua =>
                ua.RoleInApartment == RoleInApartmentType.Owner);
        }
        private async Task LoadUserProfileImagesAsync(ApartmentDto apartment)
        {
            var userIds = apartment.Users?.Select(ua => ua.User.UserId).ToList() ?? new List<int>();

            if (!userIds.Any())
                return;

            var profileImages = await _unitOfWork.GetRepository<Media>()
                .GetListAsync(
                    predicate: m => userIds.Contains(m.EntityId)
                        && m.Entity == nameof(User)
                        && m.Status == ActiveStatus.Active
                );

            var imageDict = profileImages.ToDictionary(m => m.EntityId, m => m.FilePath);

            foreach (var userApartment in apartment.Users)
            {
                if (userApartment.User != null && imageDict.TryGetValue(userApartment.User.UserId, out var imagePath))
                {
                    userApartment.User.ProfileImageUrl = imagePath;
                }
            }
        }
    }
}
