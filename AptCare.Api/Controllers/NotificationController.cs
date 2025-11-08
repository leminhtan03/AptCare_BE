using AptCare.Api.Controllers;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.API.Controllers
{
    public class NotificationController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Gửi thông báo broadcast đến toàn bộ hoặc nội bộ hệ thống.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager hoặc TechnicianLead.  
        /// - Nếu `Type = General` → gửi cho toàn bộ người dùng.  
        /// - Nếu `Type = Internal` → chỉ gửi cho nhân viên nội bộ (không gửi cho cư dân).  
        ///  
        /// Sau khi gửi, hệ thống sẽ lưu lại thông báo trong database và push đến thiết bị qua FCM.
        /// </remarks>
        /// <param name="dto">Thông tin nội dung thông báo.</param>
        /// <returns>Thông báo kết quả gửi.</returns>
        /// <response code="200">Gửi thông báo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("broadcast")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> BroadcastNotification([FromBody] NotificationCreateDto dto)
        {
            var result = await _notificationService.BroadcastNotificationAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách thông báo của người dùng hiện tại (có phân trang).
        /// </summary>
        /// <remarks>
        /// - Tự động xác định `ReceiverId` theo người dùng đang đăng nhập.  
        /// - Có thể lọc bằng `filter`:  
        ///   - `"read"` → chỉ thông báo đã đọc  
        ///   - `"not-read"` → chỉ thông báo chưa đọc  
        /// - Có thể tìm kiếm bằng `search` (theo title/description).  
        /// - Sort mặc định: mới nhất trước.
        /// </remarks>
        /// <param name="dto">Thông tin phân trang (page, size, search, filter, sortBy).</param>
        /// <returns>Danh sách thông báo.</returns>
        /// <response code="200">Trả về danh sách thông báo của người dùng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("my")]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<NotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IPaginate<NotificationDto>>> GetMyNotifications([FromQuery] PaginateDto dto)
        {
            var result = await _notificationService.GetMyNotificationPaginateAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu các thông báo là đã đọc.
        /// </summary>
        /// <remarks>
        /// Người dùng chỉ có thể đánh dấu thông báo thuộc về chính mình.
        /// </remarks>
        /// <param name="ids">Danh sách ID các thông báo cần đánh dấu là đã đọc.</param>
        /// <returns>Thông báo trạng thái thành công.</returns>
        /// <response code="200">Đánh dấu thành công.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Cố gắng đánh dấu thông báo không thuộc quyền sở hữu.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPatch("mark-as-read")]
        [Authorize]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> MarkAsRead([FromBody] IEnumerable<int> ids)
        {
            var result = await _notificationService.MarkAsReadAsync(ids);
            return Ok(result);
        }
    }
}
