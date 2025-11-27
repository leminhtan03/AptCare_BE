using PayOS.Models.Webhooks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IPayOSWebhookService
    {
        Task HandleAsync(Webhook req);
    }
}
