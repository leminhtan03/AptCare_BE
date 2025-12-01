using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class CommonAreaService : BaseService<CommonAreaService>, ICommonAreaService
    {
        private readonly IRedisCacheService _cacheService;

        public CommonAreaService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<CommonAreaService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateCommonAreaAsync(CommonAreaCreateDto dto)
        {
            try
            {
                if (dto.FloorId != null)
                {
                    var floor = await _unitOfWork.GetRepository<Floor>().SingleOrDefaultAsync(
                    predicate: x => x.FloorId == dto.FloorId
                    );
                    if (floor == null)
                    {
                        throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);
                    }
                    if (floor.Status == ActiveStatus.Inactive)
                    {
                        throw new AppValidationException("Tầng đã ngưng hoạt động.");
                    }
                }

                var isDupCommonArea = await _unitOfWork.GetRepository<CommonArea>().AnyAsync(
                    predicate: x => x.AreaCode == dto.AreaCode
                    );
                if (isDupCommonArea)
                {
                    throw new AppValidationException("Mã khu vực chung đã tồn tại.");
                }

                var CommonArea = _mapper.Map<CommonArea>(dto);

                await _unitOfWork.GetRepository<CommonArea>().InsertAsync(CommonArea);
                await _unitOfWork.CommitAsync();

                // Clear cache after create
                await _cacheService.RemoveByPrefixAsync("common_area");

                return "Tạo khu vực chung mới thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateCommonAreaAsync(int id, CommonAreaUpdateDto dto)
        {
            try
            {
                var commonArea = await _unitOfWork.GetRepository<CommonArea>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaId == id);

                if (commonArea is null)
                    throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                var floor = await _unitOfWork.GetRepository<Floor>()
                    .SingleOrDefaultAsync(predicate: x => x.FloorId == dto.FloorId);

                if (floor is null)
                    throw new AppValidationException("Tầng không tồn tại.", StatusCodes.Status404NotFound);

                if (floor.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Tầng đã ngưng hoạt động.");

                var isDup = await _unitOfWork.GetRepository<CommonArea>().AnyAsync(
                    x => x.CommonAreaId != id && x.AreaCode == dto.AreaCode);

                if (isDup)
                    throw new AppValidationException("Mã khu vực đã tồn tại.", StatusCodes.Status409Conflict);

                _mapper.Map(dto, commonArea);
                _unitOfWork.GetRepository<CommonArea>().UpdateAsync(commonArea);
                await _unitOfWork.CommitAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"common_area:{id}");
                await _cacheService.RemoveByPrefixAsync("common_area:list");
                await _cacheService.RemoveByPrefixAsync("common_area:paginate");

                return "Cập nhật khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteCommonAreaAsync(int id)
        {
            try
            {
                var commonArea = await _unitOfWork.GetRepository<CommonArea>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaId == id);

                if (commonArea is null)
                    throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                _unitOfWork.GetRepository<CommonArea>().DeleteAsync(commonArea);
                await _unitOfWork.CommitAsync();

                // Clear cache after delete
                await _cacheService.RemoveByPrefixAsync("common_area");

                return "Xóa khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<CommonAreaDto> GetCommonAreaByIdAsync(int id)
        {
            var cacheKey = $"common_area:{id}";

            var cachedResult = await _cacheService.GetAsync<CommonAreaDto>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var commonArea = await _unitOfWork.GetRepository<CommonArea>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<CommonAreaDto>(x),
                predicate: p => p.CommonAreaId == id,
                include: i => i.Include(x => x.Floor)
                               .Include(x => x.CommonAreaObjects));

            if (commonArea == null)
                throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, commonArea, TimeSpan.FromMinutes(30));

            return commonArea;
        }

        public async Task<IEnumerable<CommonAreaDto>> GetCommonAreasAsync()
        {
            var cacheKey = $"common_area:list:active";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<CommonAreaDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var commonAreas = await _unitOfWork.GetRepository<CommonArea>().GetListAsync(
                selector: x => _mapper.Map<CommonAreaDto>(x),
                predicate: p => p.Status == ActiveStatus.Active,
                include: i => i.Include(x => x.Floor)
                );

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, commonAreas, TimeSpan.FromMinutes(30));

            return commonAreas;
        }

        public async Task<IPaginate<CommonAreaDto>> GetPaginateCommonAreaAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            var cacheKey = $"common_area:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{dto.sortBy}";

            var cachedResult = await _cacheService.GetAsync<IPaginate<CommonAreaDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            Expression<Func<CommonArea, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Name.Contains(search) ||
                                                 p.AreaCode.Contains(search) ||
                                                 p.Location.Contains(search) ||
                                                 p.Description.Contains(search)) &&
                (string.IsNullOrEmpty(filter) ||
                filter.Equals(p.Status.ToString().ToLower()));

            var result = await _unitOfWork.GetRepository<CommonArea>().GetPagingListAsync(
                selector: x => _mapper.Map<CommonAreaDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.Floor),
                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            // Cache for 15 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }

        private Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.CommonAreaId);

            return sortBy.ToLower() switch
            {
                "name" => q => q.OrderBy(p => p.Name),
                "name_desc" => q => q.OrderByDescending(p => p.Name),
                "floor" => q => q.OrderBy(p => p.Floor.FloorNumber),
                "floor_desc" => q => q.OrderByDescending(p => p.Floor.FloorNumber),
                _ => q => q.OrderByDescending(p => p.CommonAreaId) // Default sort
            };
        }
    }
}
