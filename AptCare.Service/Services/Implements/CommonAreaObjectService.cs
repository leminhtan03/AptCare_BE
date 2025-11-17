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
        public CommonAreaObjectService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<CommonAreaObjectService> logger,
            IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateCommonAreaObjectAsync(CommonAreaObjectCreateDto dto)
        {
            var commonArea = await _unitOfWork.GetRepository<CommonArea>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaId == dto.CommonAreaId);

            if (commonArea is null)
                throw new AppValidationException("Khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonArea.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Khu vực chung đã ngưng hoạt động.");

            var isDup = await _unitOfWork.GetRepository<CommonAreaObject>().AnyAsync(
                x => x.CommonAreaId == dto.CommonAreaId && x.Name == dto.Name);

            if (isDup)
                throw new AppValidationException("Tên đối tượng đã tồn tại trong khu vực chung này.", StatusCodes.Status409Conflict);

            var commonAreaObject = _mapper.Map<CommonAreaObject>(dto);

            await _unitOfWork.GetRepository<CommonAreaObject>().InsertAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();
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

            var isDup = await _unitOfWork.GetRepository<CommonAreaObject>().AnyAsync(
                x => x.CommonAreaObjectId != id &&
                     x.CommonAreaId == dto.CommonAreaId &&
                     x.Name == dto.Name);

            if (isDup)
                throw new AppValidationException("Tên đối tượng đã tồn tại trong khu vực chung này.", StatusCodes.Status409Conflict);

            _mapper.Map(dto, commonAreaObject);
            _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();
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

            return "Kích hoạt đối tượng khu vực chung thành công";
        }

        public async Task<string> DeactivateCommonAreaObjectAsync(int id)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>()
                .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectId == id);

            if (commonAreaObject is null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            if (commonAreaObject.Status == ActiveStatus.Inactive)
                throw new AppValidationException("Đối tượng khu vực chung đã ở trạng thái ngưng hoạt động.");

            commonAreaObject.Status = ActiveStatus.Inactive;
            _unitOfWork.GetRepository<CommonAreaObject>().UpdateAsync(commonAreaObject);
            await _unitOfWork.CommitAsync();

            return "Vô hiệu hóa đối tượng khu vực chung thành công";
        }

        public async Task<CommonAreaObjectDto> GetCommonAreaObjectByIdAsync(int id)
        {
            var commonAreaObject = await _unitOfWork.GetRepository<CommonAreaObject>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<CommonAreaObjectDto>(x),
                predicate: p => p.CommonAreaObjectId == id,
                include: i => i.Include(x => x.CommonArea)
                                    .ThenInclude(x => x.Floor));

            if (commonAreaObject == null)
                throw new AppValidationException("Đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            return commonAreaObject;
        }

        public async Task<IPaginate<CommonAreaObjectDto>> GetPaginateCommonAreaObjectAsync(PaginateDto dto, int? commonAreaId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

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

            return result;
        }

        public async Task<IEnumerable<CommonAreaObjectBasicDto>> GetCommonAreaObjectsByCommonAreaAsync(int commonAreaId)
        {
            var result = await _unitOfWork.GetRepository<CommonAreaObject>().GetListAsync(
                selector: s => _mapper.Map<CommonAreaObjectBasicDto>(s),
                predicate: p => p.CommonAreaId == commonAreaId
            );
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
                _ => q => q.OrderByDescending(p => p.CommonAreaObjectId) // Default sort
            };
        }
    }
}