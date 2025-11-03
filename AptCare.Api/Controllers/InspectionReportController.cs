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
        /// **Chức năng:**  
        /// - Tạo báo cáo kiểm tra dựa trên thông tin lịch hẹn.  
        /// - Xác định loại lỗi (lỗi tòa nhà hoặc lỗi cư dân).  
        /// - Ghi nhận giải pháp xử lý (sửa chữa, thay thế, thuê ngoài).  
        /// - Lưu mô tả chi tiết và giải pháp đề xuất.
        /// 
        /// **Ràng buộc:**  
        /// - `FaultOwner`: Chỉ chấp nhận giá trị enum `FaultType`:
        ///   - `1` = BuildingFault (Lỗi tòa nhà)
        ///   - `2` = ResidentFault (Lỗi cư dân)
        /// - `SolutionType`: Chỉ chấp nhận giá trị enum `SolutionType`:
        ///   - `1` = Repair (Sửa chữa)
        ///   - `2` = Replacement (Thay thế)
        ///   - `3` = Outsource (Thuê ngoài)
        /// </remarks>
        /// <param name="dto">Thông tin báo cáo kiểm tra bao gồm AppointmentId, FaultOwner (enum), SolutionType (enum), Description và Solution</param>
        /// <returns>Thông báo xác nhận tạo báo cáo thành công</returns>
        [HttpPost("generate-inspection-report")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        public async Task<IActionResult> GenerateInspectionReportAsync([FromBody] CreateInspectionReporDto dto)
        {
            var result = await _inspectionReporService.CreateInspectionReportAsync(dto);
            return Ok(result);
        }
        /// <summary>
        /// Lấy thông tin chi tiết của báo cáo kiểm tra theo ID.
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Truy xuất thông tin chi tiết của một báo cáo kiểm tra cụ thể.  
        /// - Hiển thị thông tin bao gồm: loại lỗi, giải pháp xử lý, mô tả, trạng thái báo cáo.  
        /// - Bao gồm thông tin lịch hẹn liên quan và kỹ thuật viên thực hiện.
        /// 
        /// **Kết quả trả về:**  
        /// - `InspectionReportId`: ID của báo cáo kiểm tra
        /// - `AppointmentId`: ID lịch hẹn liên quan
        /// - `FaultOwner`: Loại lỗi (BuildingFault = 1, ResidentFault = 2)
        /// - `SolutionType`: Loại giải pháp (Repair = 1, Replacement = 2, Outsource = 3)
        /// - `Description`: Mô tả chi tiết lỗi
        /// - `Solution`: Giải pháp đề xuất
        /// - `Status`: Trạng thái báo cáo
        /// - `CreatedAt`: Thời gian tạo báo cáo
        /// - `AreaName`: Tên khu vực
        /// - `Technican`: Thông tin kỹ thuật viên
        /// </remarks>
        /// <param name="id">ID của báo cáo kiểm tra cần lấy thông tin</param>
        /// <returns>Thông tin chi tiết của báo cáo kiểm tra</returns>
        [HttpGet("get-inspection-report-by-id/{id}")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
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
        /// **Chức năng:**  
        /// - Lấy danh sách tất cả các báo cáo kiểm tra với hỗ trợ phân trang.  
        /// - Hỗ trợ tìm kiếm theo từ khóa.  
        /// - Hỗ trợ lọc theo các tiêu chí cụ thể.  
        /// - Hỗ trợ sắp xếp theo các trường dữ liệu.
        /// 
        /// **Tham số:**  
        /// - `page`: Số trang (mặc định = 1)
        /// - `size`: Số lượng bản ghi mỗi trang (mặc định = 10)
        /// - `sortBy`: Trường dữ liệu để sắp xếp (VD: "id", "id_des")
        /// - `search`: Từ khóa tìm kiếm (tìm trong Description, Solution, AreaName)
        /// - `filter`: Bộ lọc theo điều kiện (VD:Pending ,Approved ,Rejected)
        /// - `FaultType`: Lọc theo loại lỗi (BuildingFault ,ResidentFault)
        /// - `SolutionType`: Lọc theo loại giải pháp (Repair ,Replacement ,Outsource)
        /// 
        /// **Kết quả trả về:**  
        /// - `Size`: Số lượng bản ghi mỗi trang
        /// - `Page`: Trang hiện tại
        /// - `Total`: Tổng số bản ghi
        /// - `TotalPages`: Tổng số trang
        /// - `Items`: Danh sách báo cáo kiểm tra (InspectionBasicReportDto[])
        /// </remarks>
        /// <param name="filterDto">Tham số phân trang và lọc dữ liệu</param>
        /// <returns>Danh sách báo cáo kiểm tra có phân trang</returns>
        [HttpGet("get-paginate-inspection-reports")]
        [ProducesResponseType(typeof(IPaginate<InspectionBasicReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = nameof(AccountRole.Technician) + "," + nameof(AccountRole.Manager) + "," + nameof(AccountRole.Admin) + "," + nameof(AccountRole.TechnicianLead))]
        public async Task<IActionResult> GetPaginateInspectionReportsAsync([FromForm] InspectionReportFilterDto filterDto)
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
        /// **Chức năng:**  
        /// - Truy xuất thông tin báo cáo kiểm tra dựa trên ID lịch hẹn.  
        /// - Hiển thị thông tin cơ bản của báo cáo bao gồm: loại lỗi, giải pháp xử lý, trạng thái.  
        /// - Bao gồm thông tin kỹ thuật viên thực hiện và khu vực liên quan.  
        /// - Hiển thị danh sách media (hình ảnh/video) đính kèm nếu có.
        /// 
        /// **Kết quả trả về:**  
        /// - `InspectionReportId`: ID của báo cáo kiểm tra
        /// - `FaultOwner`: Loại lỗi (BuildingFault = 1, ResidentFault = 2)
        /// - `SolutionType`: Loại giải pháp (Repair = 1, Replacement = 2, Outsource = 3)
        /// - `Status`: Trạng thái báo cáo (Pending, Approved, Rejected)
        /// - `CreatedAt`: Thời gian tạo báo cáo
        /// - `AreaName`: Tên khu vực
        /// - `Technican`: Thông tin kỹ thuật viên (TechnicanDto)
        /// - `Medias`: Danh sách media đính kèm (List&lt;MediaDto&gt;)
        /// 
        /// **Lưu ý:**  
        /// - Trả về 404 nếu không tìm thấy báo cáo với AppointmentId được cung cấp
        /// - Chỉ người dùng có quyền phù hợp mới có thể truy cập
        /// </remarks>
        /// <param name="id">ID của lịch hẹn cần lấy báo cáo kiểm tra</param>
        /// <returns>Thông tin cơ bản của báo cáo kiểm tra liên quan đến lịch hẹn</returns>
        [HttpGet("get-inspection-report-by-appointment-id/{id}")]
        [ProducesResponseType(typeof(InspectionReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = nameof(AccountRole.Technician) + "," + nameof(AccountRole.Manager) + "," + nameof(AccountRole.Admin) + "," + nameof(AccountRole.Resident))]
        public async Task<IActionResult> GetInspectionReportByAppointmentIdAsync([FromRoute] int id)
        {
            var result = await _inspectionReporService.GetInspectionReportByAppointmentIdAsync(id);
            return Ok(result);
        }
    }
}
