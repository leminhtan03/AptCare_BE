
using AptCare.Repository.Repositories;
using AptCare.Service.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AptCare.Service.Services.Interfaces
{


    public class MailSenderService : IMailSenderService
    {
        private readonly MailSettings _mailSettings;
        private readonly IHostEnvironment _env;


        public MailSenderService(IOptions<MailSettings> mailSettings, IHostEnvironment env)
        {
            _mailSettings = mailSettings.Value;
            _env = env;
        }
        /// <inheritdoc />
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(toEmail))
                throw new ArgumentNullException(nameof(toEmail));

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_mailSettings.SystemName, _mailSettings.Sender));
            mimeMessage.To.Add(MailboxAddress.Parse(toEmail));
            mimeMessage.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            mimeMessage.Body = builder.ToMessageBody();

            await SendMimeMessageAsync(mimeMessage);
        }

        /// <inheritdoc />
        public async Task SendEmailWithTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements)
        {
            string htmlBody = await LoadEmailTemplateAsync(templateName);

            foreach (var rep in replacements)
            {
                htmlBody = htmlBody.Replace($"{{{{{rep.Key}}}}}", rep.Value);
            }
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private async Task<string> LoadEmailTemplateAsync(string templateName)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "MailTemplate", $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template '{templateName}.html' không được tìm thấy tại đường dẫn: {templatePath}");
            }

            return await File.ReadAllTextAsync(templatePath);
        }

        private async Task SendMimeMessageAsync(MimeMessage message)
        {
            if (string.IsNullOrWhiteSpace(_mailSettings.Host))
                throw new AppValidationException("MailSettings.Host is empty.");
            if (_mailSettings.Port <= 0)
                throw new AppValidationException("MailSettings.Port is invalid.");
            if (string.IsNullOrWhiteSpace(_mailSettings.Sender))
                throw new AppValidationException("MailSettings.Sender is empty.");

            using var smtpClient = new SmtpClient
            {
                Timeout = 15000 // 15s
            };

            try
            {
                await smtpClient.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(_mailSettings.Sender, _mailSettings.Password);
                await smtpClient.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new AppValidationException($"Gửi email thất bại: {ex.Message}");
            }
            finally
            {
                if (smtpClient.IsConnected)
                    await smtpClient.DisconnectAsync(true);
            }
        }
    }
}
