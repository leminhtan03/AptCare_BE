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
        /// <b>Phân quyền &amp; hành vi tự động:</b>
        /// <list type="bullet">
        ///   <item>🏠 <b>Resident (Cư dân):</b> chỉ xem các yêu cầu thuộc căn hộ của mình → <i>không cần nhập maintenanceRequestId</i>.</item>
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> chỉ xem yêu cầu được phân công → <i>có thể dùng tất cả tham số lọc</i>.</item>
        ///   <item>🧑‍💼 <b>Manager / TechnicianLead / Receptionist:</b> xem toàn bộ → <i>có thể dùng tất cả tham số lọc</i>.</item>
        /// </list>
        /// <br/>
        /// <b>Các trường lọc khả dụng:</b>
        /// <list type="bullet">
        ///   <item><c>apartmentId</c>: lọc theo căn hộ.</item>
        ///   <item><c>issueId</c>: lọc theo vấn đề.</item>
        ///   <item><c>maintenanceRequestId</c>: lọc theo yêu cầu bảo trì (nếu có).</item>
        ///   <item><c>isEmergency</c>: lọc theo loại yêu cầu (true = khẩn cấp, false = thường, null = tất cả).</item>
        /// </list>
        /// <br/>
        /// <b>Filter theo trạng thái (<c>filter</c>):</b>
        /// <list type="bullet">
        ///   <item><b>"Pending"</b> → yêu cầu mới tạo, chờ xử lý.</item>
        ///   <item><b>"Approved"</b> → đã được phê duyệt, đã gán kỹ thuật viên.</item>
        ///   <item><b>"InProgress"</b> → đang trong quá trình sửa chữa.</item>
        ///   <item><b>"Diagnosed"</b> → đã chẩn đoán xong.</item>
        ///   <item><b>"CompletedPendingVerify"</b> → hoàn tất, chờ xác nhận.</item>
        ///   <item><b>"AcceptancePendingVerify"</b> → nghiệm thu, chờ xác nhận.</item>
        ///   <item><b>"Completed"</b> → đã hoàn tất và được xác nhận.</item>
        ///   <item><b>"Cancelled"</b> → đã bị hủy.</item>
        ///   <item><b>"Rejected"</b> → bị từ chối.</item>
        /// </list>
        /// <br/>
        /// <b>Search:</b> tìm theo <c>Object</c> hoặc <c>Description</c>.
        /// <br/><br/>
        /// <b>SortBy (tùy chọn):</b>
        /// <list type="bullet">
        ///   <item><c>"apartment"</c>, <c>"apartment_desc"</c></item>
        ///   <item><c>"issue"</c>, <c>"issue_desc"</c></item>
        /// </list>
        /// </remarks>
        /// <param name="dto">Thông tin phân trang bao gồm: page (số trang), size (số lượng/trang), search (từ khóa tìm kiếm), filter (lọc theo trạng thái), sortBy (sắp xếp).</param>
        /// <param name="isEmergency">Lọc theo loại yêu cầu: true = khẩn cấp, false = thường, null = tất cả.</param>
        /// <param name="apartmentId">ID căn hộ cần lọc (tùy chọn).</param>
        /// <param name="issueId">ID vấn đề cần lọc (tùy chọn).</param>
        /// <param name="maintenanceRequestId">ID yêu cầu bảo trì liên quan (tùy chọn).</param>
        /// <returns>Danh sách yêu cầu sửa chữa có phân trang.</returns>
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

        /// <summary>
        /// Lấy chi tiết yêu cầu sửa chữa theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Phân quyền &amp; hành vi tự động:</b>
        /// <list type="bullet">
        ///   <item>🏠 <b>Resident (Cư dân):</b> chỉ được xem yêu cầu thuộc căn hộ của mình.</item>
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> chỉ xem được yêu cầu được phân công.</item>
        ///   <item>🧑‍💼 <b>Manager / TechnicianLead / Receptionist:</b> xem được toàn bộ.</item>
        /// </list>
        /// <br/>
        /// <b>Chi tiết bao gồm:</b>
        /// <list type="bullet">
        ///   <item>Thông tin người tạo yêu cầu, căn hộ, và vấn đề liên quan.</item>
        ///   <item>Danh sách lịch hẹn, báo cáo kiểm tra, báo cáo sửa chữa.</item>
        ///   <item>Lịch sử cập nhật trạng thái (RequestTracking).</item>
        ///   <item>Danh sách file đính kèm (Media).</item>
        /// </list>
        /// </remarks>
        /// <param name="id">ID của yêu cầu sửa chữa cần xem chi tiết.</param>
        /// <returns>Đối tượng <see cref="RepairRequestDetailDto"/> chứa toàn bộ thông tin chi tiết của yêu cầu.</returns>
        /// <response code="200">Trả về thông tin yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="404">Yêu cầu không tồn tại.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(RepairRequestDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RepairRequestDetailDto>> GetRepairRequestById([FromRoute] int id)
        {
            var result = await _repairRequestService.GetRepairRequestByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Chuyển đổi trạng thái của yêu cầu sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <list type="bullet">
        ///   <item>🧑‍💼 <b>Manager / TechnicianLead:</b> có thể chuyển đổi hầu hết các trạng thái.</item>
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> có thể cập nhật trạng thái liên quan đến công việc được giao (InProgress, Diagnosed, CompletedPendingVerify).</item>
        ///   <item>🏢 <b>Receptionist (Lễ tân):</b> có thể phê duyệt/từ chối yêu cầu (Approved, Rejected).</item>
        ///   <item>🏠 <b>Resident (Cư dân):</b> có thể xác nhận hoàn tất hoặc hủy yêu cầu của mình (Completed, Cancelled).</item>
        /// </list>
        /// <br/>
        /// <b>Các trạng thái có thể chuyển đổi (RequestStatus):</b>
        /// <list type="bullet">
        ///   <item><b>"Pending"</b> → yêu cầu mới tạo, chờ xử lý.</item>
        ///   <item><b>"Approved"</b> → đã được phê duyệt, đã gán kỹ thuật viên.</item>
        ///   <item><b>"InProgress"</b> → đang trong quá trình sửa chữa.</item>
        ///   <item><b>"Diagnosed"</b> → đã chẩn đoán xong.</item>
        ///   <item><b>"CompletedPendingVerify"</b> → hoàn tất, chờ xác nhận.</item>
        ///   <item><b>"AcceptancePendingVerify"</b> → nghiệm thu, chờ xác nhận.</item>
        ///   <item><b>"Completed"</b> → đã hoàn tất và được xác nhận.</item>
        ///   <item><b>"Cancelled"</b> → đã bị hủy.</item>
        ///   <item><b>"Rejected"</b> → bị từ chối.</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>RepairRequestId</c>: ID của yêu cầu sửa chữa cần cập nhật trạng thái.</item>
        ///   <item><c>NewStatus</c>: Trạng thái mới cần chuyển đổi.</item>
        ///   <item><c>Note</c> (tùy chọn): Ghi chú kèm theo khi chuyển đổi trạng thái.</item>
        /// </list>
        /// </remarks>
        /// <param name="dto">Thông tin chuyển đổi trạng thái bao gồm: RepairRequestId, NewStatus, Note (tùy chọn).</param>
        /// <returns>Thông báo cập nhật trạng thái thành công.</returns>
        /// <response code="200">Cập nhật trạng thái yêu cầu sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc chuyển đổi trạng thái không được phép.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền thực hiện thao tác này.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPatch("toggle-status")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Technician)}, {nameof(AccountRole.Receptionist)}, {nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ToggleRepairRequestStatus([FromQuery] ToggleRRStatus dto)
        {
            var result = await _repairRequestService.ToggleRepairRequestStatusAsync(dto);
            if (result)
            {
                return Ok("Cập nhật trạng thái yêu cầu sửa chữa thành công.");
            }
            else
            {
                return BadRequest("Cập nhật trạng thái yêu cầu sửa chữa thất bại.");
            }
        }
    }
}