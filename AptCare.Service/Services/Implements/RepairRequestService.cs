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

namespace AptCare.Service.Services.Implements
{
    public class RepairRequestService : BaseService<RepairRequest>, IRepairRequestService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;

        public RepairRequestService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<RepairRequest> logger,
            IMapper mapper,
            IUserContext userContext,
            ICloudinaryService cloudinaryService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
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
            var technicianidsAcceptable = await _unitOfWork.GetRepository<User>().GetListAsync(
                    selector: s => s.UserId,
                    predicate: p => p.Account.Role == AccountRole.Technician &&
                                    p.TechnicianTechniques.Any(tt => tt.TechniqueId == issue.TechniqueId) &&
                                    p.WorkSlots.Any(ws => ws.Date == DateOnly.FromDateTime(appointment.StartTime) &&
                                        ws.Slot.FromTime <= appointment.StartTime.TimeOfDay &&
                                        ws.Slot.ToTime >= appointment.StartTime.TimeOfDay) &&
                                    p.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) == DateOnly.FromDateTime(appointment.StartTime)).All(aa => aa.EstimatedEndTime <= appointment.StartTime ||
                                                                         aa.EstimatedStartTime >= appointment.EndTime),
                    include: i => i.Include(x => x.Account)
                                   .Include(x => x.WorkSlots)
                                       .ThenInclude(x => x.Slot)
                                   .Include(x => x.AppointmentAssigns)
                                   .Include(x => x.TechnicianTechniques),
                    orderBy: o => o.OrderBy(x => x.AppointmentAssigns.Count(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                                  DateOnly.FromDateTime(appointment.StartTime)))
                                           .ThenBy(x => x.AppointmentAssigns.Count(aa => aa.EstimatedStartTime.Month ==
                                                                                     appointment.StartTime.Month))
                );

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
            //var technicianidsAcceptable = await _unitOfWork.GetRepository<User>().GetListAsync(
            //        selector: s => s.UserId,
            //        predicate: p => p.Account.Role == AccountRole.Technician &&
            //                        p.TechnicianTechniques.Any(tt => tt.TechniqueId == issue.TechniqueId) &&
            //                        p.WorkSlots.Any(ws => ws.Status == WorkSlotStatus.Working) &&
            //                        p.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
            //                                                         DateOnly.FromDateTime(appointment.StartTime))
            //                                            .All(aa => aa.Status != WorkOrderStatus.Working),
            //        include: i => i.Include(x => x.Account)
            //                       .Include(x => x.WorkSlots)
            //                       .Include(x => x.AppointmentAssigns)
            //                       .Include(x => x.TechnicianTechniques),
            //        orderBy: o => o.OrderByDescending(x => x.AppointmentAssigns
            //                                         .Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
            //                                                      DateOnly.FromDateTime(appointment.StartTime) &&
            //                                                      aa.EstimatedStartTime > appointment.EndTime)
            //                                         .Select(aa => 
            //                                            (aa.EstimatedStartTime.TimeOfDay - appointment.StartTime.AddHours(issue.EstimatedDuration).TimeOfDay) > new TimeSpan(1, 0, 0) ? new TimeSpan(1, 0, 0) : (aa.EstimatedStartTime.TimeOfDay - appointment.StartTime.AddHours(issue.EstimatedDuration).TimeOfDay)))
            //                           .ThenBy(x => x.AppointmentAssigns.Count(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
            //                                                                      DateOnly.FromDateTime(appointment.StartTime)))
            //                               .ThenBy(x => x.AppointmentAssigns.Count(aa => aa.EstimatedStartTime.Month ==
            //                                                                         appointment.StartTime.Month))
            //    );

            var techniciansQuery = await _unitOfWork.GetRepository<User>().GetListAsync(
                selector: s => new
                {
                    s.UserId,
                    Assigns = s.AppointmentAssigns
                        .Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) == DateOnly.FromDateTime(appointment.StartTime)
                                     && aa.EstimatedStartTime > appointment.EndTime)
                        .Select(aa => aa.EstimatedStartTime)
                        .ToList(),
                    AssignCountToday = s.AppointmentAssigns.Count(aa =>
                        DateOnly.FromDateTime(aa.EstimatedStartTime) == DateOnly.FromDateTime(appointment.StartTime)),
                    AssignCountMonth = s.AppointmentAssigns.Count(aa =>
                        aa.EstimatedStartTime.Month == appointment.StartTime.Month)
                },
                predicate: p => p.Account.Role == AccountRole.Technician &&
                                p.TechnicianTechniques.Any(tt => tt.TechniqueId == issue.TechniqueId) &&
                                p.WorkSlots.Any(ws => ws.Status == WorkSlotStatus.Working) &&
                                p.AppointmentAssigns.Where(aa => DateOnly.FromDateTime(aa.EstimatedStartTime) ==
                                                                 DateOnly.FromDateTime(appointment.StartTime))
                                                                 .All(aa => aa.Status != WorkOrderStatus.Working),
                include: i => i.Include(x => x.Account)
                                   .Include(x => x.WorkSlots)
                                       .ThenInclude(x => x.Slot)
                                   .Include(x => x.AppointmentAssigns)
                                   .Include(x => x.TechnicianTechniques)
            );

            var technicianidsAcceptable = techniciansQuery
                .AsEnumerable()
                .OrderByDescending(x =>
                {                  
                    var minGap = x.Assigns
                        .Select(next => (next - (DateTime) appointment.EndTime).TotalMinutes)
                        .Where(gap => gap > 0)
                        .DefaultIfEmpty(double.MaxValue)
                        .Min();
                    return minGap > 60 ? 60 : minGap;
                })
                    .ThenBy(x => x.AssignCountToday)
                        .ThenBy(x => x.AssignCountMonth)
                .Select(x => x.UserId)
                .ToList();


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
    }
}
