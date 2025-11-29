using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements.RabbitMQ;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class RepairRequestService : BaseService<RepairRequestService>, IRepairRequestService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IAppointmentAssignService _appointmentAssignService;
        private readonly INotificationService _notificationService;
        private readonly IRabbitMQService _rabbitMQService;

        public RepairRequestService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<RepairRequestService> logger,
            IMapper mapper,
            IUserContext userContext,
            ICloudinaryService cloudinaryService,
            IAppointmentAssignService appointmentAssignService,
            INotificationService notificationService,
            IRabbitMQService rabbitMQService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _appointmentAssignService = appointmentAssignService;
            _notificationService = notificationService;
            _rabbitMQService = rabbitMQService;
        }

        public async Task<string> CreateNormalRepairRequestAsync(RepairRequestNormalCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var isExistingApartment = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                        predicate: x => x.ApartmentId == dto.ApartmentId
                        );
                if (!isExistingApartment)
                {
                    throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (_userContext.IsResident)
                {
                    var isResidentInApartment = await _unitOfWork.GetRepository<UserApartment>().AnyAsync(
                        predicate: x => x.ApartmentId == dto.ApartmentId && x.UserId == userId
                        );
                    if (!isResidentInApartment)
                    {
                        throw new AppValidationException("Người dùng không thuộc căn hộ này.", StatusCodes.Status404NotFound);
                    }
                }

                if (dto.ParentRequestId != null)
                {
                    var isExistingParentRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                        predicate: x => x.RepairRequestId == dto.ParentRequestId
                        );
                    if (!isExistingParentRequest)
                    {
                        throw new AppValidationException("Yêu cầu sửa chữa cũ không tồn tại.", StatusCodes.Status404NotFound);
                    }
                }

                Issue issue = null;

                if (dto.IssueId != null)
                {
                    issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(
                        predicate: x => x.IssueId == dto.IssueId
                        );
                    if (issue == null)
                    {
                        throw new AppValidationException("Vấn đề không tồn tại.", StatusCodes.Status404NotFound);
                    }
                    if (issue.IsEmergency)
                    {
                        throw new AppValidationException("Đây là Vấn đề khẩn cấp.");
                    }

                    if (issue.EstimatedDuration > 5)
                    {
                        issue.EstimatedDuration = 5;
                    }

                    var isSuitableTimeSlot = await _unitOfWork.GetRepository<Slot>().AnyAsync(
                        predicate: p => p.FromTime <= dto.PreferredAppointment.TimeOfDay &&
                                        p.ToTime >= dto.PreferredAppointment.AddHours(issue.EstimatedDuration).TimeOfDay
                        );

                    if (!isSuitableTimeSlot)
                    {
                        throw new AppValidationException("Thời gian sửa chữa không nằm trong ca làm việc của kỹ thuật viên, vui lòng chọn thời gian khác.");
                    }
                }

                var request = _mapper.Map<RepairRequest>(dto);
                request.UserId = userId;

                await _unitOfWork.GetRepository<RepairRequest>().InsertAsync(request);
                await _unitOfWork.CommitAsync();

                await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(new RequestTracking
                {
                    RepairRequestId = request.RepairRequestId,
                    Status = RequestStatus.Pending,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = userId
                });

                if (dto.Files != null)
                {

                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                        {
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);
                        }

                        await _unitOfWork.GetRepository<Media>().InsertAsync(new Media
                        {
                            Entity = nameof(RepairRequest),
                            EntityId = request.RepairRequestId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active

                        });
                    }
                }

                var appointment = new Appointment
                {
                    RepairRequestId = request.RepairRequestId,
                    StartTime = dto.PreferredAppointment,
                    EndTime = issue == null ? null : dto.PreferredAppointment.AddHours(issue.EstimatedDuration),
                    Note = dto.Note,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
                await _unitOfWork.CommitAsync();

                var newAppoTracking = new AppointmentTracking
                {
                    AppointmentId = appointment.AppointmentId,
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.Now,
                    Status = AppointmentStatus.Pending,
                    Note = "Cuộc hẹn mong muốn của khách hàng chờ được phân công"
                };
                await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(newAppoTracking);
                await _unitOfWork.CommitAsync();

                bool isAssigned = false;

                if (issue != null)
                {
                    isAssigned = await AssignTechnicianForNormalAppointmentAsync(appointment, issue);
                    if (isAssigned)
                    {
                        await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(new AppointmentTracking
                        {
                            AppointmentId = appointment.AppointmentId,
                            UpdatedBy = userId,
                            Status = AppointmentStatus.Assigned,
                            UpdatedAt = DateTime.Now,
                            Note = "Tự động phân công cho lịch hẹn với Id: " + appointment.AppointmentId.ToString()

                        });
                        await _unitOfWork.CommitAsync();
                    }
                }

                var techLeadId = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        selector: s => s.UserId,
                        predicate: p => p.Account.Role == AccountRole.TechnicianLead,
                        include: i => i.Include(x => x.Account)
                    );

                List<int> userIds = new List<int>();
                userIds.Add(techLeadId);


                await _rabbitMQService.PublishNotificationAsync(new NotificationPushRequestDto
                {
                    Title = "Có yêu cầu sửa chửa mới",
                    Type = NotificationType.Individual,
                    Description = isAssigned ? "Có 1 yêu cầu sữa chữa mới cần xác nhận." : "Có 1 yêu cầu sữa chữa mới cần phân công.",
                    UserIds = userIds
                });

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return "Tạo yêu cầu sửa chữa thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<bool> AssignTechnicianForNormalAppointmentAsync(Appointment appointment, Issue issue)
        {
            var techniciansAcceptable = await _appointmentAssignService.SuggestTechniciansForAppointment(appointment.AppointmentId, null);
            var technicianidsAcceptable = techniciansAcceptable.Select(x => x.UserId).ToList();

            if (technicianidsAcceptable.Count < issue.RequiredTechnician)
            {
                return false;
            }

            var technicianIds = technicianidsAcceptable.Take(issue.RequiredTechnician);

            foreach (var technicianId in technicianIds)
            {
                await _unitOfWork.GetRepository<AppointmentAssign>().InsertAsync(new AppointmentAssign
                {
                    Appointment = appointment,
                    TechnicianId = technicianId,
                    AssignedAt = DateTime.Now,
                    EstimatedStartTime = appointment.StartTime,
                    EstimatedEndTime = (DateTime)appointment.EndTime,
                    Status = WorkOrderStatus.Pending
                });
            }
            return true;
        }

        public async Task<string> CreateEmergencyRepairRequestAsync(RepairRequestEmergencyCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var userId = _userContext.CurrentUserId;

                var isExistingApartment = await _unitOfWork.GetRepository<Apartment>().AnyAsync(
                        predicate: x => x.ApartmentId == dto.ApartmentId
                        );
                if (!isExistingApartment)
                {
                    throw new AppValidationException("Căn hộ không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (_userContext.IsResident)
                {
                    var isResidentInApartment = await _unitOfWork.GetRepository<UserApartment>().AnyAsync(
                        predicate: x => x.ApartmentId == dto.ApartmentId && x.UserId == userId
                        );
                    if (!isResidentInApartment)
                    {
                        throw new AppValidationException("Người dùng không thuộc căn hộ này.", StatusCodes.Status404NotFound);
                    }
                }

                var issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(
                    predicate: x => x.IssueId == dto.IssueId
                    );
                if (issue == null)
                {
                    throw new AppValidationException("Vấn đề không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (!issue.IsEmergency)
                {
                    throw new AppValidationException("Đây không phải là Vấn đề khẩn cấp.");
                }

                var request = _mapper.Map<RepairRequest>(dto);
                request.UserId = userId;

                await _unitOfWork.GetRepository<RepairRequest>().InsertAsync(request);
                await _unitOfWork.CommitAsync();

                await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(new RequestTracking
                {
                    RepairRequestId = request.RepairRequestId,
                    Status = RequestStatus.Pending,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = userId
                });

                if (dto.Files != null)
                {
                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                        {
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);
                        }

                        await _unitOfWork.GetRepository<Media>().InsertAsync(new Media
                        {
                            Entity = nameof(RepairRequest),
                            EntityId = request.RepairRequestId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active

                        });
                    }
                }

                var appointment = new Appointment
                {
                    RepairRequestId = request.RepairRequestId,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddHours(issue.EstimatedDuration),
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
                await _unitOfWork.CommitAsync();
                var newAppoTracking = new AppointmentTracking
                {
                    AppointmentId = appointment.AppointmentId,
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.Now,
                    Status = AppointmentStatus.Pending,
                    Note = "Cuộc hẹn sửa chữa khẩn cấp chờ được phân công"
                };
                await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(newAppoTracking);

                await AssignTechnicianForEmergencyAppointmentAsync(appointment, issue);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                return "Tạo yêu cầu sửa chữa khẩn cấp thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task AssignTechnicianForEmergencyAppointmentAsync(Appointment appointment, Issue issue)
        {
            var techniciansAcceptable = await _appointmentAssignService.SuggestTechniciansForAppointment(appointment.AppointmentId, null);
            var technicianidsAcceptable = techniciansAcceptable.Select(x => x.UserId).ToList();

            if (technicianidsAcceptable.Count != 0)
            {
                var technicianIds = technicianidsAcceptable.Take(issue.RequiredTechnician);

                foreach (var technicianId in technicianIds)
                {
                    await _unitOfWork.GetRepository<AppointmentAssign>().InsertAsync(new AppointmentAssign
                    {
                        Appointment = appointment,
                        TechnicianId = technicianId,
                        AssignedAt = DateTime.Now,
                        EstimatedStartTime = appointment.StartTime,
                        EstimatedEndTime = (DateTime)appointment.EndTime,
                        Status = WorkOrderStatus.Working
                    });

                    await _rabbitMQService.PublishNotificationAsync(new NotificationPushRequestDto
                    {
                        Title = "Có yêu cầu sửa chửa mới được giao",
                        Type = NotificationType.Individual,
                        Description = "Có yêu cầu sữa chữa khẩn cấp được giao cho bạn. Vui lòng tiến hành sữa chữa ngay.",
                        UserIds = technicianIds
                    });
                }
            }



            var ids = await _unitOfWork.GetRepository<User>().GetListAsync(
                        selector: s => s.UserId,
                        predicate: p => p.Account.Role == AccountRole.TechnicianLead || p.Account.Role == AccountRole.Manager,
                        include: i => i.Include(x => x.Account)
                    );

            if (technicianidsAcceptable.Count < issue.RequiredTechnician)
            {
                await _rabbitMQService.PublishNotificationAsync(new NotificationPushRequestDto
                {
                    Title = "Có yêu cầu sửa chửa khẩn cấp mới",
                    Type = NotificationType.Individual,
                    Description = $"Có yêu cầu sữa chữa khẩn cấp. Đã phân công được {technicianidsAcceptable.Count}/{issue.RequiredTechnician} kĩ thuật viên. Vui lòng tiếp tục phân công.",
                    UserIds = ids
                });
            }
            else
            {
                var newAppoTracking = new AppointmentTracking
                {
                    AppointmentId = appointment.AppointmentId,
                    UpdatedBy = _userContext.CurrentUserId,
                    UpdatedAt = DateTime.Now,
                    Status = AppointmentStatus.Assigned,
                    Note = "Cuộc hẹn sửa chữa khẩn cấp đã được phân công"
                };
                await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(newAppoTracking);
                await _unitOfWork.CommitAsync();

                await _rabbitMQService.PublishNotificationAsync(new NotificationPushRequestDto
                {
                    Title = "Có yêu cầu sửa chửa khẩn cấp mới",
                    Type = NotificationType.Individual,
                    Description = $"Có yêu cầu sữa chữa khẩn cấp. Đã phân công đủ {issue.RequiredTechnician}/{issue.RequiredTechnician} kĩ thuật viên.",
                    UserIds = ids
                });
            }
        }

        public async Task<IPaginate<RepairRequestDto>> GetPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? apartmentId, int? issueId, bool? isMaintain)
        {
            if (_userContext.IsResident)
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, _userContext.CurrentUserId, null, apartmentId, issueId, null);
            }
            else if (_userContext.IsTechnician)
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, null, _userContext.CurrentUserId, apartmentId, issueId, isMaintain);
            }
            else
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, null, null, apartmentId, issueId, isMaintain);
            }
        }

        private async Task<IPaginate<RepairRequestDto>> GetGenericPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? residentId, int? technicianId, int? apartmentId, int? issueId, bool? isMaintain)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            // Parse filter to RequestStatus enum if provided
            RequestStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<RequestStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            Expression<Func<RepairRequest, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Object.Contains(search) ||
                                         p.Description.Contains(search)) &&
                (filterStatus == null || p.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status == filterStatus) &&
                (isEmergency == null || p.IsEmergency == isEmergency) &&
                (residentId == null || p.Apartment.UserApartments.Any(x => x.UserId == residentId)) &&
                (technicianId == null ||
                    (p.IsEmergency == false && p.Appointments.Any(a => !new[]
                                                                        {
                                                                            AppointmentStatus.Pending,
                                                                            AppointmentStatus.Assigned,
                                                                            AppointmentStatus.Cancelled
                                                                        }.Contains(
                                                                            a.AppointmentTrackings
                                                                                .OrderByDescending(at => at.UpdatedAt)
                                                                                .Select(at => at.Status)
                                                                                .FirstOrDefault()
                                                                        )
                                                                        && a.AppointmentAssigns.Any(aa => aa.TechnicianId == technicianId)
                                                                    ) ||
                    (p.IsEmergency == true && p.Appointments.Any(a => a.AppointmentTrackings.OrderByDescending(at => at.UpdatedAt).First().Status != AppointmentStatus.Cancelled &&
                                                                       a.AppointmentAssigns.Any(aa => aa.TechnicianId == technicianId))))) &&
                (apartmentId == null || p.ApartmentId == apartmentId) &&
                (issueId == null || p.IssueId == issueId) &&
                (isMaintain == null || 
                    isMaintain == true && p.MaintenanceScheduleId != null || 
                    isMaintain == false && p.MaintenanceScheduleId == null);

            var result = await _unitOfWork.GetRepository<RepairRequest>().GetPagingListAsync(
                selector: x => _mapper.Map<RepairRequestDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.RequestTrackings)
                               .Include(x => x.ChildRequests)
                               .Include(x => x.Appointments)
                                    .ThenInclude(x => x.AppointmentAssigns)
                                .Include(x => x.Appointments)
                                    .ThenInclude(x => x.AppointmentTrackings)
                               .Include(x => x.Apartment)
                                    .ThenInclude(x => x.Floor)
                                .Include(x => x.Apartment)
                                    .ThenInclude(x => x.UserApartments)
                               .Include(x => x.MaintenanceSchedule)
                                    .ThenInclude(x => x.CommonAreaObject)
                                        .ThenInclude(x => x.CommonArea)
                                            .ThenInclude(x => x.Floor)
                               .Include(x => x.Issue)
                                    .ThenInclude(x => x.Technique),
                orderBy: BuildOrderBy(dto.sortBy),
                    page: page,
                    size: size
                );

            foreach (var request in result.Items)
            {
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == request.RepairRequestId
                    );
                request.Medias = medias.ToList();
            }
            return result;
        }

        public async Task<RepairRequestDetailDto> GetRepairRequestByIdAsync(int id)
        {
            if (_userContext.IsResident)
            {
                var isValid = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                predicate: p => p.RepairRequestId == id &&
                                p.Apartment.UserApartments.Any(x => x.UserId == _userContext.CurrentUserId),
                include: i => i.Include(x => x.Apartment)
                                    .ThenInclude(x => x.UserApartments)
                );
                if (!isValid)
                {
                    throw new AppValidationException("Bạn không có yêu cầu sửa chữa này.");
                }
            }
            if (_userContext.IsTechnician)
            {
                var isValid = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                predicate: p => p.RepairRequestId == id &&
                                p.Appointments.Any(a => a.AppointmentAssigns.Any(aa => aa.TechnicianId == _userContext.CurrentUserId)),
                include: i => i.Include(x => x.Appointments)
                                    .ThenInclude(x => x.AppointmentAssigns)
                );
                if (!isValid)
                {
                    throw new AppValidationException("Bạn không có yêu cầu sửa chữa này.");
                }
            }

            var result = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<RepairRequestDetailDto>(x),
                predicate: p => p.RepairRequestId == id,
                include: i => i.Include(x => x.RequestTrackings)
                            .ThenInclude(x => x.UpdatedByUser)
                       .Include(x => x.ParentRequest)
                       .Include(x => x.ChildRequests)
                       .Include(x => x.Appointments)
                            .ThenInclude(x => x.AppointmentAssigns)
                                .ThenInclude(x => x.Technician)
                       .Include(x => x.Appointments)
                            .ThenInclude(x => x.AppointmentTrackings)
                                .ThenInclude(x => x.UpdatedByUser)
                       .Include(x => x.User)
                       .Include(x => x.Apartment)
                            .ThenInclude(x => x.Floor)
                       .Include(x => x.MaintenanceSchedule)
                            .ThenInclude(x => x.CommonAreaObject)
                                .ThenInclude(x => x.CommonArea)
                                    .ThenInclude(x => x.Floor)
                       .Include(x => x.Issue)
                            .ThenInclude(x => x.Technique)
                );

            if (result == null)
            {
                throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
            }

            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: s => _mapper.Map<MediaDto>(s),
                predicate: p => p.Entity == nameof(RepairRequest) && p.EntityId == id
                );
            result.Medias = medias.ToList();

            return result;
        }

        public async Task<bool> ToggleRepairRequestStatusAsync(ToggleRRStatus dto)
        {
            try
            {
                var request = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    predicate: x => x.RepairRequestId == dto.RepairRequestId,
                    include: i => i.Include(x => x.RequestTrackings)
                                   .Include(x => x.Appointments)
                                       .ThenInclude(a => a.AppointmentAssigns)
                                   .Include(x => x.Appointments)
                                       .ThenInclude(a => a.AppointmentTrackings)
                );

                if (request == null)
                    throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);

                var userId = _userContext.CurrentUserId;
                var currentStatus = request.RequestTrackings
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefault()?.Status;

                if (!ValidateStatusTransition(currentStatus, dto.NewStatus))
                {
                    throw new AppValidationException("Chuyển trạng thái không hợp lệ từ " +
                        $"{currentStatus} sang {dto.NewStatus}.", StatusCodes.Status400BadRequest);
                }
                await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(new RequestTracking
                {
                    RepairRequestId = dto.RepairRequestId,
                    Status = dto.NewStatus,
                    UpdatedAt = DateTime.Now,
                    Note = dto.Note,
                    UpdatedBy = userId
                });

                if (dto.NewStatus == RequestStatus.Cancelled)
                {
                    var appointments = request.Appointments?.ToList();
                    await ToggleCancleRequestAsync(appointments, dto, userId);
                }
                await _unitOfWork.CommitAsync();
                await SendNotificationForUserApartment(dto.RepairRequestId, dto.NewStatus);

                _logger.LogInformation(
                    "Successfully toggled repair request {RepairRequestId} status from {OldStatus} to {NewStatus}",
                    dto.RepairRequestId,
                    currentStatus,
                    dto.NewStatus
                );

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error toggling repair request status for RepairRequestId: {RepairRequestId}", dto.RepairRequestId);
                throw new AppValidationException($"Lỗi hệ thống: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> ApprovalRepairRequestAsync(ToggleRRStatus dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                if (dto.NewStatus != RequestStatus.Approved && dto.NewStatus != RequestStatus.Cancelled && dto.NewStatus != RequestStatus.Scheduling)
                    throw new AppValidationException(
                        "Trạng thái mới phải là Approved hoặc Cancelled.",
                        StatusCodes.Status400BadRequest
                    );
                await ToggleRepairRequestStatusAsync(dto);
                await _unitOfWork.CommitTransactionAsync();
                return "Cập nhật thành công";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error toggling repair request status for RepairRequestId: {RepairRequestId}", dto.RepairRequestId);
                throw new AppValidationException($"Lỗi hệ thống: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private async Task ToggleCancleRequestAsync(List<Appointment>? appointments, ToggleRRStatus dto, int userId)
        {
            if (appointments != null && appointments.Any())
            {
                var appointmentTrackingRepo = _unitOfWork.GetRepository<AppointmentTracking>();
                var appointmentAssignRepo = _unitOfWork.GetRepository<AppointmentAssign>();

                foreach (var appointment in appointments)
                {
                    var currentAppointmentStatus = appointment.AppointmentTrackings?
                        .OrderByDescending(at => at.UpdatedAt)
                        .FirstOrDefault()?.Status;
                    if (currentAppointmentStatus != AppointmentStatus.Completed &&
                        currentAppointmentStatus != AppointmentStatus.Cancelled)
                    {
                        await appointmentTrackingRepo.InsertAsync(new AppointmentTracking
                        {
                            AppointmentId = appointment.AppointmentId,
                            Status = AppointmentStatus.Cancelled,
                            Note = dto.NewStatus == RequestStatus.Cancelled
                                ? $"Cuộc hẹn bị hủy do yêu cầu sửa chữa bị hủy. Lý do: {dto.Note}"
                                : $"Cuộc hẹn bị hủy do yêu cầu sửa chữa bị từ chối. Lý do: {dto.Note}",
                            UpdatedBy = userId,
                            UpdatedAt = DateTime.Now
                        });
                        if (appointment.AppointmentAssigns != null && appointment.AppointmentAssigns.Any())
                        {
                            appointmentAssignRepo.DeleteRangeAsync(appointment.AppointmentAssigns);

                            _logger.LogInformation(
                                "Deleted {Count} appointment assigns for appointment {AppointmentId} due to repair request {RepairRequestId} being {Status}",
                                appointment.AppointmentAssigns.Count,
                                appointment.AppointmentId,
                                dto.RepairRequestId,
                                dto.NewStatus
                            );
                        }
                    }
                }
            }
        }

        private bool ValidateStatusTransition(RequestStatus? currentStatus, RequestStatus newStatus)
        {
            var validNextStatuses = currentStatus switch
            {
                RequestStatus.Pending => new[]
                {
                    RequestStatus.Approved,
                    RequestStatus.Scheduling
                },
                RequestStatus.Approved => new[]
                {
                    RequestStatus.InProgress,
                    RequestStatus.Cancelled,
                    RequestStatus.Scheduling
                },
                RequestStatus.InProgress => new[]
                {
                    RequestStatus.Scheduling,
                    RequestStatus.Cancelled,
                    RequestStatus.AcceptancePendingVerify
                },
                RequestStatus.Scheduling => new[]
                {
                    RequestStatus.InProgress,
                    RequestStatus.Approved
                },
                RequestStatus.AcceptancePendingVerify => new[]
                {
                    RequestStatus.Completed
                },
                RequestStatus.Completed => Array.Empty<RequestStatus>(),
                RequestStatus.Cancelled => Array.Empty<RequestStatus>(),
                _ => Array.Empty<RequestStatus>()
            };
            return validNextStatuses.Contains(newStatus);
        }

        private async Task SendNotificationForUserApartment(int repairRequestId, RequestStatus newStatus)
        {
            var userIds = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    selector: s => s.Apartment.UserApartments.Where(ua => ua.Status == ActiveStatus.Active)
                                                             .Select(x => x.UserId),
                    predicate: p => p.RepairRequestId == repairRequestId,
                    include: i => i.Include(x => x.Apartment)
                                        .ThenInclude(x => x.UserApartments)
                    );

            string description = string.Empty;

            switch (newStatus)
            {
                case RequestStatus.Pending:
                    description = "Yêu cầu sửa chữa của bạn đang chờ duyệt.";
                    break;

                case RequestStatus.Approved:
                    description = "Yêu cầu sửa chữa của bạn đã được duyệt và đang chờ bắt đầu.";
                    break;

                case RequestStatus.InProgress:
                    description = "Kỹ thuật viên đang tiến hành xử lý yêu cầu sửa chữa của bạn.";
                    break;
                case RequestStatus.Scheduling:
                    description = "Yêu cầu sửa chữa của bạn đang trong quá trình thay đổi lịch trình.";
                    break;

                case RequestStatus.AcceptancePendingVerify:
                    description = "Yêu cầu sửa chữa của bạn đang chờ nghiệm thu.";
                    break;

                case RequestStatus.Completed:
                    description = "Yêu cầu sửa chữa của bạn đã được hoàn tất.";
                    break;

                case RequestStatus.Cancelled:
                    description = "Yêu cầu sửa chữa của bạn đã bị hủy.";
                    break;

                default:
                    description = "Trạng thái yêu cầu sửa chữa không xác định.";
                    break;
            }

            await _rabbitMQService.PublishNotificationAsync(new NotificationPushRequestDto
            {
                Title = "Yêu cầu sửa chữa",
                Type = NotificationType.Individual,
                Description = description,
                UserIds = userIds
            });
        }

        public async Task CheckAcceptanceTimeAsync(DateTime dateTime)
        {
            try
            {
                var repairRequestIds = await _unitOfWork.GetRepository<RepairRequest>().GetListAsync(
                    selector: s => s.RepairRequestId,
                    predicate: p => p.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().Status ==
                                        RequestStatus.AcceptancePendingVerify &&
                                    p.AcceptanceTime != null &&
                                   p.AcceptanceTime.Value == DateOnly.FromDateTime(dateTime), // Thay đổi so sánh
                    include: i => i.Include(x => x.RequestTrackings)
                    );

                foreach (var id in repairRequestIds)
                {
                    await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(new RequestTracking
                    {
                        RepairRequestId = id,
                        Status = RequestStatus.Completed,
                        UpdatedAt = DateTime.Now,
                        Note = "Yêu cầu sửa chữa được tự động hoàn thành "
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Lỗi khi gửi thông báo tự động.");
            }
        }
        private Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.RepairRequestId);

            return sortBy.ToLower() switch
            {
                "apartment" => q => q.OrderBy(p => p.ApartmentId),
                "apartment_desc" => q => q.OrderByDescending(p => p.ApartmentId),
                "issue" => q => q.OrderBy(p => p.IssueId),
                "issue_desc" => q => q.OrderByDescending(p => p.IssueId),
                _ => q => q.OrderByDescending(p => p.RepairRequestId)
            };
        }

        public async Task CheckMaintenanceScheduleAsync(DateTime now)
        {
            var maintenanceSchedules = await _unitOfWork.GetRepository<MaintenanceSchedule>().GetListAsync(
                    predicate: p => p.Status == ActiveStatus.Active && p.NextScheduledDate == DateOnly.FromDateTime(now).AddDays(3),
                    include: i => i.Include(x => x.CommonAreaObject)
                                        .ThenInclude(x => x.CommonArea)
                    );
            if (maintenanceSchedules.Count == 0) return;

            foreach (var item in maintenanceSchedules)
            {
                await GenerateRepairRequestFromMaintenanceScheduleAsync(item);
            }
        }

        private async Task GenerateRepairRequestFromMaintenanceScheduleAsync(MaintenanceSchedule schedule)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var existingRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                    predicate: rr => rr.MaintenanceScheduleId == schedule.MaintenanceScheduleId &&
                                     DateOnly.FromDateTime(rr.CreatedAt) == DateOnly.FromDateTime(DateTime.Now)
                );

                if (existingRequest)
                {
                    _logger.LogWarning("RepairRequest cho MaintenanceSchedule ID {ScheduleId} đã được tạo hôm nay",
                        schedule.MaintenanceScheduleId);
                    return;
                }

                var managerId = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    selector: s => s.UserId,
                    predicate: u => u.Account.Role == AccountRole.Manager,
                    include: i => i.Include(u => u.Account)
                );

                if (managerId == 0)
                {
                    _logger.LogError("Không tìm thấy Manager để tạo RepairRequest tự động.");
                    return;
                }

                var repairRequest = new RepairRequest
                {
                    UserId = managerId, 
                    MaintenanceScheduleId = schedule.MaintenanceScheduleId,
                    Object = schedule.CommonAreaObject.Name,
                    Description = $"Bảo trì định kỳ: {schedule.Description}. " +
                                  $"Khu vực: {schedule.CommonAreaObject.CommonArea.Name}. " +
                                  $"Chu kỳ: {schedule.FrequencyInDays} ngày.",
                    IsEmergency = false,
                    CreatedAt = DateTime.Now,
                };

                await _unitOfWork.GetRepository<RepairRequest>().InsertAsync(repairRequest);
                await _unitOfWork.CommitAsync();

                var requestTracking = new RequestTracking
                {
                    RepairRequestId = repairRequest.RepairRequestId,
                    Status = RequestStatus.Pending,
                    Note = "Yêu cầu bảo trì định kỳ được tạo tự động từ hệ thống.",
                    UpdatedBy = managerId,
                    UpdatedAt = DateTime.Now
                };

                await _unitOfWork.GetRepository<RequestTracking>().InsertAsync(requestTracking);

                var startTime = schedule.NextScheduledDate.ToDateTime(TimeOnly.FromTimeSpan(schedule.TimePreference));
                var endTime = startTime.AddHours(schedule.EstimatedDuration);

                var appointment = new Appointment
                {
                    RepairRequestId = repairRequest.RepairRequestId,
                    StartTime = startTime,
                    EndTime = endTime,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
                await _unitOfWork.CommitAsync();

                var newAppoTracking = new AppointmentTracking
                {
                    AppointmentId = appointment.AppointmentId,
                    UpdatedBy = managerId,
                    UpdatedAt = DateTime.Now,
                    Status = AppointmentStatus.Pending,
                    Note = "Lịch hẹn được tạo tự động từ hệ thống"
                };
                await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(newAppoTracking);
                await _unitOfWork.CommitAsync();

                var isAssigned = await AssignTechnicianForMantainenanceAppointmentAsync(appointment, schedule);
                if (isAssigned)
                {
                    await _unitOfWork.GetRepository<AppointmentTracking>().InsertAsync(new AppointmentTracking
                    {
                        AppointmentId = appointment.AppointmentId,
                        UpdatedBy = managerId,
                        Status = AppointmentStatus.Assigned,
                        UpdatedAt = DateTime.Now,
                        Note = "Tự động phân công cho lịch hẹn với Id: " + appointment.AppointmentId.ToString()

                    });
                    await _unitOfWork.CommitAsync();
                }

                await _notificationService.SendNotificationForTechleadManager(new NotificationPushRequestDto
                {
                    Title = "Yêu cầu bảo trì",
                    Type = NotificationType.Individual,
                    Description = isAssigned ? "Có yêu cầu bảo trì định kỳ được tạo tự động từ hệ thống cần xác nhận." : "Có yêu cầu bảo trì định kỳ được tạo tự động từ hệ thống cần phân công."
                });

                schedule.LastMaintenanceDate = DateOnly.FromDateTime(DateTime.Now);
                schedule.NextScheduledDate = DateOnly.FromDateTime(DateTime.Now).AddDays(schedule.FrequencyInDays);

                _unitOfWork.GetRepository<MaintenanceSchedule>().UpdateAsync(schedule);
                await _unitOfWork.CommitAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Đã tạo RepairRequest ID {RequestId} từ MaintenanceSchedule ID {ScheduleId}",
                    repairRequest.RepairRequestId,
                    schedule.MaintenanceScheduleId
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex,
                    "Lỗi khi tạo RepairRequest từ MaintenanceSchedule ID {ScheduleId}",
                    schedule.MaintenanceScheduleId
                );
            }
        }

        private async Task<bool> AssignTechnicianForMantainenanceAppointmentAsync(Appointment appointment, MaintenanceSchedule schedule)
        {
            var techniciansAcceptable = await _appointmentAssignService.SuggestTechniciansForAppointment(appointment.AppointmentId, null);
            var technicianidsAcceptable = techniciansAcceptable.Select(x => x.UserId).ToList();

            if (technicianidsAcceptable.Count < schedule.RequiredTechnicians)
            {
                return false;
            }

            var technicianIds = technicianidsAcceptable.Take(schedule.RequiredTechnicians);

            foreach (var technicianId in technicianIds)
            {
                await _unitOfWork.GetRepository<AppointmentAssign>().InsertAsync(new AppointmentAssign
                {
                    Appointment = appointment,
                    TechnicianId = technicianId,
                    AssignedAt = DateTime.Now,
                    EstimatedStartTime = appointment.StartTime,
                    EstimatedEndTime = (DateTime)appointment.EndTime,
                    Status = WorkOrderStatus.Pending
                });
            }
            return true;
        }
    }
}
