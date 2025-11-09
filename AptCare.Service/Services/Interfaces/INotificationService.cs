using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface INotificationService
    {
        Task<string> BroadcastNotificationAsync(NotificationCreateDto dto);
        Task<IPaginate<NotificationDto>> GetMyNotificationPaginateAsync(PaginateDto dto);
        Task SendAndPushNotificationAsync(NotificationPushRequestDto dto);
        Task PushNotificationAsync(NotificationPushRequestDto dto);
        Task SendAndPushNotificationForAppointmentAsync(DateTime dateTime);
        Task<string> MarkAsReadAsync(IEnumerable<int> ids);
    }
}
