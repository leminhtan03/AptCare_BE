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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Tạo cuộc trò chuyện với 1 người thì không cần nhập tiêu đề.<br/>
        /// Tạo cuộc trò chuyện với nhiều người: Nếu không nhập tiêu đề, hệ thống tự động tạo tiêu đề từ tên các người tham gia.<br/>
        /// Nếu 2 người đã có cuộc trò chuyện, không thể tạo lại.
        /// <br/><b>ConversationCreateDto:</b>
        /// <ul>
        ///   <li><b>Title</b>: Tiêu đề cuộc trò chuyện (tùy chọn).</li>
        ///   <li><b>UserIds</b>: Danh sách ID người dùng tham gia (bắt buộc, ít nhất 1).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">Thông tin cuộc trò chuyện cần tạo.</param>
        /// <returns>Thông báo tạo cuộc trò chuyện thành công.</returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Trả về danh sách gồm: tiêu đề, thành viên, tin nhắn gần nhất, và thời gian cập nhật cuối.
        /// <br/><b>ConversationDto:</b>
        /// <ul>
        ///   <li><b>ConversationId</b>: ID cuộc trò chuyện.</li>
        ///   <li><b>Title</b>: Tiêu đề cuộc trò chuyện.</li>
        ///   <li><b>Slug</b>: Đường dẫn slug của cuộc trò chuyện.</li>
        ///   <li><b>Image</b>: Ảnh đại diện cuộc trò chuyện.</li>
        ///   <li><b>IsMuted</b>: Đã tắt thông báo hay chưa.</li>
        ///   <li><b>LastMessage</b>: Tin nhắn gần nhất.</li>
        ///   <li><b>Participants</b>: Danh sách thành viên (<b>ParticipantDto</b>).</li>
        /// </ul>
        /// <b>ParticipantDto:</b>
        /// <ul>
        ///   <li><b>UserId</b>: ID người dùng.</li>
        ///   <li><b>FirstName</b>: Tên.</li>
        ///   <li><b>LastName</b>: Họ.</li>
        ///   <li><b>JoinedAt</b>: Thời gian tham gia.</li>
        /// </ul>
        /// </remarks>
        /// <returns>Danh sách cuộc trò chuyện của người dùng hiện tại.</returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Bao gồm danh sách tin nhắn, thông tin người tham gia và trạng thái đọc.
        /// <br/><b>ConversationDto:</b>
        /// <ul>
        ///   <li><b>ConversationId</b>: ID cuộc trò chuyện.</li>
        ///   <li><b>Title</b>: Tiêu đề cuộc trò chuyện.</li>
        ///   <li><b>Slug</b>: Đường dẫn slug của cuộc trò chuyện.</li>
        ///   <li><b>Image</b>: Ảnh đại diện cuộc trò chuyện.</li>
        ///   <li><b>IsMuted</b>: Đã tắt thông báo hay chưa.</li>
        ///   <li><b>LastMessage</b>: Tin nhắn gần nhất.</li>
        ///   <li><b>Participants</b>: Danh sách thành viên (<b>ParticipantDto</b>).</li>
        /// </ul>
        /// <b>ParticipantDto:</b>
        /// <ul>
        ///   <li><b>UserId</b>: ID người dùng.</li>
        ///   <li><b>FirstName</b>: Tên.</li>
        ///   <li><b>LastName</b>: Họ.</li>
        ///   <li><b>JoinedAt</b>: Thời gian tham gia.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID cuộc trò chuyện cần lấy chi tiết.</param>
        /// <returns>Thông tin chi tiết cuộc trò chuyện.</returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Dùng khi người dùng không muốn nhận thông báo tin nhắn mới.
        /// </remarks>
        /// <param name="id">ID cuộc trò chuyện cần tắt thông báo.</param>
        /// <returns>Thông báo tắt thông báo thành công.</returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// </remarks>
        /// <param name="id">ID cuộc trò chuyện cần bật lại thông báo.</param>
        /// <returns>Thông báo bật lại thông báo thành công.</returns>
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
