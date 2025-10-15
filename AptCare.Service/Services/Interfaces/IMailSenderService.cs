namespace AptCare.Service.Services.Interfaces
{
    public interface IMailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendEmailWithTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements);
    }
}
