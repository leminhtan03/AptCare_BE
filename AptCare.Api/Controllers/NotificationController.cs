using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Dtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gửi thông báo broadcast (đến toàn bộ người dùng hoặc nội bộ)
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        public async Task<IActionResult> BroadcastNotificationAsync([FromBody] NotificationCreateDto dto)
        {
            var message = await _notificationService.BroadcastNotificationAsync(dto);
            return Ok(new { message });
        }

        /// <summary>
        /// Lấy danh sách thông báo của người dùng hiện tại (phân trang + search + filter)
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotificationPaginateAsync([FromQuery] PaginateDto dto)
        {
            var result = await _notificationService.GetMyNotificationPaginateAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Gửi thông báo đến danh sách người dùng cụ thể
        /// </summary>
        [HttpPost("push")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        public async Task<IActionResult> SendAndPushNotificationAsync([FromBody] NotificationPushRequestDto dto)
        {
            var result = await _notificationService.SendAndPushNotificationAsync(dto);
            if (result)
                return Ok(new { message = "Gửi thông báo và push thành công." });

            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Push notification thất bại." });
        }
    }
}
