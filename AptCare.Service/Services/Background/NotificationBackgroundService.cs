using AptCare.Repository.Enum;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Background
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;

                    if (now.Hour == 8 && now.Minute == 0)
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var repairRequestService = scope.ServiceProvider.GetRequiredService<IRepairRequestService>();

                        await notificationService.SendAndPushNotificationForAppointmentAsync(now);
                        await repairRequestService.CheckAcceptanceTimeAsync(now);

                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Lỗi: {e.Message}");
            }            
        }
    }

}
