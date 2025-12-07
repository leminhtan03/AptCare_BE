using AptCare.Service.Services.Interfaces.RabbitMQ;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements.RabbitMQ
{
    public class RabbitMQService : IRabbitMQService
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(ConnectionFactory factory, ILogger<RabbitMQService> logger)
        {
            _factory = factory;
            _logger = logger;
        }


        public async Task PublishNotificationAsync<T>(T message) where T : class
        {
            try
            {
                var queueName = "notification";
                await SendMessageAsync(queueName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi publish message vào RabbitMQ");
                throw new Exception(ex.Message);
            }
        }

        public async Task PushNotificationAsync<T>(T message) where T : class
        {
            try
            {
                var queueName = "notification_push";
                await SendMessageAsync(queueName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi publish message vào RabbitMQ");
                throw new Exception(ex.Message);
            }
        }

        public async Task PublishEmailAsync<T>(T message) where T : class
        {
            try
            {
                var queueName = "email_notification";
                await SendMessageAsync(queueName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi publish email message vào RabbitMQ");
                throw new Exception(ex.Message);
            }
        }

        private async Task SendMessageAsync<T>(string queueName, T message)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                };

                await channel.BasicPublishAsync(exchange: "",
                                     routingKey: queueName,
                                     mandatory: false,
                                     basicProperties: properties,
                                     body: body);

                _logger.LogInformation($" [x] Sent message to {queueName}: {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message to {queueName}: {ex.Message}");
            }
        }
    }
}
