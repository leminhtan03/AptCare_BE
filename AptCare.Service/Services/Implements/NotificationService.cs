using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Constants;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Dtos.SlotDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using AutoMapper;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class NotificationService : BaseService<NotificationService>, INotificationService
    {
        private readonly IFCMService _fcmService;
        private readonly IUserContext _userContext;
        private readonly IRabbitMQService _rabbitMQService;

        private const int APPOINTMENT_REMINDER = 3;

        public NotificationService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<NotificationService> logger,
            IMapper mapper,
            IFCMService fcmService,
            IUserContext userContext,
            IRabbitMQService rabbitMQService) : base(unitOfWork, logger, mapper)
        {
            _fcmService = fcmService;
            _userContext = userContext;
            _rabbitMQService = rabbitMQService;
        }

        public async Task<string> BroadcastNotificationAsync(NotificationCreateDto dto)
        {
            try
            {
                var pushNotiDto = _mapper.Map<NotificationPushRequestDto>(dto);

                ICollection<int> userIds = default;

                if (dto.Type == NotificationType.General)
                {
                    pushNotiDto.UserIds = await _unitOfWork.GetRepository<User>().GetListAsync(
                        selector: s => s.UserId,
                        predicate: x => x.Status == ActiveStatus.Active
                        );
                }
                else if (dto.Type == NotificationType.Internal)
                {
                    pushNotiDto.UserIds = await _unitOfWork.GetRepository<User>().GetListAsync(
                        selector: s => s.UserId,
                        predicate: x => x.Status == ActiveStatus.Active && x.Account.Role != AccountRole.Resident,
                        include: i => i.Include(x => x.Account)
                        );
                }
                
                await _rabbitMQService.PublishNotificationAsync(pushNotiDto);

                return "Gửi thông báo thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<IPaginate<NotificationDto>> GetMyNotificationPaginateAsync(PaginateDto dto)
        {
            var userId = _userContext.CurrentUserId;

            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;

            Expression<Func<Notification, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Title.Contains(search) || p.Description.Contains(search)) &&
                (string.IsNullOrEmpty(filter) || (filter.Equals("read") && p.IsRead) || (filter.Equals("not-read") && !p.IsRead)) &&
                p.ReceiverId == userId;

            var result = await _unitOfWork.GetRepository<Notification>().ProjectToPagingListAsync<NotificationDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                page: page,
                size: size
                );

            return result;
        }

        public async Task<string> MarkAsReadAsync(IEnumerable<int> ids)
        {
            var userId = _userContext.CurrentUserId;

            var notifications = await _unitOfWork.GetRepository<Notification>().GetListAsync(
                predicate: p => ids.Contains(p.NotificationId)
                );
            
            foreach (var noti in notifications)
            {
                if (noti.ReceiverId != userId)
                {
                    throw new AppValidationException($"Bạn không có thông báo ID {noti.NotificationId}.");
                }

                noti.IsRead = true;
            }

            _unitOfWork.GetRepository<Notification>().UpdateRange(notifications);
            await _unitOfWork.CommitAsync();
            return "Thành công";
        }

        public async Task<int> GetMyUnreadCountAsync()
        {
            try
            {
                var userId = _userContext.CurrentUserId;

                var unreadIds = await _unitOfWork.GetRepository<Notification>().GetListAsync(
                    selector: s => s.NotificationId,
                    predicate: p => p.ReceiverId == userId && !p.IsRead
                );

                return unreadIds?.Count() ?? 0;
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task SendAndPushNotificationAsync(NotificationPushRequestDto dto)
        {
            try
            {
                var notifications = new List<Notification>();
               
                foreach (var userId in dto.UserIds)
                {
                    var notification = _mapper.Map<Notification>(dto);
                    notification.ReceiverId = userId;
                    notifications.Add(notification);
                }

                var fcmTokens = await _unitOfWork.GetRepository<AccountToken>().GetListAsync(
                    selector: s => s.Token,
                    predicate: x => dto.UserIds.Contains(x.AccountId) && x.TokenType == TokenType.FCMToken && x.Status == TokenStatus.Active
                    );

                var image = string.IsNullOrEmpty(dto.Image) ? Constant.LOGO_IMAGE : dto.Image;

                var isPushed = await _fcmService.PushMulticastAsync(fcmTokens, dto.Title, dto.Description, image);

                await _unitOfWork.GetRepository<Notification>().InsertRangeAsync(notifications);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task PushNotificationAsync(NotificationPushRequestDto dto)
        {
            try
            {
                var fcmTokens = await _unitOfWork.GetRepository<AccountToken>().GetListAsync(
                    selector: s => s.Token,
                    predicate: x => dto.UserIds.Contains(x.AccountId) && x.TokenType == TokenType.FCMToken && x.Status == TokenStatus.Active
                    );

                var image = string.IsNullOrEmpty(dto.Image) ? Constant.LOGO_IMAGE : dto.Image;

                var isPushed = await _fcmService.PushMulticastAsync(fcmTokens, dto.Title, dto.Description, image);                
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task SendAndPushNotificationForAppointmentAsync(DateTime dateTime)
        {
            try
            {                
                var appointments = await _unitOfWork.GetRepository<Appointment>().GetListAsync(
                    predicate: p => DateOnly.FromDateTime(p.StartTime) == 
                                        DateOnly.FromDateTime(dateTime).AddDays(APPOINTMENT_REMINDER) &&
                                    p.AppointmentTrackings.OrderByDescending(x => x.UpdatedAt).First().Status == 
                                        AppointmentStatus.Confirmed,
                    include: i => i.Include(x => x.AppointmentTrackings)
                    );

                foreach (var appointment in appointments)
                {
                    var userIds = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    selector: s => s.Apartment.UserApartments.Where(ua => ua.Status == ActiveStatus.Active)
                                                             .Select(x => x.UserId),
                    predicate: p => p.RepairRequestId == appointment.RepairRequestId,
                    include: i => i.Include(x => x.Apartment)
                                        .ThenInclude(x => x.UserApartments)
                    );

                    var notificationDto = new NotificationPushRequestDto
                    {
                        Title = "Nhắc nhở lịch hẹn",
                        Type = NotificationType.Individual,
                        Description = $"Bạn có lịch hẹn sửa chữa vào {APPOINTMENT_REMINDER} ngày ({appointment.StartTime.TimeOfDay} ngày {DateOnly.FromDateTime(appointment.StartTime)})",
                        UserIds = userIds
                    };

                    await _rabbitMQService.PublishNotificationAsync(notificationDto);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Lỗi khi gửi thông báo tự động.");
            }
        }

        public async Task SendNotificationForTechnicianInAppointment(int appointmentId, NotificationPushRequestDto dto)
        {
            var userIds = await _unitOfWork.GetRepository<AppointmentAssign>().GetListAsync(
                    selector: s => s.TechnicianId,
                    predicate: p => p.AppointmentId == appointmentId && p.Status != WorkOrderStatus.Cancel
                    );

            dto.UserIds = userIds;
            await _rabbitMQService.PublishNotificationAsync(dto);
        }

        public async Task SendNotificationForResidentInRequest(int repairRequestId, NotificationPushRequestDto dto)
        {
            var userIds = await _unitOfWork.GetRepository<RepairRequest>().SingleOrDefaultAsync(
                    selector: s => s.Apartment.UserApartments.Where(ua => ua.Status == ActiveStatus.Active).Select(ua => ua.UserId),
                    predicate: p => p.RepairRequestId == repairRequestId,
                    include: i => i.Include(x => x.Apartment.UserApartments)
                    );

            dto.UserIds = userIds;
            // Publish vào RabbitMQ thay vì gọi trực tiếp
            await _rabbitMQService.PublishNotificationAsync(dto);
        }

        private Func<IQueryable<Notification>, IOrderedQueryable<Notification>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.CreatedAt);

            return sortBy.ToLower() switch
            {
                _ => q => q.OrderByDescending(p => p.CreatedAt) // Default sort
            };
        }
    }
}