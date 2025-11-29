using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceScheduleDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class MaintenanceScheduleController : BaseApiController
    {
        private readonly IMaintenanceScheduleService _maintenanceScheduleService;

        public MaintenanceScheduleController(IMaintenanceScheduleService maintenanceScheduleService)
        {
            _maintenanceScheduleService = maintenanceScheduleService;
        }

        /// <summary>
        /// Tạo lịch bảo trì mới cho đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, TechnicianLead.  
        ///  
        /// **Lưu ý quan trọng:**
        /// - Mỗi CommonAreaObject chỉ có thể có **một** lịch bảo trì duy nhất.
        /// - Nếu đối tượng đã có lịch bảo trì, API sẽ trả về lỗi 409 Conflict.
        /// - NextScheduledDate không được trong quá khứ.
        /// - FrequencyInDays, RequiredTechnicians, EstimatedDuration phải > 0.
        ///  
        /// **Body mẫu:**
        /// ```json
        /// {
        ///   "commonAreaObjectId": 1,
        ///   "description": "Bảo trì định kỳ thang máy tầng 1",
        ///   "frequencyInDays": 30,
        ///   "nextScheduledDate": "2025-02-01",
        ///   "timePreference": "08:00:00",
        ///   "requiredTechniqueId": 2,
        ///   "requiredTechnicians": 2,
        ///   "estimatedDuration": 2.5
        /// }
        /// ```
        /// </remarks>
        /// <param name="dto">Thông tin lịch bảo trì cần tạo.</param>
        /// <returns>Thông báo tạo lịch bảo trì thành công.</returns>
        /// <response code="201">Tạo lịch bảo trì thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy CommonAreaObject hoặc Technique.</response>
        /// <response code="409">CommonAreaObject đã có lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateMaintenanceSchedule([FromBody] MaintenanceScheduleCreateDto dto)
        {
            var result = await _maintenanceScheduleService.CreateMaintenanceScheduleAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật lịch bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, TechnicianLead.  
        ///  
        /// **Lưu ý quan trọng:**
        /// - Mọi thay đổi sẽ được ghi lại vào `MaintenanceTrackingHistory`.
        /// - Có thể cập nhật một hoặc nhiều trường (partial update).
        /// - Các ràng buộc validation vẫn được áp dụng.
        ///  
        /// **Body mẫu (cập nhật một phần):**
        /// ```json
        /// {
        ///   "nextScheduledDate": "2025-03-01",
        ///   "requiredTechnicians": 3
        /// }
        /// ```
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì cần cập nhật.</param>
        /// <param name="dto">Thông tin cập nhật (các trường null sẽ không được cập nhật).</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật lịch bảo trì thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateMaintenanceSchedule(int id, [FromBody] MaintenanceScheduleUpdateDto dto)
        {
            var result = await _maintenanceScheduleService.UpdateMaintenanceScheduleAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa lịch bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager.  
        ///  
        /// **Lưu ý:**
        /// - Xóa hoàn toàn lịch bảo trì khỏi hệ thống.
        /// - Các tracking history liên quan cũng sẽ bị xóa (cascade delete).
        /// - Không thể khôi phục sau khi xóa.
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Xóa lịch bảo trì thành công.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteMaintenanceSchedule(int id)
        {
            var result = await _maintenanceScheduleService.DeleteMaintenanceScheduleAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết lịch bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Tất cả người dùng đã đăng nhập.  
        ///  
        /// **Thông tin trả về bao gồm:**
        /// - Thông tin lịch bảo trì đầy đủ
        /// - CommonAreaObject liên quan
        /// - Kỹ thuật yêu cầu (nếu có)
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì cần xem.</param>
        /// <returns>Thông tin chi tiết lịch bảo trì.</returns>
        /// <response code="200">Trả về thông tin lịch bảo trì.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MaintenanceScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMaintenanceScheduleById(int id)
        {
            var result = await _maintenanceScheduleService.GetMaintenanceScheduleByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách lịch bảo trì có phân trang, tìm kiếm và lọc.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Tất cả người dùng đã đăng nhập.  
        ///  
        /// **Tham số phân trang (PaginateDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Tìm kiếm theo mô tả hoặc tên đối tượng.  
        /// - <b>filter</b>: Lọc theo trạng thái (Active/Inactive).  
        /// - <b>sortBy</b>: Sắp xếp kết quả:
        ///   - <b>next_date</b>: Theo ngày bảo trì tiếp theo tăng dần.
        ///   - <b>next_date_desc</b>: Theo ngày bảo trì tiếp theo giảm dần.
        ///   - <b>frequency</b>: Theo chu kỳ tăng dần.
        ///   - <b>frequency_desc</b>: Theo chu kỳ giảm dần.
        ///   - <b>common_area_object</b>: Theo tên đối tượng A-Z.
        ///   - <b>common_area_object_desc</b>: Theo tên đối tượng Z-A.
        ///  
        /// **Query params:**
        /// - <b>commonAreaObjectId</b>: Lọc theo CommonAreaObject cụ thể (tùy chọn).
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm và lọc.</param>
        /// <param name="commonAreaObjectId">ID của CommonAreaObject để lọc (tùy chọn).</param>
        /// <returns>Danh sách lịch bảo trì có phân trang.</returns>
        /// <response code="200">Trả về danh sách lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<MaintenanceScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateMaintenanceSchedule(
            [FromQuery] PaginateDto dto,
            [FromQuery] int? commonAreaObjectId)
        {
            var result = await _maintenanceScheduleService.GetPaginateMaintenanceScheduleAsync(dto, commonAreaObjectId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy lịch bảo trì theo CommonAreaObject ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Tất cả người dùng đã đăng nhập.  
        ///  
        /// **Lưu ý:**
        /// - Mỗi CommonAreaObject chỉ có tối đa một lịch bảo trì.
        /// - Trả về null nếu chưa có lịch bảo trì.
        /// </remarks>
        /// <param name="commonAreaObjectId">ID của CommonAreaObject.</param>
        /// <returns>Thông tin lịch bảo trì hoặc null.</returns>
        /// <response code="200">Trả về lịch bảo trì hoặc null.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("by-common-area-object/{commonAreaObjectId}")]
        [Authorize]
        [ProducesResponseType(typeof(MaintenanceScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetByCommonAreaObjectId(int commonAreaObjectId)
        {
            var result = await _maintenanceScheduleService.GetByCommonAreaObjectIdAsync(commonAreaObjectId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy lịch sử thay đổi của lịch bảo trì.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, TechnicianLead.  
        ///  
        /// **Thông tin trả về:**
        /// - Tất cả các thay đổi được ghi nhận khi cập nhật lịch bảo trì.
        /// - Bao gồm: trường thay đổi, giá trị cũ, giá trị mới, người thay đổi, thời gian.
        /// - Sắp xếp theo thời gian mới nhất.
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì.</param>
        /// <returns>Danh sách lịch sử thay đổi.</returns>
        /// <response code="200">Trả về danh sách lịch sử.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpGet("{id}/tracking-history")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(IEnumerable<MaintenanceTrackingHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetTrackingHistory(int id)
        {
            var result = await _maintenanceScheduleService.GetTrackingHistoryAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt lịch bảo trì.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, TechnicianLead.  
        ///  
        /// **Lưu ý:**
        /// - Chuyển trạng thái từ Inactive sang Active.
        /// - Không thể kích hoạt nếu CommonAreaObject đã bị vô hiệu hóa.
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì cần kích hoạt.</param>
        /// <returns>Thông báo kích hoạt thành công.</returns>
        /// <response code="200">Kích hoạt lịch bảo trì thành công.</response>
        /// <response code="400">Lịch bảo trì đã ở trạng thái Active hoặc CommonAreaObject đã bị vô hiệu hóa.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ActivateMaintenanceSchedule(int id)
        {
            var result = await _maintenanceScheduleService.ActivateMaintenanceScheduleAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa lịch bảo trì.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, TechnicianLead.  
        ///  
        /// **Lưu ý:**
        /// - Chuyển trạng thái từ Active sang Inactive.
        /// - Lịch bảo trì bị vô hiệu hóa sẽ không tự động tạo RepairRequest.
        /// </remarks>
        /// <param name="id">ID của lịch bảo trì cần vô hiệu hóa.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
        /// <response code="200">Vô hiệu hóa lịch bảo trì thành công.</response>
        /// <response code="400">Lịch bảo trì đã ở trạng thái Inactive.</response>
        /// <response code="404">Không tìm thấy lịch bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeactivateMaintenanceSchedule(int id)
        {
            var result = await _maintenanceScheduleService.InactiveMaintenanceScheduleAsync(id);
            return Ok(result);
        }
    }
}