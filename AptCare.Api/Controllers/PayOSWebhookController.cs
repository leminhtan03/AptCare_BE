using AptCare.Service.Dtos.PayOSDto;
using AptCare.Service.Services.PayOSService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/payos/webhook")]
public class PayOSWebhookController : ControllerBase
{
    private readonly IPayOSWebhookService _svc;
    private readonly ILogger<PayOSWebhookController> _logger;

    public PayOSWebhookController(IPayOSWebhookService svc, ILogger<PayOSWebhookController> logger)
    {
        _svc = svc;
        _logger = logger;
    }
    
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        try
        {
            _logger.LogInformation("PayOS webhook raw: {Payload}", payload.ToString());
            
            if (payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("data", out var _))
            {
                var model = JsonSerializer.Deserialize<PayOSWebhookRequest>(payload);
                if (model != null)
                    await _svc.HandleAsync(model);
            }
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook");
            return Ok(new { ok = true });
        }
    }

}

