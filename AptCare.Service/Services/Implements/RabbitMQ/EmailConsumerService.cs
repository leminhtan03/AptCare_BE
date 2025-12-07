using AptCare.Service.Dtos.EmailDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AptCare.Service.Services.Implements.RabbitMQ
{
    public class EmailConsumerService : BackgroundService
    {
        private readonly ILogger<EmailConsumerService> _logger;
        private readonly ConnectionFactory _factory;
        private IChannel _channel;
        private IConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "email_notification";
        private const string DLQName = "email_notification.dlq";
        private const string DLXName = "email_notification.dlx";
        private const int MaxRetryCount = 3;

        public EmailConsumerService(
            ILogger<EmailConsumerService> logger,
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

            // Declare Dead Letter Exchange
            await _channel.ExchangeDeclareAsync(
                exchange: DLXName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare Dead Letter Queue
            await _channel.QueueDeclareAsync(
                queue: DLQName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind DLQ to DLX
            await _channel.QueueBindAsync(
                queue: DLQName,
                exchange: DLXName,
                routingKey: QueueName);

            // Declare main queue with DLX configuration
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DLXName },
                { "x-dead-letter-routing-key", QueueName }
            };

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var emailDto = JsonSerializer.Deserialize<EmailRequestDto>(message);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mailService = scope.ServiceProvider.GetRequiredService<IMailSenderService>();

                        await mailService.SendEmailWithTemplateAsync(
                            toEmail: emailDto.ToEmail,
                            subject: emailDto.Subject,
                            templateName: emailDto.TemplateName,
                            replacements: emailDto.Replacements
                        );
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation($"Email sent successfully to {emailDto.ToEmail}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi xử lý send email: {message}");

                    var retryCount = GetRetryCount(ea.BasicProperties);

                    if (retryCount < MaxRetryCount)
                    {
                        _logger.LogWarning($"Retry lần {retryCount + 1}/{MaxRetryCount} cho message");

                        await RequeueMessageWithDelay(message, retryCount + 1, ea.BasicProperties);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogError($"Message đã vượt quá số lần retry ({MaxRetryCount}), chuyển vào DLQ");

                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("EmailConsumerService started listening to queue.");
        }

        private int GetRetryCount(IReadOnlyBasicProperties properties)
        {
            if (properties?.Headers != null && properties.Headers.TryGetValue("x-retry-count", out var retryCountObj))
            {
                return Convert.ToInt32(retryCountObj);
            }
            return 0;
        }

        private async Task RequeueMessageWithDelay(string message, int retryCount, IReadOnlyBasicProperties originalProperties)
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object>
                {
                    { "x-retry-count", retryCount }
                }
            };

            if (originalProperties?.Headers != null)
            {
                foreach (var header in originalProperties.Headers)
                {
                    if (header.Key != "x-retry-count")
                    {
                        properties.Headers[header.Key] = header.Value;
                    }
                }
            }

            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EmailConsumerService stopping...");

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