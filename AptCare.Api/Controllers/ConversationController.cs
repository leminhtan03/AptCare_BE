using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.ChatDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [Authorize] 
    public class ConversationController : BaseApiController
    {
        private readonly IConversationService _conversationService;

        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        /// <summary>
        /// Tạo cuộc trò chuyện mới giữa nhiều người dùng.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Tạo cuộc trò chuyện với 1 người thì không cần nhập tiêu đề
        /// Tạo cuộc trò chuyện với nhiều người: Nếu không nhập tiêu đề, hệ thống tự động tạo tiêu đề từ tên các người tham gia.  
        /// Nếu 2 người đã có cuộc trò chuyện, không thể tạo lại.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateConversation([FromBody] ConversationCreateDto dto)
        {
            var result = await _conversationService.CreateConversationAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Lấy danh sách các cuộc trò chuyện của người dùng hiện tại.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Trả về danh sách gồm: tiêu đề, thành viên, tin nhắn gần nhất, và thời gian cập nhật cuối.
        /// </remarks>
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<ConversationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMyConversations()
        {
            var result = await _conversationService.GetMyConversationsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một cuộc trò chuyện cụ thể theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Bao gồm danh sách tin nhắn, thông tin người tham gia và trạng thái đọc.
        /// </remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetConversationById(int id)
        {
            var result = await _conversationService.GetConversationByIdAsync(id);
            return Ok(result);
        }



        /// <summary>
        /// Tắt thông báo cho một cuộc trò chuyện.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.    
        /// Dùng khi người dùng không muốn nhận thông báo tin nhắn mới.
        /// </remarks>
        [HttpPatch("{id}/mute")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> MuteConversation(int id)
        {
            var result = await _conversationService.MuteConversationAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Bật lại thông báo cho một cuộc trò chuyện.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// </remarks>
        [HttpPatch("{id}/unmute")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> UnmuteConversation(int id)
        {
            var result = await _conversationService.UnmuteConversationAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra cuộc trò chuyện đã tồn tại giữa người dùng hiện tại và userId.
        /// </summary>
        /// <param name="userId">ID người dùng cần kiểm tra.</param>
        /// <returns>ID cuộc trò chuyện nếu tồn tại, trả về null nếu chưa có.</returns>
        /// <response code="200">Trả về ID cuộc trò chuyện hoặc null.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("check-existing/{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int?>> CheckExistingConversation(int userId)
        {
            var conversationId = await _conversationService.CheckExistingConversationAsync(userId);
            return Ok(conversationId);
        }
    }
}
