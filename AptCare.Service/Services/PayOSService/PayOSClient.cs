using AptCare.Service.Dtos.PayOSDto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AptCare.Service.Services.PayOSService
{
    public class PayOSClient : IPayOSClient
    {
        private readonly HttpClient _httpClient;
        private readonly PayOSOptions _options;
        private readonly ILogger<PayOSClient> _logger;

        public PayOSClient(HttpClient httpClient, IOptions<PayOSOptions> options, ILogger<PayOSClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<(string checkoutUrl, string paymentLinkId)> CreatePaymentLinkAsync(
            long orderCode, long amount, string description, string returnUrl)
        {
            var body = new
            {
                orderCode,
                amount,
                description,
                returnUrl,
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

            var res = await _httpClient.PostAsync("/v2/payment-requests", content);
            var resJson = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS error: {Res}", resJson);
                throw new Exception("Tạo link thanh toán PayOS thất bại");
            }

            using var doc = JsonDocument.Parse(resJson);
            var data = doc.RootElement.GetProperty("data");
            var checkoutUrl = data.GetProperty("checkoutUrl").GetString()!;
            var linkId = data.GetProperty("paymentLinkId").GetString()!;

            return (checkoutUrl, linkId);
        }

        public bool VerifySignature(string sortedJson, string signature)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ChecksumKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(sortedJson));
            var computedSig = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return computedSig == signature.ToLowerInvariant();
        }
    }
}
