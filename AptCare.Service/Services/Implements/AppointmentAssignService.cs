using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.AppointmentAssignDtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class AppointmentAssignService : BaseService<AppointmentAssignService>, IAppointmentAssignService
    {
        private readonly IUserContext _userContext;
        private readonly INotificationService _notificationService;

        public AppointmentAssignService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<AppointmentAssignService> logger,
            IMapper mapper,
            INotificationService notificationService,
            IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _notificationService = notificationService;
        }

        public async Task<string> AssignAppointmentAsync(int appointmentId, IEnumerable<int> userIds)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                    predicate: x => x.AppointmentId == appointmentId,
                    include: i => i.Include(x => x.AppointmentTrackings)
                    );
                if (appointment == null)
                {
                    throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (appointment.EndTime == null)
                {
                    throw new AppValidationException("Lịch hẹn chưa có thời gian kết thúc.");
                }

                var lastStatus = appointment.AppointmentTrackings?.OrderByDescending(x => x.UpdatedAt).FirstOrDefault()?.Status;
                if (lastStatus != AppointmentStatus.Pending && lastStatus != AppointmentStatus.Assigned)
                {
                    throw new AppValidationException($"Lịch hẹn đang ở trạng thái {lastStatus}. Không thể phân công.");
                }

                foreach (var userId in userIds)
                {
                    var isExistingTechnician = await _unitOfWork.GetRepository<User>().AnyAsync(
                    predicate: x => x.UserId == userId && x.Account.Role == AccountRole.Technician,
                    include: i => i.Include(x => x.Account)
                    );
                    if (!isExistingTechnician)
                    {
                        throw new AppValidationException($"Kĩ thuật viên có ID {userId} không tồn tại.", StatusCodes.Status404NotFound);
                    }

                    var isExistingAppoAssign = await _unitOfWork.GetRepository<AppointmentAssign>().AnyAsync(
                        predicate: x => x.TechnicianId == userId && x.AppointmentId == appointmentId
                        );
                    if (isExistingAppoAssign)
                    {
                        throw new AppValidationException($"Kĩ thuật viên có ID {userId} đã được phân công cho lịch hẹn này.");
                    }

                    var isConflictAppoAssign = await _unitOfWork.GetRepository<AppointmentAssign>().AnyAsync(
                        predicate: x => x.TechnicianId == userId && DateOnly.FromDateTime(x.EstimatedStartTime) ==
                                                                         DateOnly.FromDateTime(appointment.StartTime) &&
                                                                    x.Status != WorkOrderStatus.Cancel &&
                                                                    ((x.EstimatedStartTime >= appointment.StartTime &&
                                                                        x.EstimatedStartTime <= appointment.EndTime) ||
                                                                     (x.EstimatedEndTime >= appointment.StartTime &&
                                                                        x.EstimatedEndTime <= appointment.EndTime))
                        );
                    if (isConflictAppoAssign)
                    {
                        throw new AppValidationException($"Kĩ thuật viên có ID {userId} bị mâu thuẫn lịch.");
                    }

                    if (lastStatus == AppointmentStatus.Pending)
                    {
                        await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(new AppointmentTracking
                        {
                            AppointmentId = appointmentId,
                            Status = AppointmentStatus.Assigned,
                            UpdatedAt = DateTime.Now,
                            UpdatedBy = _userContext.CurrentUserId,
                            Note = "Kỹ thuật viên trưởng đang phân công Kỹ thuật viên."
                        });
                    }

                    await _unitOfWork.GetRepository<AppointmentAssign>().InsertAsync(new AppointmentAssign
                    {
                        TechnicianId = userId,
                        AppointmentId = appointmentId,
                        EstimatedStartTime = appointment.StartTime,
                        EstimatedEndTime = (DateTime)appointment.EndTime,
                        AssignedAt = DateTime.Now,
                        Status = WorkOrderStatus.Pending
                    });
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return "Phân công thành công.";

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Phân công thất bại. Lỗi: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<string> UpdateAppointmentAssignAsync(int id, AppointmentAssignUpdateDto dto)
        {
            var userId = _userContext.CurrentUserId;

            var appoAssign = await _unitOfWork.GetRepository<AppointmentAssign>().SingleOrDefaultAsync(
                predicate: x => x.AppointmentAssignId == id
                );
            if (appoAssign == null)
            {
                throw new AppValidationException("Lịch phân công không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (appoAssign.TechnicianId != userId)
            {
                throw new AppValidationException("Không thể cập nhật lịch phân công không phải của mình.", StatusCodes.Status404NotFound);
            }

            _mapper.Map(dto, appoAssign);
            _unitOfWork.GetRepository<AppointmentAssign>().UpdateAsync(appoAssign);
            await _unitOfWork.CommitAsync();
            return "Cập nhật lịch phân công thành công.";
        }
        public async Task<IEnumerable<SuggestedTechnicianDto>> SuggestTechniciansForAppointment(int appointmentId, int? techniqueId)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
               predicate: x => x.AppointmentId == appointmentId,
               include: i => i.Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Issue)
                              .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.MaintenanceSchedule)
               );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (appointment.EndTime == null)
            {
                throw new AppValidationException("Lịch hẹn chưa có thời gian kết thúc.");
            }

            if (techniqueId != null)
            {
                var isExistingTechnique = await _unitOfWork.GetRepository<Technique>().AnyAsync(
                    predicate: x => x.TechniqueId == techniqueId
                    );
                if (!isExistingTechnique)
                {
                    throw new AppValidationException("Kĩ thuật không tồn tại.", StatusCodes.Status404NotFound);
                }
            }


            if (appointment.RepairRequest.MaintenanceScheduleId == null)
            {
                if (appointment.RepairRequest.Issue == null && techniqueId == null)
                {
                    throw new AppValidationException("Vui lòng chọn kĩ thuật để gợi ý.");
                }
                if (appointment.RepairRequest.Issue != null && techniqueId == null)
                {
                    techniqueId = appointment.RepairRequest.Issue.TechniqueId;
                }
            }
            else
            {
                if (techniqueId == null)
                {
                    techniqueId = appointment.RepairRequest.MaintenanceSchedule?.RequiredTechniqueId;
                }
            }

            Expression<Func<User, bool>> predicate;

            if (appointment.RepairRequest.IsEmergency)
            {
                predicate = p => p.Account.Role == AccountRole.Technician &&
                                p.TechnicianTechniques.Any(tt => tt.TechniqueId == techniqueId) &&
                                p.WorkSlots.Any(ws => ws.Status == WorkSlotStatus.Working) &&
                                p.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                 DateOnly.FromDateTime(appointment.StartTime))
                                                                 .All(aa => aa.Status != WorkOrderStatus.Working);
            }
            else
            {
                predicate = p => p.Account.Role == AccountRole.Technician &&
                                        p.TechnicianTechniques.Any(tt => tt.TechniqueId == techniqueId) &&
                                        p.WorkSlots.Any(ws => ws.Date == DateOnly.FromDateTime(appointment.StartTime) &&
                                            ws.Slot.FromTime <= appointment.StartTime.TimeOfDay &&
                                            ws.Slot.ToTime >= appointment.StartTime.TimeOfDay) &&
                                        p.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                         DateOnly.FromDateTime(appointment.StartTime) &&
                                                                         aa.Status != WorkOrderStatus.Cancel)
                                                            .All(aa => aa.EstimatedEndTime <= appointment.StartTime || aa.EstimatedStartTime >= appointment.EndTime);
            }

            var technicians = await _unitOfWork.GetRepository<User>().GetListAsync(
                    selector: s => new SuggestedTechnicianDto
                    {
                        UserId = s.UserId,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        PhoneNumber = s.PhoneNumber,
                        Email = s.Email,
                        Birthday = s.Birthday,
                        AssignCountThatDay = s.AppointmentAssigns.Count(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                              DateOnly.FromDateTime(appointment.StartTime) &&
                                                                              aa.Status != WorkOrderStatus.Cancel),
                        AssignCountThatMonth = s.AppointmentAssigns.Count(aa => aa.EstimatedStartTime.Month == appointment.StartTime.Month &&
                                                                                aa.Status != WorkOrderStatus.Cancel),
                        AppointmentsThatDay = s.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                               DateOnly.FromDateTime(appointment.StartTime) &&
                                                                               aa.Status != WorkOrderStatus.Cancel)
                                                                  .Select(aa => _mapper.Map<AppointmentDto>(aa.Appointment)).ToList()
                    },
                    predicate: predicate,
                    include: i => i.Include(x => x.Account)
                                   .Include(x => x.WorkSlots)
                                       .ThenInclude(x => x.Slot)
                                   .Include(x => x.AppointmentAssigns)
                                       .ThenInclude(x => x.Appointment)
                                            .ThenInclude(x => x.RepairRequest)
                                                .ThenInclude(x => x.Apartment)
                                   .Include(x => x.TechnicianTechniques)
                );

            foreach (var t in technicians)
            {
                var appointments = t.AppointmentsThatDay
                    ?.OrderBy(a => a.StartTime)
                    .ToList() ?? new List<AppointmentDto>();

                var previous = appointments
                    .Where(a => a.EndTime <= appointment.StartTime)
                    .OrderByDescending(a => a.EndTime)
                    .FirstOrDefault();

                var next = appointments
                    .Where(a => a.StartTime >= appointment.EndTime)
                    .OrderBy(a => a.StartTime)
                    .FirstOrDefault();

                t.GapFromPrevious = previous != null
                    ? (appointment.StartTime - (DateTime)previous.EndTime).TotalMinutes
                    : null;

                t.GapToNext = next != null
                    ? (next.StartTime - (DateTime)appointment.EndTime).TotalMinutes
                    : null;
            }

            var orderedTechnicians = technicians
                .OrderBy(t => t.AssignCountThatDay)
                    .ThenByDescending(t => (t.GapFromPrevious == null || t.GapFromPrevious > 30 ? 30 : t.GapFromPrevious) +
                                        (t.GapToNext == null || t.GapToNext > 30 ? 30 : t.GapToNext))
                        .ThenBy(t => t.AssignCountThatMonth)
                .ToList();

            return technicians;
        }

        public async Task<string> ConfirmAssignmentAsync(int appointmentId, bool isConfirmed)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var RRRepo = _unitOfWork.GetRepository<RepairRequest>();
                var AppoimentRepo = _unitOfWork.GetRepository<Appointment>();
                var AppoimentTrackingRepo = _unitOfWork.GetRepository<AppointmentTracking>();
                var AppoimentAssignRepo = _unitOfWork.GetRepository<AppointmentAssign>();
                var Appoiment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                    predicate: x => x.AppointmentId == appointmentId,
                    include: i => i.Include(x => x.AppointmentTrackings)
                                    .Include(x => x.AppointmentAssigns)
                    );
                if (Appoiment == null)
                {
                    throw new AppValidationException("Lịch phân công không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (isConfirmed)
                {
                    var AppoinmentTracking = Appoiment.AppointmentTrackings;
                    if (AppoinmentTracking.LastOrDefault().Status == AppointmentStatus.Pending)
                        throw new AppValidationException("Lịch hẹn chưa được phân công kỹ thuật viên nhất định", StatusCodes.Status400BadRequest);

                    var n = new AppointmentTracking
                    {
                        AppointmentId = appointmentId,
                        Status = AppointmentStatus.Confirmed,
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = _userContext.CurrentUserId,
                        Note = "Kỹ thuật viên trưởng đã xác nhận phân công Kỹ thuật viên."
                    };
                    await AppoimentTrackingRepo.InsertAsync(n);
                    await _unitOfWork.CommitAsync();

                    var dtoForTechnician = new NotificationPushRequestDto
                    {
                        Title = "Có yêu cầu sửa chữa mới được phân công cho bạn",
                        Type = NotificationType.Individual,
                        Description = $"Có yêu cầu sửa chữa mới được phân công cho bạn vào {Appoiment.StartTime.TimeOfDay} ngày {DateOnly.FromDateTime(Appoiment.StartTime)}",
                    };
                    await _notificationService.SendNotificationForTechnicianInAppointment(appointmentId, dtoForTechnician);

                    var dtoForResident = new NotificationPushRequestDto
                    {
                        Title = "Hệ thống",
                        Type = NotificationType.Individual,
                        Description = $"Có lịch hẹn yêu cầu sửa chữa vào {Appoiment.StartTime.TimeOfDay} ngày {DateOnly.FromDateTime(Appoiment.StartTime)}"
                    };
                    await _notificationService.SendNotificationForResidentInRequest(Appoiment.RepairRequestId, dtoForResident);
                }
                else
                {
                    var AppoinmentTracking = Appoiment.AppointmentTrackings;
                    var n = new AppointmentTracking
                    {
                        AppointmentId = appointmentId,
                        Status = AppointmentStatus.Pending,
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = _userContext.CurrentUserId,
                        Note = "Kỹ thuật viên trưởng đã hủy phân công Kỹ thuật viên."
                    };
                    await AppoimentTrackingRepo.InsertAsync(n);
                    await _unitOfWork.CommitAsync();
                    var appoAssigns = Appoiment.AppointmentAssigns;
                    foreach (var appoAssign in appoAssigns)
                    {
                        AppoimentAssignRepo.DeleteAsync(appoAssign);
                    }
                    await _unitOfWork.CommitAsync();
                }
                await _unitOfWork.CommitTransactionAsync();
                return isConfirmed ? "Xác nhận phân công thành công." : "Hủy phân công thành công.";

            }
            catch (Exception ex)
            {
                throw new AppValidationException($"Cập nhật phân công thất bại. Lỗi: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

        }

        public async Task<string> CancleAssignmentAsync(CancleAssignDto dto)
        {
            try
            {
                var assignToCancel = await _unitOfWork.GetRepository<AppointmentAssign>().SingleOrDefaultAsync(
                    predicate: x => x.TechnicianId == dto.technicanId && x.AppointmentId == dto.appointmentId
                    );
                if (assignToCancel == null)
                {
                    throw new AppValidationException("Lịch phân công không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (assignToCancel.Status == WorkOrderStatus.Working || assignToCancel.Status == WorkOrderStatus.Completed)
                {
                    throw new AppValidationException("Không thể hủy lịch phân công đang trong trạng thái Đang thực hiện hoặc Hoàn thành.", StatusCodes.Status400BadRequest);
                }
                _unitOfWork.GetRepository<AppointmentAssign>().DeleteAsync(assignToCancel);
                await _unitOfWork.CommitAsync();
                return "Hủy phân công thành công.";
            }
            catch (Exception ex)
            {
                throw new AppValidationException($"Hủy phân công thất bại. Lỗi: {ex.Message}", StatusCodes.Status500InternalServerError);

            }
        }
    }
}
