using AptCare.Api.Controllers;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class AppointmentController : BaseApiController
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        /// <summary>
        /// Tạo lịch hẹn sửa chữa mới.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead (Trưởng bộ phận kĩ thuật) 
        /// Dùng khi trưởng bộ phận kĩ thuật muốn đặt lịch cho yêu cầu sửa chữa cụ thể.  
        /// 
        /// **Yêu cầu:**  
        /// - `RepairRequestId` phải tồn tại.  
        /// - Thời gian bắt đầu (`StartTime`) và kết thúc (`EndTime`) hợp lệ.  
        /// 
        /// **Resident và Technician** không được phép gọi API này.
        /// </remarks>
        /// <param name="dto">Thông tin lịch hẹn.</param>
        /// <returns>Thông báo tạo lịch hẹn thành công.</returns>
        /// <response code="201">Tạo lịch hẹn thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateAppointment([FromBody] AppointmentCreateDto dto)
        {
            var result = await _appointmentService.CreateAppointmentAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật lịch hẹn sửa chữa.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead (Trưởng bộ phận kĩ thuật) 
        /// Dùng khi muốn điều chỉnh thời gian hoặc thông tin của lịch hẹn hiện có.
        /// 
        /// /// **Các trạng thái (`Status`) có thể cập nhật:**
        /// - `Pending` → Chưa được phân công.  
        /// - `Assigned` → Đã gán kỹ thuật viên.  
        /// - `Confirmed` → Kỹ thuật viên đã xác nhận.  
        /// - `InProgress` → Đang thực hiện.  
        /// - `Completed` → Đã hoàn tất.  
        /// - `Canceled` → Bị hủy.  
        /// 
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần cập nhật.</param>
        /// <param name="dto">Thông tin cần cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật lịch hẹn thành công.</response>
        /// <response code="404">Không tìm thấy lịch hẹn.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateDto dto)
        {
            var result = await _appointmentService.UpdateAppointmentAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa lịch hẹn sửa chữa.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead (Trưởng bộ phận kĩ thuật)  
        /// Dùng khi cần xóa một lịch hẹn đã được đặt.
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Xóa thành công.</response>
        /// <response code="404">Không tìm thấy lịch hẹn.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteAppointment(int id)
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một lịch hẹn.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** 👷 TechnicianLead (Trưởng bộ phận kỹ thuật), 🧑‍💼 Manager hoặc 🧑‍🔧 Technician.  
        /// 
        /// Dùng để xem chi tiết lịch hẹn, bao gồm:
        /// - Thông tin yêu cầu sửa chữa (`RepairRequest`)  
        /// - Danh sách kỹ thuật viên được gán (`AppointmentAssigns`)  
        /// 
        /// **Resident** không có quyền xem lịch hẹn chi tiết của người khác.
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần xem.</param>
        /// <returns>Thông tin chi tiết lịch hẹn.</returns>
        /// <response code="200">Trả về thông tin chi tiết lịch hẹn.</response>
        /// <response code="404">Không tìm thấy lịch hẹn.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            var result = await _appointmentService.GetAppointmentByIdAsync(id);
            return Ok(result);
        }


        /// <summary>
        /// Lấy danh sách lịch hẹn có phân trang, tìm kiếm, lọc và theo khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** 👷 TechnicianLead (Trưởng bộ phận kỹ thuật), 🧑‍💼 Manager, 🧑‍🔧 Technician  
        /// 
        /// API này cho phép phân trang, tìm kiếm theo ghi chú (`Note`), lọc theo trạng thái (`Status`), 
        /// và giới hạn trong khoảng thời gian (`fromDate`, `toDate`).  
        /// 
        /// **Tham số truy vấn:**  
        /// - `page`: Trang hiện tại (bắt đầu từ 1).  
        /// - `size`: Số bản ghi mỗi trang.  
        /// - `search`: Từ khóa tìm kiếm theo ghi chú.  
        /// - `filter`: Lọc theo trạng thái (ví dụ: pending, assigned, confirmed, completed, canceled).  
        /// - `fromDate`: Ngày bắt đầu (định dạng `yyyy-MM-dd`).  
        /// - `toDate`: Ngày kết thúc (định dạng `yyyy-MM-dd`).  
        /// - `sortBy`: Tiêu chí sắp xếp (`starttime`, `starttime_desc`, v.v.).  
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, sắp xếp và lọc.</param>
        /// <param name="fromDate">Ngày bắt đầu (tùy chọn).</param>
        /// <param name="toDate">Ngày kết thúc (tùy chọn).</param>
        /// <returns>Danh sách lịch hẹn có phân trang.</returns>
        /// <response code="200">Trả về danh sách lịch hẹn.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("paginate")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(IPaginate<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IPaginate<AppointmentDto>>> GetPaginateAppointment(
            [FromQuery] PaginateDto dto,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate)
        {
            var result = await _appointmentService.GetPaginateAppointmentAsync(dto, fromDate, toDate);
            return Ok(result);
        }



        /// <summary>
        /// Lấy lịch hẹn của cư dân trong khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** 🏠 Resident.  
        /// Trả về các lịch hẹn mà căn hộ của cư dân đó có liên quan trong khoảng thời gian chỉ định.
        /// 
        /// **Tham số bắt buộc:**
        /// - `fromDate`: ngày bắt đầu (định dạng `yyyy-MM-dd`)  
        /// - `toDate`: ngày kết thúc (định dạng `yyyy-MM-dd`)
        /// 
        /// Kết quả được nhóm theo từng ngày.
        /// </remarks>
        /// <param name="fromDate">Ngày bắt đầu.</param>
        /// <param name="toDate">Ngày kết thúc.</param>
        /// <returns>Danh sách lịch hẹn theo ngày.</returns>
        /// <response code="200">Trả về danh sách lịch hẹn.</response>
        /// <response code="400">Ngày không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpGet("resident-schedule")]
        [Authorize(Roles = $"{nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(IEnumerable<ResidentAppointmentScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ResidentAppointmentScheduleDto>>> GetResidentAppointmentSchedule(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            if (fromDate > toDate)
                return BadRequest("Ngày bắt đầu không thể sau ngày kết thúc.");

            var result = await _appointmentService.GetResidentAppointmentScheduleAsync(fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Lấy lịch hẹn sửa chữa của kỹ thuật viên trong khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** 👷 TechnicianLead (Trưởng bộ phận kỹ thuật).  
        /// Dùng để xem lịch hẹn của **một kỹ thuật viên cụ thể** trong khoảng thời gian nhất định.  
        /// 
        /// **Tham số bắt buộc:**
        /// - `fromDate`: ngày bắt đầu (`yyyy-MM-dd`)
        /// - `toDate`: ngày kết thúc (`yyyy-MM-dd`)
        /// 
        /// **Tham số tùy chọn:**
        /// - `technicianId`: ID kỹ thuật viên (nếu bỏ trống, lấy toàn bộ)
        /// 
        /// Kết quả được nhóm theo ngày và chia theo ca làm việc (slot).
        /// </remarks>
        /// <param name="technicianId">ID kỹ thuật viên (tùy chọn).</param>
        /// <param name="fromDate">Ngày bắt đầu.</param>
        /// <param name="toDate">Ngày kết thúc.</param>
        /// <returns>Danh sách lịch hẹn theo ngày và ca làm việc.</returns>
        /// <response code="200">Trả về danh sách lịch hẹn.</response>
        /// <response code="400">Ngày không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpGet("technician-schedule")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [ProducesResponseType(typeof(IEnumerable<TechnicianAppointmentScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TechnicianAppointmentScheduleDto>>> GetTechnicianAppointmentSchedule(
            [FromQuery] int? technicianId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            if (fromDate > toDate)
                return BadRequest("Ngày bắt đầu không thể sau ngày kết thúc.");

            var result = await _appointmentService.GetTechnicianAppointmentScheduleAsync(technicianId, fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Lấy lịch hẹn sửa chữa của **chính kỹ thuật viên đang đăng nhập** trong khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** 👷 Technician.  
        /// Dùng cho kỹ thuật viên xem lịch làm việc của mình theo ngày và slot.
        /// 
        /// **Tham số bắt buộc:**
        /// - `fromDate`: ngày bắt đầu (`yyyy-MM-dd`)
        /// - `toDate`: ngày kết thúc (`yyyy-MM-dd`)
        /// </remarks>
        /// <param name="fromDate">Ngày bắt đầu.</param>
        /// <param name="toDate">Ngày kết thúc.</param>
        /// <returns>Lịch hẹn của kỹ thuật viên hiện tại.</returns>
        /// <response code="200">Trả về danh sách lịch hẹn.</response>
        /// <response code="400">Ngày không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        [HttpGet("my-schedule")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(IEnumerable<TechnicianAppointmentScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TechnicianAppointmentScheduleDto>>> GetMyTechnicianAppointmentSchedule(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate)
        {
            if (fromDate > toDate)
                return BadRequest("Ngày bắt đầu không thể sau ngày kết thúc.");

            var result = await _appointmentService.GetMyTechnicianAppointmentScheduleAsync(fromDate, toDate);
            return Ok(result);
        }
    }
}

