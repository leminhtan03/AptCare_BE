using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.RabbitMQ;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements.RabbitMQ
{
    public class NotificationConsumerService : BackgroundService
    {
        private readonly ILogger<NotificationConsumerService> _logger;
        private readonly ConnectionFactory _factory;
        private IChannel _channel;
        private IConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "notification";

        public NotificationConsumerService(
            ILogger<NotificationConsumerService> logger,
            ConnectionFactory factory,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _factory = factory;
            _serviceProvider = serviceProvider;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: QueueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var notificationDto = JsonSerializer.Deserialize<NotificationPushRequestDto>(message);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        await notificationService.SendAndPushNotificationAsync(notificationDto);
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation($"Đã xử lý notification thành công");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi xử lý notification: {message}");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("NotificationConsumerService đã bắt đầu lắng nghe queue.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NotificationConsumerService đang dừng lại...");

            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
