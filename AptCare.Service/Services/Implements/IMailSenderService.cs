
namespace AptCare.Service.Services.Implements
{
    public interface IMailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendEmailWithTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements);
    }
}
