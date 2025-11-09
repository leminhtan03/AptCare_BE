using AptCare.Repository.FCM;
using AptCare.Service.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class FCMService : IFCMService
{
    private readonly string _firebaseConfigPath;
    private readonly HttpClient _httpClient;
    private readonly string _projectId;
    private readonly GoogleCredential _googleCredential;

    public FCMService(IOptions<FCMSettings> config, HttpClient httpClient)
    {
        _firebaseConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-service-account.json");
        _httpClient = httpClient;

        if (!File.Exists(_firebaseConfigPath))
            throw new FileNotFoundException($"Firebase config not found at {_firebaseConfigPath}");

        _googleCredential = GoogleCredential
            .FromFile(_firebaseConfigPath)
            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

        _projectId = config.Value.ProjectId;
    }

    public async Task<bool> PushNotificationAsync(string fcmToken, string title, string body, string? image = null)
    {
        var accessToken = await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        var requestUrl = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";

        var message = new
        {
            message = new
            {
                token = fcmToken,
                notification = new
                {
                    title,
                    body,
                    image
                }                 
            }
        };

        var json = JsonSerializer.Serialize(message);
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[FCM Response] {response.StatusCode}: {result}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PushNotificationWebAsync(string fcmToken, string title, string body, string? image = null)
    {
        var accessToken = await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        var requestUrl = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";

        var message = new
        {
            message = new
            {
                token = fcmToken,
                webpush = new
                {
                    notification = new
                    {
                        title = title,
                        body = body,
                        icon = image 
                    },
                    headers = new
                    {
                        TTL = "4500"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(message);
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[FCM Response] {response.StatusCode}: {result}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PushMulticastAsync(IEnumerable<string> fcmTokens, string title, string body, string? image = null)
    {
        var tokens = fcmTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (tokens == null || tokens.Count == 0)
            return false;

        var accessToken = await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        var requestUrl = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";

        var successCount = 0;

        // Gửi song song để tối ưu hiệu suất
        var tasks = tokens.Select(async token =>
        {
            var message = new
            {
                message = new
                {
                    token,
                    notification = new
                    {
                        title,
                        body,
                        image
                    }
                }
            };

            var json = JsonSerializer.Serialize(message);
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                Interlocked.Increment(ref successCount);
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[FCM Error] {response.StatusCode}: {err}");
            }
        });

        await Task.WhenAll(tasks);
        return true;
    }
}
