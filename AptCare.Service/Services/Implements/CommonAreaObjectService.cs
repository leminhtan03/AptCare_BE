using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class CommonAreaObjectService : BaseService<CommonAreaObjectService>, ICommonAreaObjectService
    {
        private readonly IRedisCacheService _cacheService;

        public CommonAreaObjectService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<CommonAreaObjectService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateCommonAreaObjectAsync(CommonAreaObjectCreateDto dto)
        {
            var commonArea = await _unitOfWork.GetRepository<CommonArea>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaId == dto.CommonAreaId);

            if (commonArea is null)
                throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonArea.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Khu vực chung đã ngưng hoạt động.");

            var type = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId);

            if (type is null)
                throw new AppValidationException("Loại đối tượng không tồn tại.", StatusCodes.Status404NotFound);

            if (type.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Loại đối tượng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<CommonAreaObject>().AnyAsync(
                x => x.CommonAreaId == dto.CommonAreaId && x.Name == dto.Name);

            if (isDup)
                throw new AppValidationException("Tên đối tượng đã tồn tại trong khu vực chung này.", StatusCodes.Status409Conflict);

            var commonAreaObject = _mapper.Map<CommonAreaObject>(dto);

            await _unitOfWork.GetRepository<CommonAreaObject>().InsertAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();

            // Clear cache after create
            await _cacheService.RemoveByPrefixAsync("common_area_object");

            return "Tạo đối tượng khu vực chung mới thành công";
        }

        public async Task<string> UpdateCommonAreaObjectAsync(int id, CommonAreaObjectUpdateDto dto)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectId == id);

            if (commonAreaObject is null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            var commonArea = await _unitOfWork.GetRepository<CommonArea>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaId == dto.CommonAreaId);

            if (commonArea is null)
                throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonArea.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Khu vực chung đã ngưng hoạt động.");

            var type = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId);

            if (type is null)
                throw new AppValidationException("Loại đối tượng không tồn tại.", StatusCodes.Status404NotFound);

            if (type.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Loại đối tượng đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<CommonAreaObject>().AnyAsync(
                x => x.CommonAreaObjectId != id &&
                     x.CommonAreaId == dto.CommonAreaId &&
                     x.Name == dto.Name);

            if (isDup)
                throw new AppValidationException("Tên đối tượng đã tồn tại trong khu vực chung này.", StatusCodes.Status409Conflict);

            _mapper.Map(dto, commonAreaObject);
            _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();

            // Clear cache after update
            await _cacheService.RemoveAsync($"common_area_object:{id}");
            await _cacheService.RemoveByPrefixAsync("common_area_object:list");
            await _cacheService.RemoveByPrefixAsync("common_area_object:paginate");

            return "Cập nhật đối tượng khu vực chung thành công";
        }

        public async Task<string> DeleteCommonAreaObjectAsync(int id)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectId == id);

            if (commonAreaObject is null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            _unitOfWork.GetRepository<CommonAreaObject>().DeleteAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();

            // Clear cache after delete
            await _cacheService.RemoveByPrefixAsync("common_area_object");

            return "Xóa đối tượng khu vực chung thành công";
        }

        public async Task<string> ActivateCommonAreaObjectAsync(int id)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                .SingleOrDefaultAsync(
                    predicate: x => x.CommonAreaObjectId == id,
                    include: i => i.Include(x => x.CommonArea));

            if (commonAreaObject is null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonAreaObject.Status == ActiveStatus.Active)
                throw new AppValidationException("Đối tượng khu vực chung đã ở trạng thái hoạt động.");

            if (commonAreaObject.CommonArea.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Không thể kích hoạt đối tượng khi khu vực chung đã ngưng hoạt động.");

            commonAreaObject.Status = ActiveStatus.Active;
            _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();

            // Clear cache after activate
            await _cacheService.RemoveAsync($"common_area_object:{id}");
            await _cacheService.RemoveByPrefixAsync("common_area_object:list");
            await _cacheService.RemoveByPrefixAsync("common_area_object:paginate");

            return "Kích hoạt đối tượng khu vực chung thành công";
        }

        public async Task<string> DeactivateCommonAreaObjectAsync(int id)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                .SingleOrDefaultAsync(
                predicate: x => x.CommonAreaObjectId == id,
                include: i => i.Include(x => x.MaintenanceSchedule)
                );

            if (commonAreaObject is null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonAreaObject.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Đối tượng khu vực chung đã ở trạng thái ngưng hoạt động.");

            commonAreaObject.Status = ActiveStatus.Inactive;
            _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);

            if (commonAreaObject.MaintenanceSchedule != null && commonAreaObject.MaintenanceSchedule.Status == ActiveStatus.Active)
            {
                commonAreaObject.MaintenanceSchedule.Status = ActiveStatus.Inactive;
                _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);
            }
            await _unitOfWork.CommitAsync();

            // Clear cache after deactivate
            await _cacheService.RemoveAsync($"common_area_object:{id}");
            await _cacheService.RemoveByPrefixAsync("common_area_object:list");
            await _cacheService.RemoveByPrefixAsync("common_area_object:paginate");

            return "Vô hiệu hóa đối tượng khu vực chung thành công";
        }

        public async Task<CommonAreaObjectDto> GetCommonAreaObjectByIdAsync(int id)
        {
            var cacheKey = $"common_area_object:{id}";

            var cachedResult = await _cacheService.GetAsync<CommonAreaObjectDto>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<CommonAreaObjectDto>(x),
                predicate: p => p.CommonAreaObjectId == id,
                include: i => i.Include(x => x.CommonArea)
                                    .ThenInclude(x => x.Floor));

            if (commonAreaObject == null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, commonAreaObject, TimeSpan.FromMinutes(30));

            return commonAreaObject;
        }

        public async Task<IPaginate<CommonAreaObjectDto>> GetPaginateCommonAreaObjectAsync(PaginateDto dto, int? commonAreaId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;
            string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"common_area_object:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}:area:{commonAreaId}";

            var cachedResult = await _cacheService.GetAsync<Paginate<CommonAreaObjectDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            ActiveStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<ActiveStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }
            if (commonAreaId != null)
            {
                var isExistingCommonArea = await _unitOfWork.GetRepository<CommonArea>().AnyAsync(
                    predicate: x => x.CommonAreaId == commonAreaId);
                if (!isExistingCommonArea)
                    throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);
            }

            Expression<Func<CommonAreaObject, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Name.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search))) &&
                (string.IsNullOrEmpty(filter) || filterStatus == p.Status) &&
                (commonAreaId == null || p.CommonAreaId == commonAreaId);

            var result = await _unitOfWork.GetRepository<CommonAreaObject>().GetPagingListAsync(
                selector: s => _mapper.Map<CommonAreaObjectDto>(s),
                predicate: predicate,
                include: i => i.Include(x => x.CommonArea)
                                    .ThenInclude(x => x.Floor),
                orderBy: BuildOrderBy(dto.sortBy ?? string.Empty),
                page: page,
                size: size
            );

            // Cache for 15 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }

        public async Task<IEnumerable<CommonAreaObjectBasicDto>> GetCommonAreaObjectsByCommonAreaAsync(int commonAreaId)
        {
            var cacheKey = $"common_area_object:list:by_area:{commonAreaId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<CommonAreaObjectBasicDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _unitOfWork.GetRepository<CommonAreaObject>().GetListAsync(
                selector: s => _mapper.Map<CommonAreaObjectBasicDto>(s),
                predicate: p => p.CommonAreaId == commonAreaId
            );

            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        public async Task<IEnumerable<CommonAreaObjectBasicDto>> GetCommonAreaObjectsByTypeAsync(int typeId)
        {
            var cacheKey = $"common_area_object:list:by_type:{typeId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<CommonAreaObjectBasicDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            // Verify that the type exists
            var typeExists = await _unitOfWork.GetRepository<CommonAreaObjectType>().AnyAsync(
                predicate: x => x.CommonAreaObjectTypeId == typeId);

            if (!typeExists)
                throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            var result = await _unitOfWork.GetRepository<CommonAreaObject>().GetListAsync(
                selector: s => _mapper.Map<CommonAreaObjectBasicDto>(s),
                predicate: p => p.CommonAreaObjectTypeId == typeId
            );

            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        private Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(p => p.CommonAreaObjectId);

            return sortBy.ToLower() switch
            {
                "name" => q => q.OrderBy(p => p.Name),
                "name_desc" => q => q.OrderByDescending(p => p.Name),
                "common_area" => q => q.OrderBy(p => p.CommonAreaId),
                "common_area_desc" => q => q.OrderByDescending(p => p.CommonAreaId),
                _ => q => q.OrderByDescending(p => p.CommonAreaObjectId)
            };
        }
    }
}