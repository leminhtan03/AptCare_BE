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
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// <b>Yêu cầu:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa, phải tồn tại.</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu lịch hẹn (yyyy-MM-dd HH:mm).</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc lịch hẹn (yyyy-MM-dd HH:mm).</li>
        ///   <li><b>Note</b>: Ghi chú cho lịch hẹn (tùy chọn).</li>
        /// </ul>
        /// <b>Resident và Technician</b> không được phép gọi API này.
        /// </remarks>
        /// <param name="dto">
        /// <b>AppointmentCreateDto:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu lịch hẹn.</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc lịch hẹn.</li>
        ///   <li><b>Note</b>: Ghi chú (tùy chọn).</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo lịch hẹn thành công.</returns>
        /// <response code="201">Tạo lịch hẹn thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Technician)}")]
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
        /// Tạo lịch hẹn mới và tự động phân công kỹ thuật viên từ lịch hẹn cũ.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// <b>Yêu cầu:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa, phải đã có lịch hẹn trước đó.</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu lịch hẹn (yyyy-MM-dd HH:mm).</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc lịch hẹn (yyyy-MM-dd HH:mm).</li>
        ///   <li><b>Note</b>: Ghi chú cho lịch hẹn (tùy chọn).</li>
        /// </ul>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Tự động lấy danh sách kỹ thuật viên từ lịch hẹn đầu tiên của yêu cầu sửa chữa.</li>
        ///   <li>Kiểm tra xung đột lịch của từng kỹ thuật viên.</li>
        ///   <li>Nếu có kỹ thuật viên bị trùng lịch, sẽ throw exception chi tiết về tên, thời gian xung đột.</li>
        ///   <li>Tạo lịch hẹn mới và tự động phân công kỹ thuật viên (status = Assigned).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>AppointmentCreateDto:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết (phải đã có lịch hẹn trước đó).</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu lịch hẹn.</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc lịch hẹn.</li>
        ///   <li><b>Note</b>: Ghi chú (tùy chọn).</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo lịch hẹn và phân công thành công.</returns>
        /// <response code="201">Tạo lịch hẹn và phân công kỹ thuật viên thành công.</response>
        /// <response code="400">
        /// <ul>
        ///   <li>Dữ liệu không hợp lệ (thời gian sai, không có lịch hẹn cũ).</li>
        ///   <li>Kỹ thuật viên bị trùng lịch (kèm thông tin chi tiết về xung đột).</li>
        /// </ul>
        /// </response>
        /// <response code="404">
        /// <ul>
        ///   <li>Không tìm thấy yêu cầu sửa chữa.</li>
        ///   <li>Không tìm thấy lịch hẹn cũ hoặc lịch hẹn cũ chưa được phân công kỹ thuật viên.</li>
        /// </ul>
        /// </response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("with-old-technician")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateAppointmentWithOldTechnician([FromBody] AppointmentCreateDto dto)
        {
            var result = await _appointmentService.CreateAppointmentWithOldTechnicianAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật lịch hẹn sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// <b>Các trạng thái có thể cập nhật:</b>
        /// <ul>
        ///   <li><b>Pending</b>: Chưa được phân công.</li>
        ///   <li><b>Assigned</b>: Đã gán kỹ thuật viên.</li>
        ///   <li><b>Confirmed</b>: Kỹ thuật viên đã xác nhận.</li>
        ///   <li><b>InProgress</b>: Đang thực hiện.</li>
        ///   <li><b>Completed</b>: Đã hoàn tất.</li>
        ///   <li><b>Canceled</b>: Bị hủy.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần cập nhật.</param>
        /// <param name="dto">
        /// <b>AppointmentUpdateDto:</b>
        /// <ul>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu mới.</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc mới.</li>
        ///   <li><b>Note</b>: Ghi chú cập nhật (tùy chọn).</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật lịch hẹn thành công.</response>
        /// <response code="404">Không tìm thấy lịch hẹn.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Technician)}")]
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
        //[HttpDelete("{id:int}")]
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult> DeleteAppointment(int id)
        //{
        //    var result = await _appointmentService.DeleteAppointmentAsync(id);
        //    return Ok(result);
        //}

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
        /// <returns>
        /// <b>AppointmentDto:</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn.</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu.</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc.</li>
        ///   <li><b>Note</b>: Ghi chú.</li>
        ///   <li><b>Status</b>: Trạng thái lịch hẹn.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo.</li>
        ///   <li><b>RepairRequest</b>: Thông tin yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>Technicians</b>: Danh sách kỹ thuật viên được gán.</li>
        ///   <li><b>AppointmentTrackings</b>: Lịch sử trạng thái lịch hẹn.</li>
        /// </ul>
        /// </returns>
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
        /// <b>Chỉ role:</b> TechnicianLead, Manager, Technician<br/>
        /// <b>Tham số truy vấn:</b>
        /// <ul>
        ///   <li><b>dto.page</b>: Trang hiện tại (bắt đầu từ 1).</li>
        ///   <li><b>dto.size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>dto.sortBy</b>: Tiêu chí sắp xếp (starttime, starttime_desc,...).</li>
        ///   <li><b>dto.search</b>: Từ khóa tìm kiếm theo ghi chú.</li>
        ///   <li><b>dto.filter</b>: Lọc theo trạng thái.</li>
        ///   <li><b>fromDate</b>: Ngày bắt đầu (yyyy-MM-dd).</li>
        ///   <li><b>toDate</b>: Ngày kết thúc (yyyy-MM-dd).</li>
        ///   <li><b>isAprroved</b>: Lọc theo trạng thái chấp nhận của yêu cầu.</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>PaginateDto:</b>
        /// <ul>
        ///   <li><b>page</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái.</li>
        /// </ul>
        /// </param>
        /// <param name="fromDate">Ngày bắt đầu (tùy chọn).</param>
        /// <param name="toDate">Ngày kết thúc (tùy chọn).</param>
        /// <param name="isAprroved">Đã chấp nhận chưa (tùy chọn).</param>
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
            [FromQuery] DateOnly? toDate,
            [FromQuery] bool? isAprroved)
        {
            var result = await _appointmentService.GetPaginateAppointmentAsync(dto, fromDate, toDate, isAprroved);
            return Ok(result);
        }



        /// <summary>
        /// Lấy lịch hẹn của cư dân trong khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Resident<br/>
        /// Trả về các lịch hẹn mà căn hộ của cư dân đó có liên quan trong khoảng thời gian chỉ định.<br/>
        /// <b>Kết quả:</b> Nhóm theo từng ngày.
        /// </remarks>
        /// <param name="fromDate">Ngày bắt đầu (định dạng yyyy-MM-dd).</param>
        /// <param name="toDate">Ngày kết thúc (định dạng yyyy-MM-dd).</param>
        /// <returns>
        /// <b>ResidentAppointmentScheduleDto[]:</b>
        /// <ul>
        ///   <li><b>Date</b>: Ngày của lịch hẹn.</li>
        ///   <li><b>Appointments</b>: Danh sách lịch hẹn trong ngày đó (xem <b>AppointmentDto</b>).</li>
        /// </ul>
        /// <b>AppointmentDto:</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn.</li>
        ///   <li><b>StartTime</b>: Thời gian bắt đầu.</li>
        ///   <li><b>EndTime</b>: Thời gian kết thúc.</li>
        ///   <li><b>Note</b>: Ghi chú.</li>
        ///   <li><b>Status</b>: Trạng thái lịch hẹn.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo.</li>
        ///   <li><b>RepairRequest</b>: Thông tin yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>Technicians</b>: Danh sách kỹ thuật viên được gán.</li>
        ///   <li><b>AppointmentTrackings</b>: Lịch sử trạng thái lịch hẹn.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách lịch hẹn theo ngày.</response>
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
        /// <b>Chỉ role:</b> TechnicianLead, Manager<br/>
        /// Dùng để xem lịch hẹn của một kỹ thuật viên cụ thể hoặc toàn bộ trong khoảng thời gian nhất định.<br/>
        /// <b>Kết quả:</b> Nhóm theo ngày và chia theo ca làm việc (slot).
        /// </remarks>
        /// <param name="technicianId">ID kỹ thuật viên (tùy chọn).</param>
        /// <param name="fromDate">Ngày bắt đầu (định dạng yyyy-MM-dd).</param>
        /// <param name="toDate">Ngày kết thúc (định dạng yyyy-MM-dd).</param>
        /// <returns>
        /// <b>TechnicianAppointmentScheduleDto[]:</b>
        /// <ul>
        ///   <li><b>Date</b>: Ngày của lịch hẹn.</li>
        ///   <li><b>Slots</b>: Danh sách ca làm việc trong ngày đó.</li>
        /// </ul>
        /// <b>SlotAppointmentDto:</b>
        /// <ul>
        ///   <li><b>SlotId</b>: ID ca làm việc.</li>
        ///   <li><b>Appointments</b>: Danh sách lịch hẹn trong ca (xem <b>AppointmentDto</b>).</li>
        /// </ul>
        /// </returns>
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
        /// Lấy lịch hẹn sửa chữa của chính kỹ thuật viên đang đăng nhập trong khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician<br/>
        /// Dùng cho kỹ thuật viên xem lịch làm việc của mình theo ngày và slot.
        /// </remarks>
        /// <param name="fromDate">Ngày bắt đầu (định dạng yyyy-MM-dd).</param>
        /// <param name="toDate">Ngày kết thúc (định dạng yyyy-MM-dd).</param>
        /// <returns>
        /// <b>TechnicianAppointmentScheduleDto[]:</b>
        /// <ul>
        ///   <li><b>Date</b>: Ngày của lịch hẹn.</li>
        ///   <li><b>Slots</b>: Danh sách ca làm việc trong ngày đó.</li>
        /// </ul>
        /// <b>SlotAppointmentDto:</b>
        /// <ul>
        ///   <li><b>SlotId</b>: ID ca làm việc.</li>
        ///   <li><b>Appointments</b>: Danh sách lịch hẹn trong ca (xem <b>AppointmentDto</b>).</li>
        /// </ul>
        /// </returns>
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

        /// <summary>
        /// Kỹ thuật viên check-in để bắt đầu buổi hẹn.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician<br/>
        /// Gọi khi kỹ thuật viên đến địa điểm và bắt đầu buổi hẹn.
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần check-in.</param>
        /// <returns>Thông báo check-in thành công.</returns>
        [HttpPost("{id}/check-in")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckIn(int id)
        {
            var result = await _appointmentService.CheckInAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Bắt đầu thi công sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician<br/>
        /// Gọi khi kỹ thuật viên bắt đầu thực hiện công việc sửa chữa.
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần bắt đầu sửa chữa.</param>
        /// <returns>Thông báo bắt đầu sửa chữa thành công.</returns>
        [HttpPost("{id}/start-repair")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartRepair(int id)
        {
            var result = await _appointmentService.StartRepairAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Hoàn thành lịch hẹn.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// Gọi khi kỹ thuật viên hoàn tất công việc tại lịch hẹn.<br/>
        /// <ul>
        ///   <li><b>note</b>: Ghi chú hoàn thành (tùy chọn).</li>
        ///   <li><b>hasNextAppointment</b>: Nếu true sẽ tạo tracking chuyển Request sang trạng thái Scheduling.</li>
        ///   <li><b>acceptanceTime</b>: Thời gian nghiệm thu (tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID lịch hẹn cần hoàn thành.</param>
        /// <param name="note">Ghi chú hoàn thành (tùy chọn).</param>
        /// <param name="hasNextAppointment">Có lịch hẹn tiếp theo không.</param>
        /// <param name="acceptanceTime">Thời gian nghiệm thu (tùy chọn).</param>
        /// <returns>Thông báo hoàn thành thành công.</returns>
        /// <response code="200">Hoàn thành thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc chuyển trạng thái không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy lịch hẹn.</response>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CompleteAppointment(int id, string note, bool hasNextAppointment, DateOnly? acceptanceTime)
        {
            var result = await _appointmentService.CompleteAppointmentAsync(id, note, hasNextAppointment, acceptanceTime);
            return Ok(result);
        }
    }
}

