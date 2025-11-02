using AptCare.Repository.FCM;
using AptCare.Service.Constants;
using AptCare.Service.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace AptCare.Service.Services.Implements
{
    public class FCMService : IFCMService
    {
        private readonly IOptions<FCMSettings> _config;
        private readonly HttpClient _httpClient;

        public FCMService(IOptions<FCMSettings> config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<bool> PushNotificationaaaAsync(string fcmToken, string title, string body, string image)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={_config.Value.ServerKey}");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sender", $"id={_config.Value.SenderId}");

            var payload = new
            {
                to = fcmToken,
                notification = new
                {
                    title = title,
                    body = body,
                    image = image
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", requestContent);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PushNotificationAsync(string fcmToken, string title, string body, string? image = null)
        {
            return await PushMulticastAsync(new List<string> { fcmToken }, title, body, image);
        }

        /// <summary>
        /// Gửi thông báo đến nhiều thiết bị cùng lúc (tối đa 1000 token / request).
        /// </summary>
        public async Task<bool> PushMulticastAsync(IEnumerable<string> fcmTokens, string title, string body, string? image = null)
        {
            var tokens = fcmTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (tokens == null || tokens.Count == 0)
                return false;

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/aptcare-28d40/messages:send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={_config.Value.ServerKey}");
            request.Headers.TryAddWithoutValidation("Sender", $"id={_config.Value.SenderId}");

            var payload = new
            {
                registration_ids = tokens,
                notification = new
                {
                    title,
                    body,
                    image = string.IsNullOrEmpty(image) ? $"{Constant.LOGO_IMAGE}" : image
                },
                data = new
                {
                    click_action = "FLUTTER_NOTIFICATION_CLICK",
                    sound = "default"
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[FCM Error] {response.StatusCode}: {err}");
            }

            return response.IsSuccessStatusCode;
        }
    }
}
