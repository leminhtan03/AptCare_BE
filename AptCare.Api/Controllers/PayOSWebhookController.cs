using AptCare.Service.Services.PayOSService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
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

    /// <summary>
    /// PayOS Webhook Endpoint
    /// </summary>
    /// <remarks>
    /// **Endpoint này nhận webhook từ PayOS khi có cập nhật trạng thái thanh toán:**
    /// 
    /// **Các trạng thái PayOS:**
    /// - `PAID` - Thanh toán thành công
    /// - `CANCELLED` - Thanh toán bị hủy
    /// - `PENDING` - Đang chờ thanh toán
    /// 
    /// **Logic xử lý:**
    /// 1. Xác thực chữ ký từ PayOS
    /// 2. Tìm Transaction theo orderCode
    /// 3. Cập nhật trạng thái Transaction và Invoice
    /// 4. Log chi tiết để audit
    /// 
    /// **Bảo mật:**
    /// - Webhook được verify signature từ PayOS
    /// - Anonymous access (PayOS không gửi auth header)
    /// - Rate limiting nên được áp dụng ở reverse proxy
    /// </remarks>
    /// <param name="payload">Raw JSON payload từ PayOS</param>
    /// <response code="200">Webhook đã được xử lý thành công</response>
    /// <response code="400">Payload không hợp lệ</response>
    /// <response code="500">Lỗi xử lý webhook</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[Webhook-{RequestId}] Received PayOS webhook: {Payload}",
            requestId, payload.ToString());

        try
        {
            if (payload.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("[Webhook-{RequestId}] Invalid payload format - not an object", requestId);
                return BadRequest(new { error = "Invalid payload format", ok = false, requestId });
            }

            if (!payload.TryGetProperty("data", out var dataElement))
            {
                _logger.LogWarning("[Webhook-{RequestId}] Missing 'data' property in payload", requestId);
                return BadRequest(new { error = "Missing 'data' property", ok = false, requestId });
            }
            var model = JsonSerializer.Deserialize<Webhook>(payload);
            if (model?.Data == null)
            {
                _logger.LogWarning("[Webhook-{RequestId}] Failed to deserialize payload", requestId);
                return BadRequest(new { error = "Invalid payload structure", ok = false, requestId });
            }

            _logger.LogInformation("[Webhook-{RequestId}] Processing orderCode: {OrderCode}, status: {Status}", requestId, model.Data.OrderCode, model.Success);

            await _svc.HandleAsync(model);

            _logger.LogInformation("[Webhook-{RequestId}] Successfully processed PayOS webhook", requestId);
            return Ok(new
            {
                message = "Webhook processed successfully",
                ok = true,
                requestId,
                orderCode = model.Data.OrderCode,
                status = model.Success
            });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "[Webhook-{RequestId}] JSON deserialization error", requestId);
            return BadRequest(new
            {
                error = "Invalid JSON format",
                details = jsonEx.Message,
                ok = false,
                requestId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook");
            return Ok(new { ok = true });
        }
    }

}

