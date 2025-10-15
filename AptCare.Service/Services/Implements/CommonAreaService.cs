using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Extensions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class CommonAreaService : BaseService<CommonArea>, ICommonAreaService
    {
        public CommonAreaService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<CommonArea> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
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
                        throw new AppValidationException("Tầng không tồn tại.");
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
                return "Tạo khu vực chung mới thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> UpdateCommonAreaAsync(int id, CommonAreaUpdateDto dto)
        {
            try
            {
                var commonArea = await _unitOfWork.GetRepository<CommonArea>().SingleOrDefaultAsync(
                    predicate: x => x.CommonAreaId == id
                    );

                if (commonArea == null)
                {
                    throw new AppValidationException("Khu vực chung không tồn tại.");
                }

                if (dto.FloorId != null)
                {
                    var floor = await _unitOfWork.GetRepository<Floor>().SingleOrDefaultAsync(
                    predicate: x => x.FloorId == dto.FloorId
                    );
                    if (floor == null)
                    {
                        throw new AppValidationException("Tầng không tồn tại.");
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


                _mapper.Map(dto, commonArea);
                _unitOfWork.GetRepository<CommonArea>().UpdateAsync(commonArea);
                await _unitOfWork.CommitAsync();
                return "Cập nhật Khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> DeleteCommonAreaAsync(int id)
        {
            try
            {
                var commonArea = await _unitOfWork.GetRepository<CommonArea>().SingleOrDefaultAsync(
                    predicate: x => x.CommonAreaId == id
                    );
                if (commonArea == null)
                {
                    throw new AppValidationException("Khu vực chung không tồn tại.");
                }

                _unitOfWork.GetRepository<CommonArea>().DeleteAsync(commonArea);
                await _unitOfWork.CommitAsync();
                return "Xóa Khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<CommonAreaDto> GetCommonAreaByIdAsync(int id)
        {
            var commonArea = await _unitOfWork.GetRepository<CommonArea>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<CommonAreaDto>(x),
                predicate: p => p.CommonAreaId == id,
                include: i => i.Include(x => x.Floor)
                );
            if (commonArea == null)
            {
                throw new AppValidationException("Khu vực chung không tồn tại");
            }

            return commonArea;
        }

        public async Task<IPaginate<CommonAreaDto>> GetPaginateCommonAreaAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

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
