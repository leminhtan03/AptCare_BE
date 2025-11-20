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
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.SlotDtos;

namespace AptCare.Service.Services.Implements
{
    public class SlotService : BaseService<SlotService>, ISlotService
    {
        private readonly IRedisCacheService _cacheService;

        public SlotService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<SlotService> logger, IMapper mapper, IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateSlotAsync(SlotCreateDto dto)
        {
            try
            {
                if (dto.FromTime >= dto.ToTime)
                {
                    throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
                }

                var isDupSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                    predicate: x => x.FromTime == dto.FromTime && x.ToTime == dto.ToTime
                    );
                if (isDupSlot)
                {
                    throw new AppValidationException("Đã tồn tại slot có khoảng thời gian này.");
                }

                var slot = _mapper.Map<Slot>(dto);

                await _unitOfWork.GetRepository<Slot>().InsertAsync(slot);
                await _unitOfWork.CommitAsync();

                // Clear cache after create
                await _cacheService.RemoveByPrefixAsync("slot");

                return "Tạo slot mới thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateSlotAsync(int id, SlotUpdateDto dto)
        {
            try
            {
                var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                    predicate: x => x.SlotId == id
                    );

                if (slot == null)
                {
                    throw new AppValidationException("Slot không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (dto.FromTime >= dto.ToTime)
                {
                    throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
                }

                var isDupSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                    predicate: x => x.FromTime == dto.FromTime && x.ToTime == dto.ToTime && x.SlotId != id
                    );
                if (isDupSlot)
                {
                    throw new AppValidationException("Đã tồn tại slot có khoảng thời gian này.");
                }

                _mapper.Map(dto, slot);
                _unitOfWork.GetRepository<Slot>().UpdateAsync(slot);
                await _unitOfWork.CommitAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"slot:{id}");
                await _cacheService.RemoveByPrefixAsync("slot:list");
                await _cacheService.RemoveByPrefixAsync("slot:paginate");

                return "Cập nhật slot thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteSlotAsync(int id)
        {
            try
            {
                var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                    predicate: x => x.SlotId == id
                    );

                if (slot == null)
                {
                    throw new AppValidationException("Slot không tồn tại.", StatusCodes.Status404NotFound);
                }

                _unitOfWork.GetRepository<Slot>().DeleteAsync(slot);
                await _unitOfWork.CommitAsync();

                // Clear cache after delete
                await _cacheService.RemoveByPrefixAsync("slot");

                return "Xóa slot thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<SlotDto> GetSlotByIdAsync(int id)
        {
            var cacheKey = $"slot:{id}";

            // Try to get from cache
            var cachedSlot = await _cacheService.GetAsync<SlotDto>(cacheKey);
            if (cachedSlot != null)
            {
                return cachedSlot;
            }

            var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: p => p.SlotId == id
                );

            if (slot == null)
            {
                throw new AppValidationException("Slot không tồn tại", StatusCodes.Status404NotFound);
            }

            // Cache for 1 hour (slots rarely change)
            await _cacheService.SetAsync(cacheKey, slot, TimeSpan.FromHours(1));

            return slot;
        }

        public async Task<IPaginate<SlotDto>> GetPaginateSlotAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;
            string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"slot:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}";

            var cachedResult = await _cacheService.GetAsync<Paginate<SlotDto>>(cacheKey);
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

            Expression<Func<Slot, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.SlotName.Contains(search)) &&
                (string.IsNullOrEmpty(filter) || filterStatus == p.Status);

            var result = await _unitOfWork.GetRepository<Slot>().GetPagingListAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                page: page,
                size: size
                );

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }

        public async Task<IEnumerable<SlotDto>> GetSlotsAsync()
        {
            var cacheKey = "slot:list:active";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<SlotDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _unitOfWork.GetRepository<Slot>().GetListAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: p => p.Status == ActiveStatus.Active,
                orderBy: o => o.OrderBy(x => x.DisplayOrder)
                );

            // Cache for 2 hours (slots rarely change)
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(2));

            return result;
        }

        private Func<IQueryable<Slot>, IOrderedQueryable<Slot>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.SlotId);

            return sortBy.ToLower() switch
            {
                "display" => q => q.OrderBy(p => p.DisplayOrder),
                "display_desc" => q => q.OrderByDescending(p => p.DisplayOrder),
                _ => q => q.OrderByDescending(p => p.SlotId)
            };
        }
    }
}