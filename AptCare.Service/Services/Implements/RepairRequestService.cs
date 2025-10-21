using AptCare.Repository.Entities;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Repository.Enum.AccountUserEnum;
using Microsoft.EntityFrameworkCore;
using AptCare.Repository.Enum;
using AptCare.Service.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Linq.Dynamic.Core;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class RepairRequestService : BaseService<RepairRequestService>, IRepairRequestService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IAppointmentAssignService _appointmentAssignService;

        public RepairRequestService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<RepairRequestService> logger,
            IMapper mapper,
            IUserContext userContext,
            ICloudinaryService cloudinaryService,
            IAppointmentAssignService appointmentAssignService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _appointmentAssignService = appointmentAssignService;
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
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
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
                            CreatedAt = DateTime.UtcNow.AddHours(7),
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
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = AppointmentStatus.Pending
                };

                await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
                await _unitOfWork.CommitAsync();

                bool isAssigned = false;

                if (issue != null)
                {
                    isAssigned = await AssignTechnicianForNormalAppointmentAsync(appointment, issue);
                }



                var techLeadId = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        selector: s => s.UserId,
                        predicate: p => p.Account.Role == AccountRole.TechnicianLead,
                        include: i => i.Include(x => x.Account)
                    );

                var notification = new Notification
                {
                    ReceiverId = techLeadId,
                    Type = NotificationType.Individual,
                    Description = isAssigned ? "Có 1 yêu cầu sữa chữa mới cần xác nhận." : "Có 1 yêu cầu sữa chữa mới cần phân công.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                };

                await _unitOfWork.GetRepository<Notification>().InsertAsync(notification);

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
                    AssignedAt = DateTime.UtcNow.AddHours(7),
                    EstimatedStartTime = appointment.StartTime,
                    EstimatedEndTime = (DateTime) appointment.EndTime,
                    Status = WorkOrderStatus.Pending
                });

                await _unitOfWork.GetRepository<Notification>().InsertAsync(new Notification
                {
                    ReceiverId = technicianId,
                    Type = NotificationType.Individual,
                    Description = "Có yêu cầu sữa chữa mới được giao cho bạn.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });
            }

            appointment.Status = AppointmentStatus.Assigned;
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
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
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
                            CreatedAt = DateTime.UtcNow.AddHours(7),
                            Status = ActiveStatus.Active

                        });
                    }
                }

                var appointment = new Appointment
                {
                    RepairRequestId = request.RepairRequestId,
                    StartTime = DateTime.UtcNow.AddHours(7),
                    EndTime = DateTime.UtcNow.AddHours(7).AddHours(issue.EstimatedDuration),
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    Status = AppointmentStatus.Pending
                };

                await _unitOfWork.GetRepository<Appointment>().InsertAsync(appointment);
                await _unitOfWork.CommitAsync();
              
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

            var notifications = new List<Notification>();
           
            if (technicianidsAcceptable.Count != 0)
            {
                var technicianIds = technicianidsAcceptable.Take(issue.RequiredTechnician);

                foreach (var technicianId in technicianIds)
                {
                    await _unitOfWork.GetRepository<AppointmentAssign>().InsertAsync(new AppointmentAssign
                    {
                        Appointment = appointment,
                        TechnicianId = technicianId,
                        AssignedAt = DateTime.UtcNow.AddHours(7),
                        EstimatedStartTime = appointment.StartTime,
                        EstimatedEndTime = (DateTime) appointment.EndTime,
                        Status = WorkOrderStatus.Working
                    });

                    notifications.Add(new Notification
                    {
                        ReceiverId = technicianId,
                        Type = NotificationType.Individual,
                        Description = "Có yêu cầu sữa chữa khẩn cấp được giao cho bạn. Vui lòng tiến hành sữa chữa ngay",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
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
                foreach (var id in ids)
                {
                    notifications.Add(new Notification
                    {
                        ReceiverId = id,
                        Type = NotificationType.Individual,
                        Description = $"Có yêu cầu sữa chữa khẩn cấp. Đã phân công được {technicianidsAcceptable.Count}/{issue.RequiredTechnician} kĩ thuật viên. Vui lòng tiếp tục phân công.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    });
                }
            }
            else
            {
                appointment.Status = AppointmentStatus.Assigned;

                foreach (var id in ids)
                {
                    notifications.Add(new Notification
                    {
                        ReceiverId = id,
                        Type = NotificationType.Individual,
                        Description = $"Có yêu cầu sữa chữa khẩn cấp. Đã phân công đủ {issue.RequiredTechnician}/{issue.RequiredTechnician} kĩ thuật viên.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    });
                }
            }

            await _unitOfWork.GetRepository<Notification>().InsertRangeAsync(notifications);
        }

        public async Task<IPaginate<RepairRequestDto>> GetPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? apartmentId, int? issueId, int? maintenanceRequestId)
        {
            if (_userContext.IsResident)
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, _userContext.CurrentUserId, null, apartmentId, issueId, null);
            }
            else if (_userContext.IsTechnician)
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, null, _userContext.CurrentUserId, apartmentId, issueId, maintenanceRequestId);
            }
            else
            {
                return await GetGenericPaginateRepairRequestAsync(dto, isEmergency, null, null, apartmentId, issueId, maintenanceRequestId);
            }
        }

        private async Task<IPaginate<RepairRequestDto>> GetGenericPaginateRepairRequestAsync(PaginateDto dto, bool? isEmergency, int? residentId, int? technicianId, int? apartmentId, int? issueId, int? maintenanceRequestId)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<RepairRequest, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Object.Contains(search) ||
                                                 p.Description.Contains(search)) &&
                (string.IsNullOrEmpty(filter) ||
                filter.Equals(p.RequestTrackings.OrderByDescending(x => x.UpdatedAt).First().ToString().ToLower())) &&
                (isEmergency == null || p.IsEmergency == isEmergency) &&
                (residentId == null || p.Apartment.UserApartments.Any(x => x.UserId == residentId)) &&
                (technicianId == null || p.Appointments.Any(a => a.AppointmentAssigns.Any(aa => aa.TechnicianId == technicianId))) &&
                (apartmentId == null || p.ApartmentId == apartmentId) &&
                (issueId == null || p.IssueId == issueId) &&
                (maintenanceRequestId == null || p.MaintenanceRequestId == maintenanceRequestId);

            var result = await _unitOfWork.GetRepository<RepairRequest>().GetPagingListAsync(
                selector: x => _mapper.Map<RepairRequestDto>(x),
                predicate: predicate,
                include: i => i.Include(x => x.RequestTrackings)
                                    .ThenInclude(x => x.UpdatedByUser)
                               .Include(x => x.ParentRequest)
                               .Include(x => x.ChildRequests)
                               .Include(x => x.Appointments)
                                    .ThenInclude(x => x.AppointmentAssigns)
                                        .ThenInclude(x => x.Technician)
                               .Include(x => x.User)
                               .Include(x => x.Apartment)
                               .Include(x => x.MaintenanceRequest)
                               .Include(x => x.Issue),
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

        private Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.RepairRequestId);

            return sortBy.ToLower() switch
            {
                "apartment" => q => q.OrderBy(p => p.ApartmentId),
                "apartment_desc" => q => q.OrderByDescending(p => p.ApartmentId),
                "issue" => q => q.OrderBy(p => p.IssueId),
                "issue_desc" => q => q.OrderByDescending(p => p.IssueId),
                _ => q => q.OrderByDescending(p => p.RepairRequestId) // Default sort
            };
        }
    }
}
