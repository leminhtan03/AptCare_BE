using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [Authorize]
    public class MessageController : BaseApiController
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        /// <summary>
        /// Gửi tin nhắn văn bản trong một cuộc trò chuyện.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Tin nhắn sẽ được lưu vào cơ sở dữ liệu và gửi thông báo đến các thành viên khác trong cuộc trò chuyện.
        /// </remarks>
        [HttpPost("text")]
        public async Task<ActionResult> CreateTextMessage([FromBody] TextMessageCreateDto dto)
        {
            var result = await _messageService.CreateTextMessageAsync(dto);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Gửi tin nhắn có tệp đính kèm (ảnh, video, âm thanh, tài liệu, v.v...).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// File sẽ được tải lên Cloudinary, sau đó gửi liên kết trong tin nhắn.  
        /// Loại tin nhắn sẽ được xác định tự động dựa trên `ContentType` của file.
        /// </remarks>
        [HttpPost("file")]
        public async Task<ActionResult> CreateFileMessage(int conversationId, IFormFile file)
        {
            var result = await _messageService.CreateFileMessageAsync(conversationId, file);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Lấy danh sách tin nhắn trong một cuộc trò chuyện (phân trang).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Có thể truyền tham số `before` để tải các tin nhắn cũ hơn (hỗ trợ tính năng "load more").  
        /// Kết quả được sắp xếp theo thời gian giảm dần (tin mới nhất trước).
        /// </remarks>
        [HttpGet]
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
        ///  
        /// Trả về thông tin người gửi, nội dung, thời gian, loại tin nhắn và nếu có thì cả tin nhắn được trả lời.
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetMessageById(int id)
        {
            var result = await _messageService.GetMessageByIdAsync(id);
            return Ok(result);
        }
    }
}
