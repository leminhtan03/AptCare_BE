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
using AptCare.Service.Exceptions;
using AptCare.Service.Dtos.AppointmentDtos;

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
                    throw new AppValidationException("Kĩ thuật viên không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (dto.FromDate > dto.ToDate)
                {
                    throw new AppValidationException("'Từ ngày' phải nhỏ hơn hoặc bằng 'Đến ngày'");
                }

                var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                    predicate: x => x.SlotId == dto.SlotId 
                    );
                if (slot == null)
                {

                    throw new AppValidationException("Slot không tồn tại.", StatusCodes.Status404NotFound);
                }

                var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                    predicate: x => x.TechnicianId == dto.TechnicianId && x.Date >= dto.FromDate && x.Date <= dto.ToDate && x.SlotId == dto.SlotId
                    );
                if (isDupWorkSlot)
                {
                    throw new AppValidationException("Lịch làm việc đã tồn tại.");
                }

                var workSlots = new List<WorkSlot>();

                for (var date = dto.FromDate; date <= dto.ToDate; date = date.AddDays(1))
                {
                    var isSameDay = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == date
                        );
                    if (isSameDay)
                    {
                        throw new AppValidationException($"Không thể làm 2 slot chung 1 ngày ({date}).");
                    }

                    var isContinueSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId &&
                                        ((x.Date.AddDays(1) == date && slot.FromTime == x.Slot.ToTime) ||
                                         (x.Date.AddDays(-1) == date && slot.ToTime == x.Slot.FromTime)),
                        include: i => i.Include(x => x.Slot)
                        );
                    if (isContinueSlot)
                    {
                        throw new AppValidationException($"Không thể làm 2 slot liên tiếp 1 ngày (Slot {slot.FromTime} - {slot.ToTime} ngày {date}).");
                    }

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
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
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
                    throw new AppValidationException("Kĩ thuật viên không tồn tại.", StatusCodes.Status404NotFound);
                }

                var workSlots = new List<WorkSlot>();

                foreach (var dateSlot in dto.DateSlots)
                {
                    var slot = await _unitOfWork.GetRepository<Slot>().SingleOrDefaultAsync(
                        predicate: x => x.SlotId == dateSlot.SlotId
                        );
                    if (slot == null)
                    {

                        throw new AppValidationException("Slot không tồn tại.", StatusCodes.Status404NotFound);
                    }

                    var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dateSlot.Date && x.SlotId == dateSlot.SlotId
                        );
                    if (isDupWorkSlot)
                    {
                        throw new AppValidationException($"Lịch làm việc đã tồn tại (Slot {slot.FromTime} - {slot.ToTime} ngày {dateSlot.Date}).");
                    }

                    var isSameDay = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dateSlot.Date
                        );
                    if (isSameDay)
                    {
                        throw new AppValidationException($"Không thể làm 2 slot chung 1 ngày ({dateSlot.Date}).");
                    }

                    var isContinueSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                        predicate: x => x.TechnicianId == dto.TechnicianId && 
                                        ((x.Date.AddDays(1) ==  dateSlot.Date && slot.FromTime == x.Slot.ToTime) ||
                                         (x.Date.AddDays(-1) == dateSlot.Date && slot.ToTime == x.Slot.FromTime)),
                        include: i => i.Include(x => x.Slot)
                        );
                    if (isContinueSlot)
                    {
                        throw new AppValidationException($"Không thể làm 2 slot liên tiếp 1 ngày (Slot {slot.FromTime} - {slot.ToTime} ngày {dateSlot.Date}).");
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
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
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
                    throw new AppValidationException("lịch làm việc không tồn tại.", StatusCodes.Status404NotFound);
                }

                var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == dto.TechnicianId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                if (!isExistingTechnician)
                {
                    throw new AppValidationException("Kĩ thuật viên không tồn tại.", StatusCodes.Status404NotFound);
                }

                var isExistingSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                    predicate: x => x.SlotId == dto.SlotId
                    );
                if (!isExistingSlot)
                {

                    throw new AppValidationException("Slot không tồn tại.", StatusCodes.Status404NotFound);
                }

                var isDupWorkSlot = await _unitOfWork.GetRepository<WorkSlot>().AnyAsync(
                    predicate: x => x.TechnicianId == dto.TechnicianId && x.Date == dto.Date && x.SlotId == dto.SlotId);
                if (isDupWorkSlot)
                {
                    throw new AppValidationException("Lịch làm việc đã tồn tại.");
                }

                _mapper.Map(dto, workSlot);
                _unitOfWork.GetRepository<WorkSlot>().UpdateAsync(workSlot);
                await _unitOfWork.CommitAsync();
                return "Cập nhật lịch làm việc thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
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
                    throw new AppValidationException("lịch làm việc không tồn tại.", StatusCodes.Status404NotFound);
                }

                _unitOfWork.GetRepository<WorkSlot>().DeleteAsync(workSlot);
                await _unitOfWork.CommitAsync();
                return "Xóa lịch làm việc thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
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
                    throw new AppValidationException("Kĩ thuật viên không tồn tại.", StatusCodes.Status404NotFound);
                }
            }

            if (fromDate > toDate)
            {
                throw new AppValidationException("'Từ ngày' phải nhỏ hơn hoặc bằng 'Đến ngày'.");
            }

            var workSlots = await _unitOfWork.GetRepository<WorkSlot>().GetListAsync(
                predicate: x => x.Date >= fromDate && x.Date <= toDate
                                && (!status.HasValue || x.Status == status)
                                && (!technicianId.HasValue || x.TechnicianId == technicianId),
                include: i => i.Include(x => x.Technician)
                                    .ThenInclude(x => x.AppointmentAssigns)
                                        .ThenInclude(x => x.Appointment)
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
                                    .Select(slotGroup => new SlotWorkDto
                                    {
                                        SlotId = slotGroup.Key,
                                        TechnicianWorkSlots = slotGroup
                                            .Select(ws => new TechnicianWorkSlotDto
                                            {
                                                WorkSlotId = ws.WorkSlotId,
                                                Status = ws.Status.ToString(),
                                                Technician = _mapper.Map<UserBasicDto>(ws.Technician),
                                                Appointments = ws.Technician.AppointmentAssigns.Select(aa => _mapper.Map<AppointmentDto>(aa.Appointment)).Where(aa => DateOnly.FromDateTime(aa.StartTime) == dateGroup.Key).ToList()
                                            })
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

        public async Task<string> CheckInAsync(DateOnly date, int slotId)
        {
            var userId = _userContext.CurrentUserId;
            var workSlot = await _unitOfWork.GetRepository<WorkSlot>().SingleOrDefaultAsync(
                predicate: x => x.Date == date && x.SlotId == slotId && x.TechnicianId == userId                
            );
            if (workSlot == null)
            {
                throw new AppValidationException("Lịch làm việc không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (workSlot.Status != WorkSlotStatus.NotStarted)
            {
                throw new AppValidationException($"Trạng thái lịch làm việc là {workSlot.Status}.");
            }

            workSlot.Status = WorkSlotStatus.Working;

            _unitOfWork.GetRepository<WorkSlot>().UpdateAsync(workSlot);
            await _unitOfWork.CommitAsync();
            return "Điểm danh đầu giờ thành công.";
        }

        public async Task<string> CheckOutAsync(DateOnly date, int slotId)
        {
            var userId = _userContext.CurrentUserId;
            var workSlot = await _unitOfWork.GetRepository<WorkSlot>().SingleOrDefaultAsync(
                predicate: x => x.Date == date && x.SlotId == slotId && x.TechnicianId == userId
            );
            if (workSlot == null)
            {
                throw new AppValidationException("lịch làm việc không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (workSlot.Status != WorkSlotStatus.Working)
            {
                throw new AppValidationException($"Trạng thái lịch làm việc là {workSlot.Status}.");
            }

            workSlot.Status = WorkSlotStatus.Working;

            _unitOfWork.GetRepository<WorkSlot>().UpdateAsync(workSlot);
            await _unitOfWork.CommitAsync();
            return "Điểm danh cuối giờ thành công.";
        }
    }
}
