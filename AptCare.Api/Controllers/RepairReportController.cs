using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.RepairReportDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class RepairReportController : BaseApiController
    {
        private readonly IRepairReportService _repairReportService;

        public RepairReportController(IRepairReportService repairReportService)
        {
            _repairReportService = repairReportService;
        }

        /// <summary>
        /// Tạo báo cáo sửa chữa cho một cuộc hẹn.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician
        /// 
        /// **Chức năng:**
        /// - Tạo báo cáo sau khi hoàn tất công việc sửa chữa
        /// - Tự động tạo approval pending cho TechnicianLead
        /// - Upload ảnh kết quả sửa chữa (trước/sau)
        /// 
        /// **Ràng buộc:**
        /// - Cuộc hẹn phải tồn tại và đã bắt đầu
        /// - Một cuộc hẹn chỉ có 1 báo cáo sửa chữa
        /// - `WorkDescription`: Mô tả công việc đã thực hiện
        /// - `Result`: Kết quả sau khi sửa chữa
        /// - `Recommendation`: Khuyến nghị bảo trì tiếp theo (tùy chọn)
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(RepairReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateRepairReportAsync([FromForm] CreateRepairReportDto dto)
        {
            var result = await _repairReportService.CreateRepairReportAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết báo cáo sửa chữa theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician, TechnicianLead, Manager, Admin, Resident
        /// 
        /// **Kết quả bao gồm:**
        /// - Thông tin chi tiết báo cáo
        /// - Danh sách ảnh đính kèm
        /// - Lịch sử approval (TechnicianLead, Manager)
        /// - Thông tin cuộc hẹn liên quan
        /// </remarks>
        [HttpGet("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}, {nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Admin)}, {nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(RepairReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRepairReportByIdAsync([FromRoute] int id)
        {
            var result = await _repairReportService.GetRepairReportByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy báo cáo sửa chữa theo AppointmentId.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician, TechnicianLead, Manager, Admin, Resident
        /// 
        /// **Use case:** Khi cần xem báo cáo sửa chữa của một cuộc hẹn cụ thể
        /// </remarks>
        [HttpGet("by-appointment/{appointmentId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}, {nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Admin)}, {nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(RepairReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRepairReportByAppointmentIdAsync([FromRoute] int appointmentId)
        {
            var result = await _repairReportService.GetRepairReportByAppointmentIdAsync(appointmentId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách báo cáo sửa chữa có phân trang và lọc.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician, TechnicianLead, Manager, Admin
        /// 
        /// **Tham số lọc:**
        /// - `page`: Số trang (mặc định = 1)
        /// - `size`: Số bản ghi/trang (mặc định = 10)
        /// - `search`: Tìm kiếm theo WorkDescription, Result, Room/Area
        /// - `filter`: Lọc theo status (Pending, Approved, Rejected)
        /// - `Fromdate`: Lọc từ ngày (yyyy-MM-dd)
        /// - `Todate`: Lọc đến ngày (yyyy-MM-dd)
        /// - `TechnicianId`: Lọc theo kỹ thuật viên
        /// - `ApartmentId`: Lọc theo căn hộ
        /// - `sortBy`: Sắp xếp (id, id_desc, date, date_desc)
        /// 
        /// **Kết quả:**
        /// - Danh sách báo cáo dạng rút gọn
        /// - Thông tin phân trang
        /// </remarks>
        [HttpGet("paginate")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}, {nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(IPaginate<RepairReportBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaginateRepairReportsAsync([FromQuery] RepairReportFilterDto filterDto)
        {
            var result = await _repairReportService.GetPaginateRepairReportsAsync(filterDto);
            return Ok(result);
        }

        ///// <summary>
        ///// Cập nhật báo cáo sửa chữa.
        ///// </summary>
        ///// <remarks>
        ///// **Chỉ role:** Technician (chính chủ báo cáo)
        ///// 
        ///// **Ràng buộc:**
        ///// - Chỉ cập nhật được nếu status là Pending hoặc Rejected
        ///// - Không thể cập nhật báo cáo đã Approved
        ///// 
        ///// **Các trường có thể cập nhật:**
        ///// - `WorkDescription`: Mô tả công việc
        ///// - `Result`: Kết quả
        ///// - `Recommendation`: Khuyến nghị
        ///// - `Note`: Ghi chú
        ///// </remarks>
        //[HttpPut("{id}")]
        //[Authorize(Roles = nameof(AccountRole.Technician))]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> UpdateRepairReportAsync([FromRoute] int id, [FromBody] UpdateRepairReportDto dto)
        //{
        //    var result = await _repairReportService.UpdateRepairReportAsync(id, dto);
        //    return Ok(result);
        //}
    }
}