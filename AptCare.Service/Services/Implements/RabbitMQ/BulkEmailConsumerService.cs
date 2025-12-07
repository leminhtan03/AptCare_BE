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
    public class BulkEmailConsumerService : BackgroundService
    {
        private readonly ILogger<BulkEmailConsumerService> _logger;
        private readonly ConnectionFactory _factory;
        private IChannel _channel;
        private IConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "bulk_email";
        private const string DLQName = "bulk_email.dlq";
        private const string DLXName = "bulk_email.dlx";
        private const int MaxRetryCount = 3;

        public BulkEmailConsumerService(
            ILogger<BulkEmailConsumerService> logger,
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

            await _channel.ExchangeDeclareAsync(
                exchange: DLXName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            await _channel.QueueDeclareAsync(
                queue: DLQName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: DLQName,
                exchange: DLXName,
                routingKey: QueueName);

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

            // ? Gi?m prefetchCount vì m?i message ch?a nhi?u email
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 2, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var bulkMetadata = JsonSerializer.Deserialize<BulkEmailMetadataDto>(message);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mailService = scope.ServiceProvider.GetRequiredService<IMailSenderService>();

                        // ? T?O VÀ G?I EMAIL SONG SONG NGAY T?I ?ÂY
                        var emailTasks = bulkMetadata.Recipients.Select(async recipient =>
                        {
                            try
                            {
                                // T?o replacements cho t?ng user
                                var userReplacements = new Dictionary<string, string>(bulkMetadata.CommonReplacements)
                                {
                                    ["ResidentName"] = $"{recipient.FirstName} {recipient.LastName}"
                                };

                                // G?i email tr?c ti?p
                                await mailService.SendEmailWithTemplateAsync(
                                    toEmail: recipient.Email,
                                    subject: bulkMetadata.Subject,
                                    templateName: bulkMetadata.TemplateName,
                                    replacements: userReplacements
                                );

                                _logger.LogDebug("Email sent to {Email}", recipient.Email);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send email to {Email}", recipient.Email);
                                // Không throw ?? các email khác v?n ???c g?i
                            }
                        });

                        // Ch? t?t c? email ???c g?i
                        await Task.WhenAll(emailTasks);
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Bulk email batch processed: {Count} emails", bulkMetadata.Recipients.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"L?i khi x? lý bulk email batch: {message}");

                    var retryCount = GetRetryCount(ea.BasicProperties);

                    if (retryCount < MaxRetryCount)
                    {
                        _logger.LogWarning($"Retry l?n {retryCount + 1}/{MaxRetryCount} cho bulk email batch");
                        await RequeueMessageWithDelay(message, retryCount + 1, ea.BasicProperties);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogError($"Bulk email batch ?ã v??t quá s? l?n retry ({MaxRetryCount}), chuy?n vào DLQ");
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("BulkEmailConsumerService started listening to queue.");
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
            _logger.LogInformation("BulkEmailConsumerService stopping...");

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