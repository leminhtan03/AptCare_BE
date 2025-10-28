using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Hub;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AptCare.Api.Controllers
{
    public class MessageController : BaseApiController
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(IMessageService messageService, IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Gửi tin nhắn văn bản trong một cuộc trò chuyện.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// Tin nhắn sẽ được lưu vào cơ sở dữ liệu và gửi thông báo đến các thành viên khác trong cuộc trò chuyện.
        /// </remarks>
        /// <param name="dto">Thông tin tin nhắn văn bản.</param>
        /// <returns>Thông báo tạo tin nhắn thành công.</returns>
        /// <response code="201">Tin nhắn được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpPost("text")]
        [Authorize]
        [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateTextMessage([FromBody] TextMessageCreateDto dto)
        {
            var result = await _messageService.CreateTextMessageAsync(dto);
            await _hubContext.Clients.Group(result.Slug).SendAsync("ReceiveMessage", result);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Gửi tin nhắn có tệp đính kèm (ảnh, video, âm thanh, tài liệu, v.v...).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// File sẽ được tải lên Cloudinary, sau đó gửi liên kết trong tin nhắn.  
        /// Loại tin nhắn sẽ được xác định tự động dựa trên `ContentType` của file.
        /// </remarks>
        /// <param name="conversationId">ID cuộc trò chuyện.</param>
        /// <param name="file">Tệp tin gửi kèm.</param>
        /// <returns>Thông báo tạo tin nhắn thành công.</returns>
        /// <response code="201">Tin nhắn được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpPost("file")]
        [Authorize]
        [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateFileMessage(int conversationId, IFormFile file)
        {
            var result = await _messageService.CreateFileMessageAsync(conversationId, file);
            await _hubContext.Clients.Group(result.Slug).SendAsync("ReceiveMessage", result);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Lấy danh sách tin nhắn trong một cuộc trò chuyện (phân trang).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// Có thể truyền tham số `before` để tải các tin nhắn cũ hơn (hỗ trợ tính năng "load more").  
        /// Kết quả được sắp xếp theo thời gian giảm dần (tin mới nhất trước).
        /// </remarks>
        /// <param name="conversationId">ID cuộc trò chuyện.</param>
        /// <param name="before">Chỉ lấy tin nhắn trước thời điểm này.</param>
        /// <param name="pageSize">Số lượng tin nhắn trên một trang.</param>
        /// <returns>Danh sách tin nhắn phân trang.</returns>
        /// <response code="200">Trả về danh sách tin nhắn.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<MessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMessages(
            int conversationId,
            [FromQuery] DateTime? before = null,
            [FromQuery] int pageSize = 20)
        {
            var result = await _messageService.GetPaginateMessagesAsync(conversationId, before, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một tin nhắn cụ thể.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// Trả về thông tin người gửi, nội dung, thời gian, loại tin nhắn và nếu có thì cả tin nhắn được trả lời.
        /// </remarks>
        /// <param name="id">ID tin nhắn.</param>
        /// <returns>Chi tiết tin nhắn.</returns>
        /// <response code="200">Trả về chi tiết tin nhắn.</response>
        /// <response code="404">Không tìm thấy tin nhắn.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMessageById(int id)
        {
            var result = await _messageService.GetMessageByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Đánh dấu tin nhắn là đã giao đến người nhận (Delivered).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập (người nhận tin nhắn).  
        /// Cập nhật trạng thái `MessageStatus.Delivered`.
        /// </remarks>
        /// <param name="id">ID tin nhắn cần đánh dấu đã giao.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Tin nhắn được đánh dấu là đã giao.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy tin nhắn.</response>
        [HttpPatch("{conversationId}/mark-as-delivered")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> MarkAsDeliveried(int conversationId)
        {
            await _messageService.MarkAsDeliveredAsync(conversationId);
            return NoContent();
        }

        /// <summary>
        /// Đánh dấu tin nhắn là đã đọc (Read).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập (người nhận tin nhắn).  
        /// Cập nhật trạng thái `MessageStatus.Read`.
        /// </remarks>
        /// <param name="id">ID tin nhắn cần đánh dấu đã đọc.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Tin nhắn được đánh dấu là đã đọc.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy tin nhắn.</response>
        [HttpPatch("{conversationId}/mark-as-read")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> MarkAsRead(int conversationId)
        {
            await _messageService.MarkAsReadAsync(conversationId);
            return NoContent();
        }

    }

}
