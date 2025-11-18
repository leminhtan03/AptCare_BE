
using PayOS.Models.Webhooks;

namespace AptCare.Service.Services.PayOSService
{
    public interface IPayOSWebhookService
    {
        Task HandleAsync(Webhook req);
    }
}
