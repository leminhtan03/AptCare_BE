using AptCare.Service.Dtos.PayOSDto;

namespace AptCare.Service.Services.PayOSService
{
    public interface IPayOSWebhookService
    {
        Task HandleAsync(PayOSWebhookRequest req);
    }
}
