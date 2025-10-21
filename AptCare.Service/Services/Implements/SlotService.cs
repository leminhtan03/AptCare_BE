using AptCare.Repository.Entities;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
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
using AptCare.Repository.Enum;

namespace AptCare.Service.Services.Implements
{
    public class SlotService : BaseService<SlotService>, ISlotService
    {
        public SlotService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<SlotService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
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
                return "Taọ slot mới thành công";
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
                    predicate: x => x.FromTime == dto.FromTime && x.ToTime == dto.ToTime
                    );
                if (isDupSlot)
                {
                    throw new AppValidationException("Đã tồn tại slot có khoảng thời gian này.");
                }


                _mapper.Map(dto, slot);
                _unitOfWork.GetRepository<Slot>().UpdateAsync(slot);
                await _unitOfWork.CommitAsync();
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
                return "Xóa slot thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<SlotDto> GetSlotByIdAsync(int id)
        {
            var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: p => p.SlotId == id
                );

            if (slot == null)
            {
                throw new AppValidationException("Tầng không tồn tại", StatusCodes.Status404NotFound);
            }

            return slot;
        }

        public async Task<IPaginate<SlotDto>> GetPaginateSlotAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Slot, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.SlotName.Contains(search)) &&
                (string.IsNullOrEmpty(filter) || filter.Equals(p.Status.ToString().ToLower()));

            var result = await _unitOfWork.GetRepository<Slot>().GetPagingListAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            return result;
        }

        public async Task<IEnumerable<SlotDto>> GetSlotsAsync()
        {
            
            var result = await _unitOfWork.GetRepository<Slot>().GetListAsync(
                selector: x => _mapper.Map<SlotDto>(x),
                predicate: p => p.Status == ActiveStatus.Active,
                orderBy: o => o.OrderBy(x => x.DisplayOrder)
                );
            return result;
        }

        private Func<IQueryable<Slot>, IOrderedQueryable<Slot>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.SlotId);

            return sortBy.ToLower() switch
            {
                "display" => q => q.OrderBy(p => p.DisplayOrder),
                "display_desc" => q => q.OrderByDescending(p => p.DisplayOrder),
                _ => q => q.OrderByDescending(p => p.SlotId) // Default sort
            };
        }
    }
}
