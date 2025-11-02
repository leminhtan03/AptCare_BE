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
using AptCare.Repository.Entities;
using AptCare.Service.Dtos.SlotDtos;
using AptCare.Service.Exceptions;
using Microsoft.AspNetCore.Http;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.Enum.AccountUserEnum;
using System.Collections;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using System.Linq.Expressions;
using AptCare.Service.Constants;

namespace AptCare.Service.Services.Implements
{
    public class NotificationService : BaseService<NotificationService>, INotificationService
    {
        private readonly IFCMService _fcmService;
        private readonly IUserContext _userContext;

        public NotificationService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<NotificationService> logger,
            IMapper mapper,
            IFCMService fcmService,
            IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _fcmService = fcmService;
            _userContext = userContext;
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
                await SendAndPushNotificationAsync(pushNotiDto);

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

            var notifications = await _unitOfWork.GetRepository<Notification>().GetListAsync(
                predicate: predicate                
                );
            var notificationNotread = notifications.Skip(page * size).Take(size).Where(x => !x.IsRead);

            foreach (var noti in notificationNotread)
            {
                noti.IsRead = true;
            }

            _unitOfWork.GetRepository<Notification>().UpdateRange(notifications);
            await _unitOfWork.CommitAsync();
            return result;
        }

        public async Task<bool> SendAndPushNotificationAsync(NotificationPushRequestDto dto)
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

                var image = dto.Image == null ? Constant.LOGO_IMAGE : dto.Image;

                var isPushed = await _fcmService.PushMulticastAsync(fcmTokens, dto.Title, dto.Description, image);
                if (!isPushed)
                {
                    throw new AppValidationException("Push notification thất bại.", StatusCodes.Status500InternalServerError);
                }

                await _unitOfWork.GetRepository<Notification>().InsertRangeAsync(notifications);
                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
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