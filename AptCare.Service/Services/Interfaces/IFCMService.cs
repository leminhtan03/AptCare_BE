using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IFCMService
    {
        Task<bool> PushNotificationAsync(string fcmToken, string title, string body, string? image = null);
        Task<bool> PushMulticastAsync(IEnumerable<string> fcmTokens, string title, string body, string? image = null);
    }
}
