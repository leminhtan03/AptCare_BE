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
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                var dlqName = "notification.dlq";
                var dlxName = "notification.dlx";
                await SendMessageAsync(queueName, dlqName, dlxName, message);
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
                var dlqName = "notification_push.dlq";
                var dlxName = "notification_push.dlx";
                await SendMessageAsync(queueName, dlqName, dlxName, message);
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
                var dlqName = "email_notification.dlq";
                var dlxName = "email_notification.dlx";
                await SendMessageAsync(queueName, dlqName, dlxName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi publish email message vào RabbitMQ");
                throw new Exception(ex.Message);
            }
        }

        public async Task PublishBulkEmailAsync<T>(T message) where T : class
        {
            try
            {
                var queueName = "bulk_email";
                var dlqName = "bulk_email.dlq";
                var dlxName = "bulk_email.dlx";
                await SendMessageAsync(queueName, dlqName, dlxName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi publish bulk email message vào RabbitMQ");
                throw new Exception(ex.Message);
            }
        }

        private async Task SendMessageAsync<T>(string queueName, string dlqName, string dlxName, T message)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Declare Dead Letter Exchange
                await channel.ExchangeDeclareAsync(
                    exchange: dlxName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                // Declare Dead Letter Queue
                await channel.QueueDeclareAsync(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind DLQ to DLX
                await channel.QueueBindAsync(
                    queue: dlqName,
                    exchange: dlxName,
                    routingKey: queueName);

                // Declare main queue with DLX configuration
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", dlxName },
                    { "x-dead-letter-routing-key", queueName }
                };

                await channel.QueueDeclareAsync(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: queueArgs);

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
