using AptCare.Service.Dtos.Account;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class FCMController : BaseApiController
    {
        private readonly IFCMService _fcmService;

        public FCMController(IFCMService fcmService)
        {
            _fcmService = fcmService;
        }

        /// <summary>
        /// Gửi thông báo đến 1 thiết bị cụ thể.
        /// </summary>
        [HttpPost("single")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToSingleAsync(string fcmToken, string title, string body, string? image = null)
        {
            var result = await _fcmService.PushNotificationAsync(fcmToken, title, body, image);

            if (!result)
                return StatusCode(StatusCodes.Status500InternalServerError, "Gửi thông báo thất bại.");
            return Ok("Gửi thông báo thành công.");
        }

        /// <summary>
        /// Gửi thông báo đến nhiều thiết bị (tối đa 1000 token / request).
        /// </summary>
        [HttpPost("multicast")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToMultipleAsync(IEnumerable<string> fcmTokens, string title, string body, string? image = null)
        {
            var result = await _fcmService.PushMulticastAsync(fcmTokens, title, body, image);
            if (!result)
                return StatusCode(StatusCodes.Status500InternalServerError, "Gửi thông báo thất bại.");
            return Ok("Gửi thông báo thành công đến nhiều thiết bị.");
        }

        [HttpPost("singleaaaa")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToSingleaaaaAsync(string fcmToken, string title, string body, string image)
        {
            var result = await _fcmService.PushNotificationaaaAsync(fcmToken, title, body, image);

            if (!result)
                return StatusCode(StatusCodes.Status500InternalServerError, "Gửi thông báo thất bại.");
            return Ok("Gửi thông báo thành công.");
        }
    }
}
