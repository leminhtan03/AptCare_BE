using AptCare.Api.Controllers;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.SlotDtos;
using AptCare.Service.Dtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.API.Controllers
{
    public class SlotController : BaseApiController
    {
        private readonly ISlotService _slotService;

        public SlotController(ISlotService slotService)
        {
            _slotService = slotService;
        }

        /// <summary>
        /// Lấy danh sách slot có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///
        /// **Tham số phân trang (PaginateDto):**  
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm theo tên slot.  
        /// - <b>filter</b>: Lọc theo trạng thái slot (active/inactive).  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp kết quả:  
        ///   - <b>display</b>: Theo thứ tự hiển thị tăng dần.  
        ///   - <b>display_desc</b>: Theo thứ tự hiển thị giảm dần.  
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, lọc và sắp xếp.</param>
        /// <returns>Danh sách slot kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách slot.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<SlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateSlots([FromQuery] PaginateDto dto)
        {
            var result = await _slotService.GetPaginateSlotAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách slot đang hoạt động (Active).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <returns>Danh sách slot đang hoạt động.</returns>
        [HttpGet("active")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<SlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetActiveSlots()
        {
            var result = await _slotService.GetSlotsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết slot theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của slot cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết slot.</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetSlotById(int id)
        {
            var result = await _slotService.GetSlotByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một slot.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///
        /// **Body mẫu:**  
        /// ```json
        /// {
        ///   "slotName": "Ca sáng",
        ///   "fromTime": "08:00:00",
        ///   "toTime": "16:00:00",
        ///   "displayOrder": 1
        /// }
        /// ```
        /// </remarks>
        /// <param name="dto">Thông tin slot cần tạo.</param>
        /// <returns>Thông báo tạo slot thành công.</returns>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateSlot([FromBody] SlotCreateDto dto)
        {
            var result = await _slotService.CreateSlotAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin slot theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///
        /// **Body mẫu:**  
        /// ```json
        /// {
        ///   "slotName": "Ca tối",
        ///   "fromTime": "16:00:00",
        ///   "toTime": "23:59:00",
        ///   "displayOrder": 2,
        ///   "status": 1
        /// }
        /// ```
        /// <br/>Giá trị status:  
        /// - 1: Active  
        /// - 2: Inactive
        /// </remarks>
        /// <param name="id">ID của slot cần cập nhật.</param>
        /// <param name="dto">Thông tin slot cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateSlot(int id, [FromBody] SlotUpdateDto dto)
        {
            var result = await _slotService.UpdateSlotAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa slot theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///
        /// Việc xóa slot sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của slot cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteSlot(int id)
        {
            var result = await _slotService.DeleteSlotAsync(id);
            return Ok(result);
        }
    }
}
