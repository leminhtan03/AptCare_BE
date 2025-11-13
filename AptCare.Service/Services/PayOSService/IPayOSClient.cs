namespace AptCare.Service.Services.PayOSService
{
    public interface IPayOSClient
    {
        Task<(string checkoutUrl, string? paymentLinkId)> CreatePaymentLinkAsync(long orderCode, long amount, string description, string returnUrl);
        bool VerifySignature(string sortedJson, string signature);
    }
}
