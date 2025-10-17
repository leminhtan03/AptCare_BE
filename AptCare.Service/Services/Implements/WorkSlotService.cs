using AptCare.Repository.Entities;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.WorkSlotDtos;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.UserDtos;
using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Services.Implements
{
    public class WorkSlotService : BaseService<WorkSlotService>, IWorkSlotService
    {
        private readonly IUserContext _userContext;

        public WorkSlotService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<WorkSlotService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateWorkSlotsFromDateToDateAsync(WorkSlotCreateFromDateToDateDto dto)
        {
            try
            {
                var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == dto.TechnicianId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                if (!isExistingTechnician)
                {
                    throw new Exception("Kĩ thuật viên không tồn tại.");
                }

                if (dto.FromDate > dto.ToDate)
                {
                    throw new Exception("'Từ ngày' phải nhỏ hơn hoặc bằng 'Đến ngày'");
                }

                var isExistingSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                    predicate: x => x.SlotId == dto.SlotId 
                    );
                if (!isExistingSlot)
                {

                    throw new Exception("Slot không tồn tại.");
                }

                var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                    predicate: x => x.TechnicianId == dto.TechnicianId && x.Date >= dto.FromDate && x.Date <= dto.ToDate && x.SlotId == dto.SlotId
                    );
                if (isDupWorkSlot)
                {
                    throw new Exception("Lịch làm việc đã tồn tại.");
                }

                var workSlots = new List<WorkSlot>();

                for (var date = dto.FromDate; date <= dto.ToDate; date = date.AddDays(1))
                {
                    workSlots.Add(new WorkSlot
                    {
                        TechnicianId = dto.TechnicianId,
                        Date = date,
                        SlotId = dto.SlotId,
                        Status = WorkSlotStatus.NotStarted
                    });
                }

                await _unitOfWork.GetRepository<WorkSlot>().InsertRangeAsync(workSlots);
                await _unitOfWork.CommitAsync();

                return "Taọ lịch làm việc mới thành công.";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> CreateWorkSlotsDateSlotAsync(WorkSlotCreateDateSlotDto dto)
        {
            try
            {
                var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == dto.TechnicianId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                if (!isExistingTechnician)
                {
                    throw new Exception("Kĩ thuật viên không tồn tại.");
                }

                var workSlots = new List<WorkSlot>();

                foreach (var dateSlot in dto.DateSlots)
                {
                    var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                        predicate: x => x.SlotId == dateSlot.SlotId
                        );
                    if (slot == null)
                    {

                        throw new Exception("Slot không tồn tại.");
                    }

                    var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dateSlot.Date && x.SlotId == dateSlot.SlotId
                        );
                    if (isDupWorkSlot)
                    {
                        throw new Exception($"Lịch làm việc đã tồn tại (Slot {slot.FromTime} - {slot.ToTime} ngày {dateSlot.Date}).");
                    }

                    var isSameDay = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dateSlot.Date
                        );
                    if (isSameDay)
                    {
                        throw new Exception($"Không thể làm 2 slot chung 1 ngày ({dateSlot.Date}).");
                    }

                    var isContinueSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && 
                                        ((x.Date.AddDays(1) ==  dateSlot.Date && slot.FromTime == x.Slot.ToTime) ||
                                         (x.Date.AddDays(-1) == dateSlot.Date && slot.ToTime == x.Slot.FromTime)),
                        include: i => i.Include(x => x.Slot)
                        );
                    if (isContinueSlot)
                    {
                        throw new Exception($"Không thể làm 2 slot liên tiếp 1 ngày (Slot {slot.FromTime} - {slot.ToTime} ngày {dateSlot.Date}).");
                    }

                    workSlots.Add(new WorkSlot
                    {
                        TechnicianId = dto.TechnicianId,
                        Date = dateSlot.Date,
                        SlotId = dateSlot.SlotId,
                        Status = WorkSlotStatus.NotStarted
                    });
                }

                await _unitOfWork.GetRepository<WorkSlot>().InsertRangeAsync(workSlots);
                await _unitOfWork.CommitAsync();

                return "Taọ lịch làm việc mới thành công.";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> UpdateWorkSlotAsync(int id, WorkSlotUpdateDto dto)
        {
            try
            {
                var workSlot = await _unitOfWork.GetRepository<WorkSlot>().SingleOrDefaultAsync(
                    predicate: x => x.WorkSlotId == id
                    );
                if (workSlot == null)
                {
                    throw new KeyNotFoundException("lịch làm việc không tồn tại.");
                }

                var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == dto.TechnicianId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                if (!isExistingTechnician)
                {
                    throw new Exception("Kĩ thuật viên không tồn tại.");
                }

                var isExistingSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                    predicate: x => x.SlotId == dto.SlotId
                    );
                if (!isExistingSlot)
                {

                    throw new Exception("Slot không tồn tại.");
                }

                var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                    predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dto.Date && x.SlotId == dto.SlotId);
                if (isDupWorkSlot)
                {
                    throw new Exception("Lịch làm việc đã tồn tại.");
                }

                _mapper.Map(dto, workSlot);
                _unitOfWork.GetRepository<WorkSlot>().UpdateAsync(workSlot);
                await _unitOfWork.CommitAsync();
                return "Cập nhật lịch làm việc thành công.";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<string> DeleteWorkSlotAsync(int id)
        {
            try
            {
                var workSlot = await _unitOfWork.GetRepository<WorkSlot>().SingleOrDefaultAsync(
                    predicate: x => x.WorkSlotId == id
                    );

                if (workSlot == null)
                {
                    throw new KeyNotFoundException("lịch làm việc không tồn tại.");
                }

                _unitOfWork.GetRepository<WorkSlot>().DeleteAsync(workSlot);
                await _unitOfWork.CommitAsync();
                return "Xóa lịch làm việc thành công.";
            }
            catch (Exception e)
            {
                throw new Exception($"Lỗi hệ thống: {e.Message}");
            }
        }

        public async Task<IEnumerable<WorkSlotDto>> GetTechnicianScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate, WorkSlotStatus? status)
        {
            if (technicianId.HasValue)
            {
                var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == technicianId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                if (!isExistingTechnician)
                {
                    throw new Exception("Kĩ thuật viên không tồn tại.");
                }
            }

            if (fromDate > toDate)
            {
                throw new Exception("'Từ ngày' phải nhỏ hơn hoặc bằng 'Đến ngày'.");
            }

            var workSlots = await _unitOfWork.GetRepository<WorkSlot>().GetListAsync(
                predicate: x => x.Date >= fromDate && x.Date <= toDate
                                && (!status.HasValue || x.Status == status)
                                && (!technicianId.HasValue || x.TechnicianId == technicianId),
                include: i => i.Include(x => x.Technician)
                               .Include(x => x.Slot)
            );

            if (workSlots == null || !workSlots.Any())
                return Enumerable.Empty<WorkSlotDto>();

            var result = workSlots
                            .GroupBy(w => w.Date)
                            .Select(dateGroup => new WorkSlotDto
                            {
                                Date = dateGroup.Key,
                                Slots = dateGroup
                                    .GroupBy(s => s.SlotId)
                                    .Select(slotGroup => new SlotDto
                                    {
                                        SlotId = slotGroup.Key,
                                        TechnicianWorkSlots = slotGroup
                                            .Select(ws => _mapper.Map<TechnicianWorkSlotDto>(ws))
                                            .ToList()
                                    })
                                    .ToList()
                            })
                            .OrderBy(x => x.Date)
                            .ToList();

            return result;
        }

        public async Task<IEnumerable<WorkSlotDto>> GetMyScheduleAsync(DateOnly fromDate, DateOnly toDate, WorkSlotStatus? status)
        {
            int technicianId = _userContext.CurrentUserId;
            return await GetTechnicianScheduleAsync(technicianId, fromDate, toDate, status);
        }
    }
}
