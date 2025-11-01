using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class RepairRequestController : BaseApiController
    {
        private readonly IRepairRequestService _repairRequestService;

        public RepairRequestController(IRepairRequestService repairRequestService)
        {
            _repairRequestService = repairRequestService;
        }

        /// <summary>
        /// Tạo yêu cầu sửa chữa thông thường.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** cư dân hoặc lễ tân.  
        /// Hệ thống sẽ kiểm tra căn hộ, kỹ thuật viên phù hợp và tạo cuộc hẹn tương ứng.  
        /// Nếu có tệp đính kèm, file sẽ được tải lên Cloudinary.
        /// </remarks>
        /// <param name="dto">Thông tin yêu cầu sửa chữa.</param>
        /// <returns>Thông báo tạo yêu cầu thành công.</returns>
        /// <response code="201">Tạo yêu cầu sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy căn hộ hoặc vấn đề liên quan.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("normal")]
        [Authorize(Roles = $"{nameof(AccountRole.Resident)}, {nameof(AccountRole.Receptionist)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateNormalRepairRequest([FromForm] RepairRequestNormalCreateDto dto)
        {
            var result = await _repairRequestService.CreateNormalRepairRequestAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Tạo yêu cầu sửa chữa Khẩn cấp.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** cư dân hoặc lễ tân.  
        /// Hệ thống sẽ kiểm tra căn hộ, kỹ thuật viên phù hợp và tạo cuộc hẹn tương ứng.  
        /// Nếu có tệp đính kèm, file sẽ được tải lên Cloudinary.
        /// </remarks>
        /// <param name="dto">Thông tin yêu cầu sửa chữa.</param>
        /// <returns>Thông báo tạo yêu cầu thành công.</returns>
        /// <response code="201">Tạo yêu cầu sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy căn hộ hoặc vấn đề liên quan.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("emergency")]
        [Authorize(Roles = $"{nameof(AccountRole.Resident)}, {nameof(AccountRole.Receptionist)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateEmergencyRepairRequest([FromForm] RepairRequestEmergencyCreateDto dto)
        {
            var result = await _repairRequestService.CreateEmergencyRepairRequestAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu sửa chữa có phân trang.
        /// </summary>
        /// <remarks>
        /// **Phân quyền & hành vi tự động:**
        /// - 🏠 **Resident (Cư dân):** chỉ xem các yêu cầu thuộc căn hộ của mình → *không cần nhập maintenanceRequestId*.  
        /// - 🔧 **Technician (Kỹ thuật viên):** chỉ xem yêu cầu được phân công → *có thể dùng tất cả tham số lọc*.
        /// - 🧑‍💼 **Manager / TechnicianLead / Receptionist:** xem toàn bộ → *có thể dùng tất cả tham số lọc*.  
        ///
        /// **Các trường lọc khả dụng:**
        /// - `apartmentId`: lọc theo căn hộ.
        /// - `issueId`: lọc theo vấn đề
        /// - `maintenanceRequestId`: lọc theo yêu cầu bảo trì (nếu có).
        ///
        /// **Filter theo trạng thái (`filter`):**
        /// - "Pending" → yêu cầu mới tạo.  
        /// - "Approved" → đã được phê duyệt.  
        /// - "InProgress" → đang sửa chữa.  
        /// - "Completed" → đã hoàn tất.  
        /// - "Rejected" → bị từ chối.  
        /// - "Cancelled" → bị hủy.  
        ///
        /// **Search:** tìm theo `Object` hoặc `Description`.  
        ///
        /// **SortBy (tùy chọn):**
        /// - `"apartment"`, `"apartment_desc"`, `"issue"`, `"issue_desc"`.
        /// </remarks>
        /// <param name="dto">Thông tin phân trang (page, size, search, filter, sortBy).</param>
        /// <param name="apartmentId">Lọc theo căn hộ.</param>
        /// <param name="issueId">Lọc theo vấn đề cần sửa.</param>
        /// <param name="maintenanceRequestId">Lọc theo yêu cầu bảo trì (nếu có).</param>
        /// <returns>Danh sách yêu cầu sửa chữa theo trang.</returns>
        /// <response code="200">Trả về danh sách yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("paginate")]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<RepairRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IPaginate<RepairRequestDto>>> GetPaginateRepairRequests(
            [FromQuery] PaginateDto dto,
            [FromQuery] bool? isEmergency,
            [FromQuery] int? apartmentId,
            [FromQuery] int? issueId,
            [FromQuery] int? maintenanceRequestId)
        {
            var result = await _repairRequestService.GetPaginateRepairRequestAsync(dto, isEmergency, apartmentId, issueId, maintenanceRequestId);
            return Ok(result);
        }
    }
}