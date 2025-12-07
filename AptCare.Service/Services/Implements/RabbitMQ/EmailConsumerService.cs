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
            var arguments = new Dictionary<string, object>
            {
                { "x-max-priority", 10 } // Priority queue support
            };

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments
            );

            // Set prefetch count để limit số message xử lý đồng thời
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
                    _logger.LogError(ex, $"Error sending email: {message}");

                    // Retry logic: requeue if delivery count < 3
                    if (ea.BasicProperties?.Headers != null &&
                        ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryCountObj))
                    {
                        var retryCount = Convert.ToInt32(retryCountObj);
                        if (retryCount >= 3)
                        {
                            // Dead letter queue or log failure
                            await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                            _logger.LogError($"Email failed after 3 retries, discarding message");
                            return;
                        }
                    }
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("EmailConsumerService started listening to queue.");
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