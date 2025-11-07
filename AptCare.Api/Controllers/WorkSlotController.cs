using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.WorkSlotDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class WorkSlotController : BaseApiController
    {
        private readonly IWorkSlotService _workSlotService;

        public WorkSlotController(IWorkSlotService workSlotService)
        {
            _workSlotService = workSlotService;
        }

        /// <summary>
        /// Tạo lịch làm việc liên tục từ ngày bắt đầu đến ngày kết thúc (cùng 1 ca làm việc).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ  
        ///  
        /// **Ràng buộc:**  
        /// - 'FromDate' phải nhỏ hơn hoặc bằng 'ToDate'.  
        /// - Mỗi kỹ thuật viên, ngày và slot chỉ có thể tồn tại một lịch làm việc duy nhất.  
        /// 
        /// <response code="201">Tạo lịch thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// </remarks>
        [HttpPost("from-date-to-date")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateWorkSlotsFromDateToDate([FromBody] WorkSlotCreateFromDateToDateDto dto)
        {
            var result = await _workSlotService.CreateWorkSlotsFromDateToDateAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Tạo lịch làm việc cho danh sách ngày và ca cụ thể.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager.  
        ///  
        /// Cho phép tạo nhiều lịch làm việc không liên tiếp — ví dụ:  
        /// - 01/10/2025 (Slot1)  
        /// - 03/10/2025 (Slot2)  
        /// - 05/10/2025 (Slot3)  
        ///  
        ///  
        /// **Ràng buộc:** Mỗi kỹ thuật viên, ngày và slot chỉ có thể có 1 lịch duy nhất.
        /// 
        /// <response code="201">Tạo lịch thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// </remarks>
        [HttpPost("date-slot-list")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateWorkSlotsDateSlot([FromBody] WorkSlotCreateDateSlotDto dto)
        {
            var result = await _workSlotService.CreateWorkSlotsDateSlotAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật lịch làm việc theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Cập nhật thông tin như: kỹ thuật viên, ngày làm việc, ca làm và trạng thái.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ  
        ///  
        /// **Ràng buộc:** Không thể cập nhật sang lịch đã tồn tại (trùng TechnicianId, Date, Slot).
        /// <response code="200">Cập nhật lịch thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy lịch.</response>
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateWorkSlot(int id, [FromBody] WorkSlotUpdateDto dto)
        {
            var result = await _workSlotService.UpdateWorkSlotAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa lịch làm việc theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Xóa hoàn toàn lịch làm việc của kỹ thuật viên khỏi hệ thống.  
        /// **Lưu ý:** Không thể khôi phục lại lịch đã xóa.
        /// 
        /// <response code="200">Xóa lịch thành công.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy lịch.</response>
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteWorkSlot(int id)
        {
            var result = await _workSlotService.DeleteWorkSlotAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Xem lịch làm việc của kỹ thuật viên (có thể lọc theo ID, thời gian, trạng thái).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Trả về danh sách lịch làm việc theo khoảng thời gian, nhóm theo ngày và ca làm việc.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ
        ///   
        /// <response code="200">Lấy lịch thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// </remarks>
        [HttpGet("technician-schedule")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(IEnumerable<WorkSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetTechnicianSchedule(
            [FromQuery] int? technicianId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] WorkSlotStatus? status)
        {
            var result = await _workSlotService.GetTechnicianScheduleAsync(technicianId, fromDate, toDate, status);
            return Ok(result);
        }

        /// <summary>
        /// Xem lịch làm việc cá nhân của kỹ thuật viên đang đăng nhập.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician.  
        ///  
        /// Trả về danh sách lịch làm việc của chính người dùng đăng nhập trong khoảng thời gian xác định.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ
        ///   
        /// <response code="200">Lấy lịch thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// </remarks>
        [HttpGet("my-schedule")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(IEnumerable<WorkSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetMySchedule(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] WorkSlotStatus? status)
        {
            var result = await _workSlotService.GetMyScheduleAsync(fromDate, toDate, status);
            return Ok(result);
        }

        /// <summary>
        /// Điểm danh đầu giờ (Check-in) cho kỹ thuật viên.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician.  
        ///  
        /// Kỹ thuật viên điểm danh để xác nhận bắt đầu ca làm việc.  
        ///  
        /// **Ràng buộc:**  
        /// - Lịch làm việc phải tồn tại.  
        /// - Trạng thái phải là `NotStarted`.  
        /// 
        /// <response code="200">Điểm danh đầu giờ thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy lịch làm việc.</response>
        /// </remarks>
        [HttpPatch("check-in")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CheckIn([FromQuery] DateOnly date, [FromQuery] int slotId)
        {
            var result = await _workSlotService.CheckInAsync(date, slotId);
            return Ok(result);
        }

        /// <summary>
        /// Điểm danh cuối giờ (Check-out) cho kỹ thuật viên.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician.  
        ///  
        /// Kỹ thuật viên điểm danh cuối giờ để xác nhận hoàn thành ca làm việc.  
        ///  
        /// **Ràng buộc:**  
        /// - Lịch làm việc phải tồn tại.  
        /// - Trạng thái phải là `Working`.  
        /// 
        /// <response code="200">Điểm danh cuối giờ thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy lịch làm việc.</response>
        /// </remarks>
        [HttpPatch("check-out")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CheckOut([FromQuery] DateOnly date, [FromQuery] int slotId)
        {
            var result = await _workSlotService.CheckOutAsync(date, slotId);
            return Ok(result);
        }

    }
}
