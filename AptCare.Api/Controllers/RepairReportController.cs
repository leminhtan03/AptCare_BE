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
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <list type="bullet">
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> tạo báo cáo sau khi hoàn tất công việc sửa chữa.</item>
        /// </list>
        /// <br/>
        /// <b>Chức năng:</b>
        /// <list type="bullet">
        ///   <item>Tạo báo cáo sửa chữa với mô tả công việc đã thực hiện.</item>
        ///   <item>Tự động tạo approval pending cho TechnicianLead để phê duyệt.</item>
        ///   <item>Upload ảnh kết quả sửa chữa (trước/sau) để minh chứng.</item>
        /// </list>
        /// <br/>
        /// <b>Ràng buộc:</b>
        /// <list type="bullet">
        ///   <item>Cuộc hẹn phải tồn tại và đã bắt đầu (không còn ở trạng thái Pending hoặc Assigned).</item>
        ///   <item>Một cuộc hẹn chỉ có thể có 1 báo cáo sửa chữa duy nhất.</item>
        ///   <item><c>WorkDescription</c>: Mô tả chi tiết công việc đã thực hiện (bắt buộc).</item>
        ///   <item><c>Note</c>: Ghi chú bổ sung (tùy chọn).</item>
        ///   <item><c>Files</c>: Danh sách file ảnh đính kèm (tùy chọn).</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>AppointmentId</c>: ID của cuộc hẹn cần tạo báo cáo (bắt buộc).</item>
        ///   <item><c>WorkDescription</c>: Mô tả công việc đã thực hiện (bắt buộc).</item>
        ///   <item><c>Note</c>: Ghi chú bổ sung (tùy chọn).</item>
        ///   <item><c>Files</c>: Danh sách file ảnh minh chứng (tùy chọn).</item>
        /// </list>
        /// </remarks>
        /// <param name="dto">Thông tin báo cáo sửa chữa bao gồm: AppointmentId, WorkDescription, Note, Files.</param>
        /// <returns>Thông tin báo cáo sửa chữa đã được tạo.</returns>
        /// <response code="200">Tạo báo cáo sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc cuộc hẹn đã có báo cáo.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền thực hiện thao tác này.</response>
        /// <response code="404">Không tìm thấy cuộc hẹn.</response>
        /// <response code="500">Lỗi hệ thống.</response>
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
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <list type="bullet">
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> xem báo cáo do mình tạo.</item>
        ///   <item>🧑‍💼 <b>TechnicianLead / Manager:</b> xem và phê duyệt báo cáo.</item>
        ///   <item>👨‍💻 <b>Admin:</b> xem toàn bộ báo cáo trong hệ thống.</item>
        ///   <item>🏠 <b>Resident (Cư dân):</b> xem báo cáo liên quan đến yêu cầu của mình.</item>
        /// </list>
        /// <br/>
        /// <b>Kết quả bao gồm:</b>
        /// <list type="bullet">
        ///   <item>Thông tin chi tiết báo cáo (ID, mô tả công việc, ghi chú, trạng thái).</item>
        ///   <item>Danh sách ảnh đính kèm (nếu có).</item>
        ///   <item>Lịch sử phê duyệt từ TechnicianLead và Manager.</item>
        ///   <item>Thông tin cuộc hẹn liên quan.</item>
        ///   <item>Thông tin kỹ thuật viên thực hiện.</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>id</c>: ID của báo cáo sửa chữa cần lấy thông tin.</item>
        /// </list>
        /// </remarks>
        /// <param name="id">ID của báo cáo sửa chữa.</param>
        /// <returns>Thông tin chi tiết báo cáo sửa chữa.</returns>
        /// <response code="200">Lấy thông tin báo cáo sửa chữa thành công.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy báo cáo sửa chữa.</response>
        /// <response code="500">Lỗi hệ thống.</response>
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
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <list type="bullet">
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> xem báo cáo của cuộc hẹn được giao.</item>
        ///   <item>🧑‍💼 <b>TechnicianLead / Manager:</b> xem và quản lý báo cáo.</item>
        ///   <item>👨‍💻 <b>Admin:</b> xem toàn bộ báo cáo.</item>
        ///   <item>🏠 <b>Resident (Cư dân):</b> xem báo cáo của cuộc hẹn liên quan.</item>
        /// </list>
        /// <br/>
        /// <b>Use case:</b>
        /// <list type="bullet">
        ///   <item>Khi cần xem báo cáo sửa chữa của một cuộc hẹn cụ thể.</item>
        ///   <item>Kiểm tra xem cuộc hẹn đã có báo cáo hay chưa.</item>
        ///   <item>Theo dõi tiến độ và kết quả sửa chữa.</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>appointmentId</c>: ID của cuộc hẹn cần lấy báo cáo.</item>
        /// </list>
        /// </remarks>
        /// <param name="appointmentId">ID của cuộc hẹn.</param>
        /// <returns>Thông tin báo cáo sửa chữa của cuộc hẹn.</returns>
        /// <response code="200">Lấy báo cáo sửa chữa thành công.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy báo cáo sửa chữa cho cuộc hẹn này.</response>
        /// <response code="500">Lỗi hệ thống.</response>
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
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <list type="bullet">
        ///   <item>🔧 <b>Technician (Kỹ thuật viên):</b> xem danh sách báo cáo do mình tạo.</item>
        ///   <item>🧑‍💼 <b>TechnicianLead / Manager:</b> xem và quản lý toàn bộ báo cáo.</item>
        ///   <item>👨‍💻 <b>Admin:</b> xem toàn bộ báo cáo trong hệ thống.</item>
        /// </list>
        /// <br/>
        /// <b>Tham số lọc &amp; tìm kiếm:</b>
        /// <list type="bullet">
        ///   <item><c>page</c>: Số trang (mặc định = 1).</item>
        ///   <item><c>size</c>: Số bản ghi trên mỗi trang (mặc định = 10).</item>
        ///   <item><c>search</c>: Tìm kiếm theo mô tả công việc (Description), tên phòng/khu vực.</item>
        ///   <item><c>filter</c>: Lọc theo trạng thái:
        ///     <list type="bullet">
        ///     <item><description>Pending (1) - Chờ phê duyệt</description></item>
        ///     <item><description>Approved (2) - Đã phê duyệt</description></item>
        ///     <item><description>Rejected (3) - Bị từ chối</description></item>
        ///     </list>
        ///   </item>
        ///   <item><c>Fromdate</c>: Lọc từ ngày (định dạng: yyyy-MM-dd).</item>
        ///   <item><c>Todate</c>: Lọc đến ngày (định dạng: yyyy-MM-dd).</item>
        ///   <item><c>TechnicianId</c>: Lọc theo kỹ thuật viên thực hiện.</item>
        ///   <item><c>ApartmentId</c>: Lọc theo căn hộ.</item>
        ///   <item><c>sortBy</c>: Sắp xếp theo trường (id, id_desc, date, date_desc).</item>
        /// </list>
        /// <br/>
        /// <b>Kết quả:</b>
        /// <list type="bullet">
        ///   <item>Danh sách báo cáo dạng rút gọn (ID, kỹ thuật viên, mô tả công việc, trạng thái, ngày tạo).</item>
        ///   <item>Thông tin phân trang (tổng số, số trang, trang hiện tại).</item>
        ///   <item>Danh sách ảnh đính kèm cho mỗi báo cáo (nếu có).</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>filterDto</c>: DTO chứa các tham số lọc và phân trang.</item>
        /// </list>
        /// </remarks>
        /// <param name="filterDto">Thông tin lọc và phân trang.</param>
        /// <returns>Danh sách báo cáo sửa chữa có phân trang.</returns>
        /// <response code="200">Lấy danh sách báo cáo sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="500">Lỗi hệ thống.</response>
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
        ///// <b>Phân quyền &amp; hành vi theo role:</b>
        ///// <list type="bullet">
        /////   <item>🔧 <b>Technician (Kỹ thuật viên):</b> chỉ được cập nhật báo cáo do mình tạo và chưa được phê duyệt.</item>
        ///// </list>
        ///// <br/>
        ///// <b>Ràng buộc:</b>
        ///// <list type="bullet">
        /////   <item>Chỉ cập nhật được nếu báo cáo ở trạng thái <b>Pending</b> hoặc <b>Rejected</b>.</item>
        /////   <item>Không thể cập nhật báo cáo đã được phê duyệt (<b>Approved</b>).</item>
        /////   <item>Phải là kỹ thuật viên tạo báo cáo đó.</item>
        ///// </list>
        ///// <br/>
        ///// <b>Các trường có thể cập nhật:</b>
        ///// <list type="bullet">
        /////   <item><c>WorkDescription</c>: Mô tả công việc đã thực hiện.</item>
        /////   <item><c>Result</c>: Kết quả sau khi sửa chữa.</item>
        /////   <item><c>Recommendation</c>: Khuyến nghị bảo trì tiếp theo.</item>
        /////   <item><c>Note</c>: Ghi chú bổ sung.</item>
        ///// </list>
        ///// <br/>
        ///// <b>Tham số:</b>
        ///// <list type="bullet">
        /////   <item><c>id</c>: ID của báo cáo sửa chữa cần cập nhật.</item>
        /////   <item><c>dto</c>: Thông tin cập nhật (các trường đều optional).</item>
        ///// </list>
        ///// </remarks>
        ///// <param name="id">ID của báo cáo sửa chữa.</param>
        ///// <param name="dto">Thông tin cập nhật báo cáo.</param>
        ///// <returns>Thông báo cập nhật thành công.</returns>
        ///// <response code="200">Cập nhật báo cáo sửa chữa thành công.</response>
        ///// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc báo cáo đã được phê duyệt.</response>
        ///// <response code="401">Không có quyền truy cập.</response>
        ///// <response code="403">Không đủ quyền thực hiện thao tác này.</response>
        ///// <response code="404">Không tìm thấy báo cáo sửa chữa.</response>
        ///// <response code="500">Lỗi hệ thống.</response>
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