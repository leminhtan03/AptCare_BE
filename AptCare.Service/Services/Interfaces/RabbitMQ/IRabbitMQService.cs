using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces.RabbitMQ
{
    public interface IRabbitMQService
    {
        Task PublishNotificationAsync<T>(T message) where T : class;
        Task PushNotificationAsync<T>(T message) where T : class;
        Task PublishEmailAsync<T>(T message) where T : class;
    }
}
