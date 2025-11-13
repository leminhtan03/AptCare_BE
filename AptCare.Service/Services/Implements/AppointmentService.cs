using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Linq;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class AppointmentService : BaseService<AppointmentService>, IAppointmentService
    {
        private readonly IUserContext _userContext;
        private readonly INotificationService _notificationService;
        private readonly IRepairRequestService _repairRequestService;

        public AppointmentService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<AppointmentService> logger,
            IMapper mapper,
            IUserContext userContext,
            INotificationService notificationService,
            IRepairRequestService IRepairRequestService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _notificationService = notificationService;
            _repairRequestService = IRepairRequestService;
        }

        public async Task<string> CreateAppointmentAsync(AppointmentCreateDto dto)
        {
            var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                        predicate: x => x.RepairRequestId == dto.RepairRequestId
                        );
            if (!isExistingRepairRequest)
            {
                throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
            }
            if (dto.StartTime >= dto.EndTime)
            {
                throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            var appointment = _mapper.Map<Appointment>(dto);
            await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Tạo lịch hẹn thành công";
        }

        public async Task<string> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                predicate: x => x.AppointmentId == id,
                include: i => i.Include(x => x.AppointmentAssigns)
                );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            if (appointment.StartTime != dto.StartTime || appointment.EndTime != dto.EndTime)
            {
                if (appointment.AppointmentAssigns.Any())
                {
                    throw new AppValidationException("Không thể thay đổi thời gian lịch hẹn khi đã phân công.");
                }
            }

            if (dto.StartTime >= dto.EndTime)
            {
                throw new AppValidationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            _mapper.Map(dto, appointment);
            _unitOfWork.GetRepository<Appointment>().UpdateAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Cập nhật lịch hẹn thành công";
        }

        public async Task<string> DeleteAppointmentAsync(int id)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                        predicate: x => x.AppointmentId == id
                        );
            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            _unitOfWork.GetRepository<Appointment>().DeleteAsync(appointment);
            await _unitOfWork.CommitAsync();
            return "Xóa lịch hẹn thành công";
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int id)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => p.AppointmentId == id,
                include: i => i.Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.Floor)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Issue)
                                .Include(x => x.AppointmentTrackings)
                                    .ThenInclude(x => x.UpdatedByUser)
                                .Include(x => x.InspectionReports)
                                    .ThenInclude(x => x.ReportApprovals)
                                .Include(x => x.InspectionReports)
                                    .ThenInclude(x => x.ReportApprovals)
                );

            if (appointment == null)
            {
                throw new AppValidationException("Tầng không tồn tại", StatusCodes.Status404NotFound);
            }

            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == appointment.RepairRequest.RepairRequestId
                    );
            appointment.RepairRequest.Medias = medias.ToList();

            return appointment;
        }

        public async Task<IPaginate<AppointmentDto>> GetPaginateAppointmentAsync(PaginateDto dto, DateOnly? fromDate, DateOnly? toDate, bool? isAprroved)
        {
            if (fromDate != null && toDate != null && fromDate > toDate)
                throw new AppValidationException("Ngày bắt đầu không thể sau ngày kết thúc");

            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Appointment, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Note.Contains(search)) &&
                (string.IsNullOrEmpty(filter) || filter.Equals(p.AppointmentTrackings.LastOrDefault().Status.ToString().ToLower())) &&
                (fromDate == null || DateOnly.FromDateTime(p.StartTime) >= fromDate) &&
                (toDate == null || DateOnly.FromDateTime(p.StartTime) <= toDate) &&
                (isAprroved == null ||
                    (isAprroved == true && p.RepairRequest.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status != RequestStatus.Pending) ||
                    (isAprroved == false && p.RepairRequest.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status == RequestStatus.Pending));

            var result = await _unitOfWork.GetRepository<Appointment>().GetPagingListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Issue)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.RequestTrackings)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.Floor)
                                .Include(x => x.AppointmentTrackings)
                                    .ThenInclude(x => x.UpdatedByUser),

                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            return result;
        }

        public async Task<IEnumerable<ResidentAppointmentScheduleDto>> GetResidentAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate)
        {
            var userId = _userContext.CurrentUserId;

            var appointments = await _unitOfWork.GetRepository<Appointment>().GetListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => DateOnly.FromDateTime(p.StartTime) >= fromDate &&
                                DateOnly.FromDateTime(p.StartTime) <= toDate &&
                                p.RepairRequest.Apartment.UserApartments.Any(ua => ua.UserId == userId),
                include: i => i.Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.UserApartments)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.Floor)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Issue)
                                .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.AppointmentTrackings)
                                    .ThenInclude(x => x.UpdatedByUser)
                );

            var result = appointments.GroupBy(a => DateOnly.FromDateTime(a.StartTime))
                                   .Select(x => new ResidentAppointmentScheduleDto
                                   {
                                       Date = x.Key,
                                       Appointments = x.ToList()
                                   });
            return result;
        }

        public async Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetTechnicianAppointmentScheduleAsync(int? technicianId, DateOnly fromDate, DateOnly toDate)
        {
            var appointments = await _unitOfWork.GetRepository<Appointment>().GetListAsync(
                selector: x => _mapper.Map<AppointmentDto>(x),
                predicate: p => DateOnly.FromDateTime(p.StartTime) >= fromDate &&
                                DateOnly.FromDateTime(p.StartTime) <= toDate &&
                                p.AppointmentTrackings.OrderByDescending(x => x.UpdatedAt).First().Status != AppointmentStatus.Pending &&
                                p.AppointmentTrackings.OrderByDescending(x => x.UpdatedAt).First().Status != AppointmentStatus.Assigned &&
                                (technicianId == null || p.AppointmentAssigns.Any(ua => ua.TechnicianId == technicianId)),
                include: i => i.Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Apartment)
                                        .ThenInclude(x => x.Floor)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.RequestTrackings)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.User)
                                .Include(x => x.RepairRequest)
                                    .ThenInclude(x => x.Issue)
                                .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                .Include(x => x.AppointmentTrackings)
                );

            var slots = await _unitOfWork.GetRepository<Slot>().GetListAsync(predicate: p => p.Status == ActiveStatus.Active);

            var result = appointments
                            .GroupBy(a => DateOnly.FromDateTime(a.StartTime))
                            .Select(dateGroup => new TechnicianAppointmentScheduleDto
                            {
                                Date = dateGroup.Key,
                                Slots = dateGroup
                                    .GroupBy(x => slots.FirstOrDefault(s => s.FromTime <= x.StartTime.TimeOfDay &&
                                                                            s.ToTime > x.StartTime.TimeOfDay).SlotId)
                                    .Select(slotGroup => new SlotAppointmentDto
                                    {
                                        SlotId = slotGroup.Key,
                                        Appointments = slotGroup
                                            .Select(x => x)
                                            .ToList()
                                    })
                                    .ToList()
                            })
                            .OrderBy(x => x.Date)
                            .ToList();
            return result;
        }

        public async Task<IEnumerable<TechnicianAppointmentScheduleDto>> GetMyTechnicianAppointmentScheduleAsync(DateOnly fromDate, DateOnly toDate)
        {
            int technicianId = _userContext.CurrentUserId;
            return await GetTechnicianAppointmentScheduleAsync(technicianId, fromDate, toDate);
        }

        private Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.AppointmentId);

            return sortBy.ToLower() switch
            {
                "start_time" => q => q.OrderBy(p => p.StartTime),
                "start_time_desc" => q => q.OrderByDescending(p => p.StartTime),
                _ => q => q.OrderByDescending(p => p.AppointmentId) // Default sort
            };
        }

        public async Task<bool> CheckInAsync(int id)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                    predicate: x => x.AppointmentId == id,
                    include: i => i.Include(x => x.AppointmentTrackings)
                                    .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                    .Include(x => x.RepairRequest)
                    );
                if (appointment == null)
                {
                    throw new AppValidationException("Lịch hẹn không tồn tại hoặc bạn không được phân công cho lịch hẹn này.", StatusCodes.Status404NotFound);
                }

                await _unitOfWork.BeginTransactionAsync();
                var timeNow = DateTime.Now;
                var appointmentStartTime = appointment.StartTime;
                //if (timeNow < appointmentStartTime.AddMinutes(-30))
                //{
                //    throw new AppValidationException("Chưa đến thời gian check-in cho lịch hẹn này.");
                //}

                var latestRequestStaus = await _unitOfWork.GetRepository<RequestTracking>().SingleOrDefaultAsync(
                    selector: s => s.Status,
                    predicate: x => x.RepairRequestId == appointment.RepairRequestId,
                    orderBy: o => o.OrderByDescending(x => x.UpdatedAt)
                    );

                if (latestRequestStaus != RequestStatus.InProgress)
                {
                    var StatusChange = await _repairRequestService.ToggleRepairRequestStatusAsync(new ToggleRRStatus
                    {
                        RepairRequestId = appointment.RepairRequestId,
                        NewStatus = RequestStatus.InProgress,
                        Note = "Kỹ thuật đã bắt đầu thực hiện yêu cầu sữa chữa."
                    });
                }

                var latestTracking = appointment.AppointmentTrackings
                                                .OrderByDescending(at => at.UpdatedAt)
                                                .FirstOrDefault();
                if (latestTracking != null && latestTracking.Status == AppointmentStatus.Confirmed)
                {
                    var appointmentTracking = new AppointmentTracking
                    {
                        AppointmentId = appointment.AppointmentId,
                        Status = AppointmentStatus.InVisit,
                        Note = "Kỹ thuật đã check-in.",
                        UpdatedBy = userId,
                        UpdatedAt = DateTime.Now
                    };
                    await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(appointmentTracking);
                    await _unitOfWork.CommitAsync();
                    var appointmentAssign = appointment.AppointmentAssigns.Where(aa => aa.Status != WorkOrderStatus.Cancel);
                    foreach (var assign in appointmentAssign)
                    {
                        if (assign != null)
                        {
                            assign.ActualStartTime = DateTime.Now;
                            assign.Status = WorkOrderStatus.Working;
                            _unitOfWork.GetRepository<AppointmentAssign>().UpdateAsync(assign);
                            await _unitOfWork.CommitAsync();
                        }
                    }
                    await _unitOfWork.CommitTransactionAsync();
                    return true;
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new AppValidationException("Lịch hẹn không ở trạng thái chưa được xác nhận phân công, không thể check-in.");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CheckInAsync for Appointment ID {AppointmentId}", id);
                throw new AppValidationException(ex.Message);
            }
        }

        public async Task<bool> StartRepairAsync(int id)
        {
            try
            {
                var userId = _userContext.CurrentUserId;
                var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                    predicate: x => x.AppointmentId == id,
                    include: i => i.Include(x => x.AppointmentTrackings)
                                    .Include(x => x.AppointmentAssigns)
                                    .ThenInclude(x => x.Technician)
                                    .Include(x => x.RepairRequest)
                                    .Include(x => x.InspectionReports)
                    );

                if (appointment == null)
                {
                    throw new AppValidationException(
                        "Lịch hẹn không tồn tại hoặc bạn không được phân công cho lịch hẹn này.",
                        StatusCodes.Status404NotFound);
                }

                var latestTracking = appointment.AppointmentTrackings
                                                .OrderByDescending(at => at.UpdatedAt)
                                                .FirstOrDefault();

                if (!IsValidStatusTransition(latestTracking.Status, AppointmentStatus.InRepair))
                {
                    throw new AppValidationException(
                        $"Không thể bắt đầu sửa chữa từ trạng thái '{latestTracking?.Status}'. Yêu cầu phải ở trạng thái InVisit hoặc AwaitingIRApproval.");
                }
                if (latestTracking.Status == AppointmentStatus.AwaitingIRApproval)
                {
                    if (appointment.InspectionReports.OrderByDescending(x => x.CreatedAt)
                                                        .FirstOrDefault().Status == ReportStatus.Approved)
                    {
                        throw new AppValidationException("Báo cáo khảo sát chưa được chấp thuận.");
                    }
                }
                if (latestTracking.Status == AppointmentStatus.InVisit)
                {
                    var isValid = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                    predicate: p => p.RepairRequestId == appointment.RepairRequestId && 
                                    p.Appointments.Any(a => a.InspectionReports.OrderByDescending(x => x.CreatedAt)
                                                                               .FirstOrDefault().Status == ReportStatus.Approved),
                    include: i => i.Include(x => x.Appointments)
                                        .ThenInclude(x => x.InspectionReports)
                    );

                    if (!isValid)
                    {
                        throw new AppValidationException("Báo cáo khảo sát trước đó chưa được chấp thuận.");
                    } 
                }
                await _unitOfWork.BeginTransactionAsync();

                var appointmentTracking = new AppointmentTracking
                {
                    AppointmentId = appointment.AppointmentId,
                    Status = AppointmentStatus.InRepair,
                    Note = "Kỹ thuật viên bắt đầu sửa chữa.",
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.Now
                };
                await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(appointmentTracking);
                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error during StartRepairAsync for Appointment ID {AppointmentId}", id);
                throw;
            }
        }

        public async Task<bool> ToogleAppoimnentStatus(int Id, string note, AppointmentStatus appointmentStatus)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                predicate: x => x.AppointmentId == Id,
                include: i => i.Include(x => x.AppointmentTrackings)
            );

            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            var latestTracking = appointment.AppointmentTrackings
                                            .OrderByDescending(at => at.UpdatedAt)
                                            .FirstOrDefault();

            var currentStatus = latestTracking?.Status ?? AppointmentStatus.Pending;

            if (!IsValidStatusTransition(currentStatus, appointmentStatus))
            {
                throw new AppValidationException(
                    $"Không thể chuyển trạng thái từ '{currentStatus}' sang '{appointmentStatus}'.",
                    StatusCodes.Status400BadRequest);
            }

            var userId = _userContext.CurrentUserId;

            var appointmentTracking = new AppointmentTracking
            {
                AppointmentId = Id,
                Status = appointmentStatus,
                Note = note ?? string.Empty,
                UpdatedBy = userId,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(appointmentTracking);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<string> CompleteAppointmentAsync(int id, string note, bool hasNextAppointment)
        {
            var appointment = await _unitOfWork.GetRepository<Appointment>().SingleOrDefaultAsync(
                predicate: x => x.AppointmentId == id,
                include: i => i.Include(x => x.AppointmentTrackings)
                               .Include(x => x.AppointmentAssigns)
                               .Include(x => x.InspectionReports)
                                    .ThenInclude(x => x.ReportApprovals)
                               .Include(x => x.RepairReport)
            );

            if (appointment == null)
            {
                throw new AppValidationException("Lịch hẹn không tồn tại.", StatusCodes.Status404NotFound);
            }

            var latestTracking = appointment.AppointmentTrackings
                                            .OrderByDescending(at => at.UpdatedAt)
                                            .FirstOrDefault();

            var currentStatus = latestTracking?.Status ?? AppointmentStatus.Pending;

            if (!IsValidStatusTransition(currentStatus, AppointmentStatus.Completed))
            {
                throw new AppValidationException(
                    $"Không thể chuyển trạng thái từ '{currentStatus}' sang 'Complete'.",
                    StatusCodes.Status400BadRequest);
            }

            if (currentStatus == AppointmentStatus.InRepair)
            {
                if (appointment.RepairReport == null)
                {
                    throw new AppValidationException("Chưa có báo cáo hoàn thành.");
                }

                if (appointment.RepairReport.Status == ReportStatus.Approved)
                {
                    throw new AppValidationException("Báo cáo hoàn thành chưa được chấp thuận.");
                }
            }
            if (currentStatus == AppointmentStatus.AwaitingIRApproval)
            {
                if (appointment.InspectionReports != null && appointment.InspectionReports.Any())
                {
                    if (!appointment.InspectionReports.OrderByDescending(x => x.CreatedAt)
                                                     .FirstOrDefault().ReportApprovals.Any(ra =>
                                                            ra.Role == AccountRole.TechnicianLead && ra.Status == ReportStatus.Approved))
                    {
                        throw new AppValidationException("Báo cáo khảo sát chưa được trưởng kĩ thuật viên chấp thuận.");
                    }
                }
            }

            var userId = _userContext.CurrentUserId;

            var appointmentTracking = new AppointmentTracking
            {
                AppointmentId = id,
                Status = AppointmentStatus.Completed,
                Note = note ?? string.Empty,
                UpdatedBy = userId,
                UpdatedAt = DateTime.Now
            };

            if (hasNextAppointment)
            {
                await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(new RequestTracking
                {
                    RepairRequestId = appointment.RepairRequestId,
                    Status = RequestStatus.Scheduling,
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.Now
                });
            }

            await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(appointmentTracking);

            var appointmentAssign = appointment.AppointmentAssigns.Where(aa => aa.Status == WorkOrderStatus.Working);
            foreach (var assign in appointmentAssign)
            {
                if (assign != null)
                {
                    assign.ActualEndTime = DateTime.Now;
                    assign.Status = WorkOrderStatus.Completed;
                    _unitOfWork.GetRepository<AppointmentAssign>().UpdateAsync(assign);
                }
            }
            await _unitOfWork.CommitAsync();

            return "Lịch hẹn đã được hoàn thành";
        }

        private bool IsValidStatusTransition(AppointmentStatus currentStatus, AppointmentStatus newStatus)
        {
            var validNextStatuses = currentStatus switch
            {
                AppointmentStatus.Pending => new[]
                {
                    AppointmentStatus.Assigned,
                    AppointmentStatus.Cancelled
                },
                AppointmentStatus.Assigned => new[]
                {
                    AppointmentStatus.Confirmed,
                    AppointmentStatus.Cancelled,
                },
                AppointmentStatus.Confirmed => new[]
                {
                    AppointmentStatus.InVisit,
                    AppointmentStatus.Cancelled
                },
                AppointmentStatus.InVisit => new[]
                {
                    AppointmentStatus.AwaitingIRApproval,
                    AppointmentStatus.InRepair,
                    AppointmentStatus.Cancelled
                },
                AppointmentStatus.AwaitingIRApproval => new[]
                {
                    AppointmentStatus.InRepair,
                    AppointmentStatus.Completed
                },
                AppointmentStatus.InRepair => new[]
                {
                    AppointmentStatus.Completed
                },
                AppointmentStatus.Completed => Array.Empty<AppointmentStatus>(),
                AppointmentStatus.Cancelled => Array.Empty<AppointmentStatus>(),
                _ => Array.Empty<AppointmentStatus>()
            };
            return validNextStatuses.Contains(newStatus);
        }
    }
}
