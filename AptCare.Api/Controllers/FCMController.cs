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
        /// <remarks>
        /// <b>Chức năng:</b> Gửi thông báo push đến một thiết bị thông qua FCM token.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>fcmToken</b>: Token FCM của thiết bị nhận thông báo (bắt buộc).</li>
        ///   <li><b>title</b>: Tiêu đề thông báo (bắt buộc).</li>
        ///   <li><b>body</b>: Nội dung thông báo (bắt buộc).</li>
        ///   <li><b>image</b>: Đường dẫn ảnh hiển thị trong thông báo (tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="fcmToken">Token FCM của thiết bị nhận thông báo.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="body">Nội dung thông báo.</param>
        /// <param name="image">Đường dẫn ảnh hiển thị (tùy chọn).</param>
        /// <returns>Thông báo gửi thành công hoặc thất bại.</returns>
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
        /// Gửi thông báo đến 1 thiết bị cụ thể (dành cho web client).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Gửi thông báo push đến một thiết bị web thông qua FCM token.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>fcmToken</b>: Token FCM của thiết bị nhận thông báo (bắt buộc).</li>
        ///   <li><b>title</b>: Tiêu đề thông báo (bắt buộc).</li>
        ///   <li><b>body</b>: Nội dung thông báo (bắt buộc).</li>
        ///   <li><b>image</b>: Đường dẫn ảnh hiển thị trong thông báo (tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="fcmToken">Token FCM của thiết bị nhận thông báo.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="body">Nội dung thông báo.</param>
        /// <param name="image">Đường dẫn ảnh hiển thị (tùy chọn).</param>
        /// <returns>Thông báo gửi thành công hoặc thất bại.</returns>
        [HttpPost("web")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToSinglaaaeAsync(string fcmToken, string title, string body, string? image = null)
        {
            var result = await _fcmService.PushNotificationWebAsync(fcmToken, title, body, image);

            if (!result)
                return StatusCode(StatusCodes.Status500InternalServerError, "Gửi thông báo thất bại.");
            return Ok("Gửi thông báo thành công.");
        }

        /// <summary>
        /// Gửi thông báo đến nhiều thiết bị (tối đa 1000 token / request).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Gửi thông báo push đến nhiều thiết bị cùng lúc thông qua danh sách FCM token.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>fcmTokens</b>: Danh sách token FCM của các thiết bị nhận thông báo (bắt buộc, tối đa 1000).</li>
        ///   <li><b>title</b>: Tiêu đề thông báo (bắt buộc).</li>
        ///   <li><b>body</b>: Nội dung thông báo (bắt buộc).</li>
        ///   <li><b>image</b>: Đường dẫn ảnh hiển thị trong thông báo (tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="fcmTokens">Danh sách token FCM của các thiết bị nhận thông báo.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="body">Nội dung thông báo.</param>
        /// <param name="image">Đường dẫn ảnh hiển thị (tùy chọn).</param>
        /// <returns>Thông báo gửi thành công hoặc thất bại.</returns>
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

        //[HttpPost("singleaaaa")]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> PushToSingleaaaaAsync(string fcmToken, string title, string body, string image)
        //{
        //    var result = await _fcmService.PushNotificationaaaAsync(fcmToken, title, body, image);

        //    if (!result)
        //        return StatusCode(StatusCodes.Status500InternalServerError, "Gửi thông báo thất bại.");
        //    return Ok("Gửi thông báo thành công.");
        //}
    }
}
