using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AptCare.Api.Controllers
{
    public class InspectionReportController : BaseApiController
    {
        private readonly IInspectionReporService _inspectionReporService;
        public InspectionReportController(IInspectionReporService _inspectionReporService)
        {
            this._inspectionReporService = _inspectionReporService;
        }
        /// <summary>
        /// Tạo báo cáo kiểm tra cho một lịch hẹn.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Tạo báo cáo kiểm tra dựa trên thông tin lịch hẹn.</li>
        ///   <li>Xác định loại lỗi (lỗi tòa nhà hoặc lỗi cư dân).</li>
        ///   <li>Ghi nhận giải pháp xử lý (sửa chữa, thay thế, thuê ngoài).</li>
        ///   <li>Lưu mô tả chi tiết và giải pháp đề xuất.</li>
        /// </ul>
        /// <b>Ràng buộc:</b>
        /// <ul>
        ///   <li><b>FaultOwner</b>: Chỉ chấp nhận giá trị enum <c>FaultType</c>:
        ///     <ul>
        ///       <li>1 = BuildingFault (Lỗi tòa nhà)</li>
        ///       <li>2 = ResidentFault (Lỗi cư dân)</li>
        ///     </ul>
        ///   </li>
        ///   <li><b>SolutionType</b>: Chỉ chấp nhận giá trị enum <c>SolutionType</c>:
        ///     <ul>
        ///       <li>1 = Repair (Sửa chữa)</li>
        ///       <li>2 = Replacement (Thay thế)</li>
        ///       <li>3 = Outsource (Thuê ngoài)</li>
        ///     </ul>
        ///   </li>
        /// </ul>
        /// <b>Tham số (<c>CreateInspectionReporDto</c>):</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết (bắt buộc).</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi (enum, bắt buộc).</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp (enum, bắt buộc).</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Files</b>: Danh sách file đính kèm (hình ảnh/video, tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>CreateInspectionReporDto:</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi (enum).</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp (enum).</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Files</b>: Danh sách file đính kèm.</li>
        /// </ul>
        /// </param>
        /// <returns>
        /// <b>InspectionReportDto:</b>
        /// <ul>
        ///   <li><b>InspectionReportId</b>: ID báo cáo kiểm tra.</li>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>UserId</b>: ID kỹ thuật viên tạo báo cáo.</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp.</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Status</b>: Trạng thái báo cáo.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo báo cáo.</li>
        ///   <li><b>AreaName</b>: Tên khu vực liên quan.</li>
        ///   <li><b>Technican</b>: Thông tin kỹ thuật viên.</li>
        ///   <li><b>Medias</b>: Danh sách media đính kèm.</li>
        ///   <li><b>ReportApprovals</b>: Danh sách phê duyệt báo cáo.</li>
        ///   <li><b>Appointment</b>: Thông tin lịch hẹn liên kết.</li>
        /// </ul>
        /// </returns>
        [HttpPost("inspection-report")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        public async Task<IActionResult> GenerateInspectionReportAsync([FromForm] CreateInspectionReporDto dto)
        {
            var result = await _inspectionReporService.CreateInspectionReportAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Tạo báo cáo kiểm tra bảo trì cho một lịch hẹn với checklist nhiệm vụ.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Tạo báo cáo kiểm tra cho yêu cầu bảo trì định kỳ.</li>
        ///   <li>Cập nhật trạng thái hoàn thành cho tất cả nhiệm vụ trong checklist.</li>
        ///   <li>Ghi nhận giải pháp xử lý (sửa chữa, thay thế, thuê ngoài).</li>
        ///   <li>Lưu mô tả chi tiết và kết quả kiểm tra cho từng nhiệm vụ.</li>
        ///   <li>Tự động cập nhật trạng thái cuộc hẹn sang "Chờ duyệt báo cáo".</li>
        /// </ul>
        /// <b>Ràng buộc:</b>
        /// <ul>
        ///   <li>Tất cả nhiệm vụ của yêu cầu sửa chữa phải được cập nhật.</li>
        ///   <li>Mỗi nhiệm vụ phải có <c>InspectionResult</c> (bắt buộc).</li>
        ///   <li><b>SolutionType</b>: Chỉ chấp nhận giá trị enum <c>SolutionType</c>:
        ///     <ul>
        ///       <li>1 = Repair (Sửa chữa)</li>
        ///       <li>2 = Replacement (Thay thế)</li>
        ///       <li>3 = Outsource (Thuê ngoài)</li>
        ///     </ul>
        ///   </li>
        /// </ul>
        /// <b>Tham số (<c>InspectionMaintenanceReporCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết (bắt buộc).</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp (enum, bắt buộc).</li>
        ///   <li><b>Description</b>: Mô tả chi tiết kết quả kiểm tra.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Files</b>: Danh sách file đính kèm (hình ảnh/video, tùy chọn).</li>
        ///  
        /// </ul>
        /// <b>Validation:</b>
        /// <ul>
        ///   <li>Kiểm tra tất cả nhiệm vụ của repair request đã được cập nhật.</li>
        ///   <li>Kiểm tra không có nhiệm vụ không thuộc repair request này.</li>
        ///   <li>Nếu thiếu nhiệm vụ, trả về lỗi với danh sách tên nhiệm vụ còn thiếu.</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>InspectionMaintenanceReporCreateDto:</b>
        /// <ul>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp (enum).</li>
        ///   <li><b>Description</b>: Mô tả chi tiết kết quả.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Files</b>: Danh sách file đính kèm.</li>
        /// </ul>
        /// </param>
        /// <returns>
        /// <b>InspectionReportDto:</b>
        /// <ul>
        ///   <li><b>InspectionReportId</b>: ID báo cáo kiểm tra.</li>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>UserId</b>: ID kỹ thuật viên tạo báo cáo.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp.</li>
        ///   <li><b>Description</b>: Mô tả chi tiết kết quả.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Status</b>: Trạng thái báo cáo (Pending).</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo báo cáo.</li>
        ///   <li><b>Technican</b>: Thông tin kỹ thuật viên.</li>
        ///   <li><b>Medias</b>: Danh sách media đính kèm.</li>
        ///   <li><b>ReportApprovals</b>: Danh sách phê duyệt báo cáo.</li>
        ///   <li><b>Appointment</b>: Thông tin lịch hẹn liên kết.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Tạo báo cáo kiểm tra bảo trì thành công.</response>
        /// <response code="400">
        /// Dữ liệu không hợp lệ:
        /// <ul>
        ///   <li>Chưa có công việc nào được cập nhật.</li>
        ///   <li>Chưa cập nhật đủ tất cả nhiệm vụ (hiển thị danh sách nhiệm vụ thiếu).</li>
        ///   <li>Có nhiệm vụ không thuộc yêu cầu sửa chữa này.</li>
        ///   <li>Có nhiệm vụ chưa hoàn thành (hiển thị danh sách nhiệm vụ chưa hoàn thành).</li>
        ///   <li>Báo cáo đang chờ phê duyệt.</li>
        /// </ul>
        /// </response>
        /// <response code="404">Không tìm thấy cuộc hẹn hoặc yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập (chỉ Technician).</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("inspection-maintenance-report")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        public async Task<IActionResult> GenerateInspectionMaintenanceReportAsync([FromForm] InspectionMaintenanceReporCreateDto dto)
        {
            var result = await _inspectionReporService.CreateInspectionMaintenanceReportAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của báo cáo kiểm tra theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Truy xuất thông tin chi tiết của một báo cáo kiểm tra cụ thể.</li>
        ///   <li>Hiển thị thông tin bao gồm: loại lỗi, giải pháp xử lý, mô tả, trạng thái báo cáo.</li>
        ///   <li>Bao gồm thông tin lịch hẹn liên quan và kỹ thuật viên thực hiện.</li>
        /// </ul>
        /// <b>Kết quả (<c>InspectionReportDetailDto</c>):</b>
        /// <ul>
        ///   <li><b>InspectionReportId</b>: ID báo cáo kiểm tra.</li>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên quan.</li>
        ///   <li><b>UserId</b>: ID kỹ thuật viên tạo báo cáo.</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp.</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Status</b>: Trạng thái báo cáo.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo báo cáo.</li>
        ///   <li><b>AreaName</b>: Tên khu vực liên quan.</li>
        ///   <li><b>Technican</b>: Thông tin kỹ thuật viên.</li>
        ///   <li><b>Medias</b>: Danh sách media đính kèm.</li>
        ///   <li><b>ReportApprovals</b>: Danh sách phê duyệt báo cáo.</li>
        ///   <li><b>Appointment</b>: Thông tin lịch hẹn liên kết.</li>
        ///   <li><b>Invoice</b>: Thông tin hóa đơn liên quan (nếu có).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của báo cáo kiểm tra cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của báo cáo kiểm tra.</returns>
        [HttpGet("inspection-report/{id}")]
        [ProducesResponseType(typeof(InspectionReportDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = nameof(AccountRole.Technician) + "," + nameof(AccountRole.Manager) + "," + nameof(AccountRole.Admin) + "," + nameof(AccountRole.Resident) + "," + nameof(AccountRole.TechnicianLead))]
        public async Task<IActionResult> GetInspectionReportByIdAsync([FromRoute] int id)
        {
            var result = await _inspectionReporService.GetInspectionReportByIdAsync(id);
            return Ok(result);
        }
        /// <summary>
        /// Lấy danh sách báo cáo kiểm tra có phân trang và tìm kiếm.
        /// </summary>
        /// <remarks>
        /// <b>Phân quyền &amp; hành vi theo role:</b>
        /// <ul>
        ///   <li>🔧 <b>Technician (Kỹ thuật viên):</b> xem danh sách báo cáo do mình tạo.</li>
        ///   <li>🧑‍💼 <b>TechnicianLead / Manager:</b> xem và quản lý toàn bộ báo cáo.</li>
        ///   <li>👨‍💻 <b>Admin:</b> xem toàn bộ báo cáo trong hệ thống.</li>
        /// </ul>
        /// <b>Tham số lọc &amp; tìm kiếm (<c>InspectionReportFilterDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang (mặc định = 1).</li>
        ///   <li><b>size</b>: Số bản ghi trên mỗi trang (mặc định = 10).</li>
        ///   <li><b>sortBy</b>: Sắp xếp theo trường (id, id_desc, date, date_desc).</li>
        ///   <li><b>search</b>: Tìm kiếm theo Description, Solution, AreaName.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái (Pending, Approved, Rejected).</li>
        ///   <li><b>FaultType</b>: Lọc theo loại lỗi (BuildingFault, ResidentFault).</li>
        ///   <li><b>SolutionType</b>: Lọc theo giải pháp (Repair, Replacement, Outsource).</li>
        ///   <li><b>Fromdate</b>: Lọc từ ngày (yyyy-MM-dd).</li>
        ///   <li><b>Todate</b>: Lọc đến ngày (yyyy-MM-dd).</li>
        /// </ul>
        /// </remarks>
        /// <param name="filterDto">
        /// <b>InspectionReportFilterDto:</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại.</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái.</li>
        ///   <li><b>FaultType</b>: Lọc theo loại lỗi.</li>
        ///   <li><b>SolutionType</b>: Lọc theo giải pháp.</li>
        ///   <li><b>Fromdate</b>: Lọc từ ngày.</li>
        ///   <li><b>Todate</b>: Lọc đến ngày.</li>
        /// </ul>
        /// </param>
        /// <returns>
        /// <b>IPaginate&lt;InspectionReportDetailDto&gt;:</b>
        /// <ul>
        ///   <li><b>InspectionReportId</b>: ID báo cáo kiểm tra.</li>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>UserId</b>: ID kỹ thuật viên tạo báo cáo.</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp.</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Status</b>: Trạng thái báo cáo.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo báo cáo.</li>
        ///   <li><b>AreaName</b>: Tên khu vực liên quan.</li>
        ///   <li><b>Technican</b>: Thông tin kỹ thuật viên.</li>
        ///   <li><b>Medias</b>: Danh sách media đính kèm.</li>
        ///   <li><b>ReportApprovals</b>: Danh sách phê duyệt báo cáo.</li>
        ///   <li><b>Appointment</b>: Thông tin lịch hẹn liên kết.</li>
        ///   <li><b>Invoice</b>: Thông tin hóa đơn liên quan (nếu có).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Lấy danh sách báo cáo kiểm tra thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("inspection-report/paginate")]
        [ProducesResponseType(typeof(IPaginate<InspectionReportDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = nameof(AccountRole.Technician) + "," + nameof(AccountRole.Manager) + "," + nameof(AccountRole.Admin) + "," + nameof(AccountRole.TechnicianLead))]
        public async Task<IActionResult> GetPaginateInspectionReportsAsync([FromQuery] InspectionReportFilterDto filterDto)
        {
            var result = await _inspectionReporService.GetPaginateInspectionReportsAsync(filterDto);
            return Ok(result);
        }
        ///// <summary>
        ///// Cập nhật thông tin báo cáo kiểm tra.
        ///// </summary>
        ///// <remarks>
        ///// **Chức năng:**  
        ///// - Cập nhật thông tin chi tiết của báo cáo kiểm tra đã tồn tại.  
        ///// - Cho phép thay đổi loại lỗi (lỗi tòa nhà hoặc lỗi cư dân).  
        ///// - Cập nhật giải pháp xử lý (sửa chữa, thay thế, thuê ngoài).  
        ///// - Chỉnh sửa mô tả chi tiết và giải pháp đề xuất.
        ///// 
        ///// **Ràng buộc:**  
        ///// - `FaultOwner`: Chỉ chấp nhận giá trị enum `FaultType`:
        /////   - `1` = BuildingFault (Lỗi tòa nhà)
        /////   - `2` = ResidentFault (Lỗi cư dân)
        ///// - `SolutionType`: Chỉ chấp nhận giá trị enum `SolutionType`:
        /////   - `1` = Repair (Sửa chữa)
        /////   - `2` = Replacement (Thay thế)
        /////   - `3` = Outsource (Thuê ngoài)
        ///// - `Description`: Bắt buộc, mô tả chi tiết về lỗi
        ///// - `Solution`: Bắt buộc, giải pháp đề xuất
        ///// 
        ///// **Lưu ý:**  
        ///// - Chỉ có thể cập nhật báo cáo đang ở trạng thái cho phép chỉnh sửa
        ///// - ID báo cáo phải tồn tại trong hệ thống
        ///// </remarks>
        ///// <param name="id">ID của báo cáo kiểm tra cần cập nhật</param>
        ///// <param name="dto">Thông tin cập nhật bao gồm FaultOwner (enum), SolutionType (enum), Description và Solution</param>
        ///// <returns>Thông báo xác nhận cập nhật báo cáo thành công</returns>
        //[HttpPut("update-inspection-report/{id}")]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[Authorize(Roles = nameof(AccountRole.Technician))]
        //public async Task<IActionResult> UpdateInspectionReportAsync([FromRoute] int id, [FromBody] UpdateInspectionReporDto dto)
        //{
        //    var result = await _inspectionReporService.UpdateInspectionReportAsync(id, dto);
        //    return Ok(result);
        //}

        /// <summary>
        /// Lấy thông tin báo cáo kiểm tra theo ID lịch hẹn.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Truy xuất thông tin báo cáo kiểm tra dựa trên ID lịch hẹn.</li>
        ///   <li>Hiển thị thông tin cơ bản của báo cáo bao gồm: loại lỗi, giải pháp xử lý, trạng thái.</li>
        ///   <li>Bao gồm thông tin kỹ thuật viên thực hiện và khu vực liên quan.</li>
        ///   <li>Hiển thị danh sách media (hình ảnh/video) đính kèm nếu có.</li>
        /// </ul>
        /// <b>Kết quả (<c>InspectionReportDto</c>):</b>
        /// <ul>
        ///   <li><b>InspectionReportId</b>: ID báo cáo kiểm tra.</li>
        ///   <li><b>AppointmentId</b>: ID lịch hẹn liên kết.</li>
        ///   <li><b>UserId</b>: ID kỹ thuật viên tạo báo cáo.</li>
        ///   <li><b>FaultOwner</b>: Loại lỗi.</li>
        ///   <li><b>SolutionType</b>: Loại giải pháp.</li>
        ///   <li><b>Description</b>: Mô tả chi tiết lỗi.</li>
        ///   <li><b>Solution</b>: Giải pháp đề xuất.</li>
        ///   <li><b>Status</b>: Trạng thái báo cáo.</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo báo cáo.</li>
        ///   <li><b>AreaName</b>: Tên khu vực liên quan.</li>
        ///   <li><b>Technican</b>: Thông tin kỹ thuật viên.</li>
        ///   <li><b>Medias</b>: Danh sách media đính kèm.</li>
        ///   <li><b>ReportApprovals</b>: Danh sách phê duyệt báo cáo.</li>
        ///   <li><b>Appointment</b>: Thông tin lịch hẹn liên kết.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của lịch hẹn cần lấy báo cáo kiểm tra.</param>
        /// <returns>Thông tin cơ bản của báo cáo kiểm tra liên quan đến lịch hẹn.</returns>
        [HttpGet("inspection-report/by-appointment-id/{id}")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = nameof(AccountRole.Technician) + "," + nameof(AccountRole.Manager) + "," + nameof(AccountRole.Admin) + "," + nameof(AccountRole.TechnicianLead) + "," + nameof(AccountRole.Resident))]
        public async Task<IActionResult> GetInspectionReportByAppointmentIdAsync([FromRoute] int id)
        {
            var result = await _inspectionReporService.GetInspectionReportByAppointmentIdAsync(id);
            return Ok(result);
        }
    }
}
