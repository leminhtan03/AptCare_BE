using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.ReportDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class ReportController : BaseApiController
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Lấy danh sách báo cáo có phân trang, tìm kiếm và lọc.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, Admin  
        ///  
        /// **Tham số (ReportFilterDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo tiêu đề, mô tả, tên đối tượng).  
        /// - <b>filter</b>: Lọc theo trạng thái (Active/Inactive).  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp:
        ///   - <b>date</b>: Sắp xếp theo ngày tạo tăng dần.  
        ///   - <b>date_desc</b>: Sắp xếp theo ngày tạo giảm dần.  
        ///   - <b>title</b>: Sắp xếp theo tiêu đề tăng dần.  
        ///   - <b>title_desc</b>: Sắp xếp theo tiêu đề giảm dần.  
        /// - <b>Fromdate</b>: Lọc từ ngày (DateOnly).  
        /// - <b>Todate</b>: Lọc đến ngày (DateOnly).  
        /// - <b>CommonAreaObjectId</b>: Lọc theo đối tượng khu vực chung.  
        /// - <b>UserId</b>: Lọc theo người tạo báo cáo.  
        /// </remarks>
        /// <param name="filterDto">Thông tin phân trang và lọc.</param>
        /// <returns>Danh sách báo cáo kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách báo cáo.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpGet]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(IPaginate<ReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetPaginateReports([FromQuery] ReportFilterDto filterDto)
        {
            var result = await _reportService.GetPaginateReportsAsync(filterDto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách báo cáo theo đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, Admin  
        ///  
        /// Dùng để xem tất cả báo cáo liên quan đến một đối tượng khu vực chung cụ thể.
        /// </remarks>
        /// <param name="commonAreaObjectId">ID của đối tượng khu vực chung.</param>
        /// <returns>Danh sách báo cáo.</returns>
        /// <response code="200">Trả về danh sách báo cáo.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpGet("by-common-area-object/{commonAreaObjectId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(IEnumerable<ReportBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetReportsByCommonAreaObject(int commonAreaObjectId)
        {
            var result = await _reportService.GetReportsByCommonAreaObjectAsync(commonAreaObjectId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách báo cáo của người dùng hiện tại.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Resident  
        ///  
        /// **Tham số (ReportFilterDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo tiêu đề, mô tả, tên đối tượng).  
        /// - <b>filter</b>: Lọc theo trạng thái (Active/Inactive).  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp:
        ///   - <b>date</b>: Sắp xếp theo ngày tạo tăng dần.  
        ///   - <b>date_desc</b>: Sắp xếp theo ngày tạo giảm dần.  
        ///   - <b>title</b>: Sắp xếp theo tiêu đề tăng dần.  
        ///   - <b>title_desc</b>: Sắp xếp theo tiêu đề giảm dần.  
        /// - <b>Fromdate</b>: Lọc từ ngày (DateOnly).  
        /// - <b>Todate</b>: Lọc đến ngày (DateOnly).  
        /// - <b>CommonAreaObjectId</b>: Lọc theo đối tượng khu vực chung.  
        /// </remarks>
        /// <param name="filterDto">Thông tin phân trang và lọc.</param>
        /// <returns>Danh sách báo cáo kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách báo cáo.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpGet("my-reports")]
        [Authorize(Roles = nameof(AccountRole.Resident))]
        [ProducesResponseType(typeof(IEnumerable<ReportBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMyReports([FromQuery] ReportFilterDto filterDto)
        {
            var result = await _reportService.GetMyReportsAsync(filterDto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một báo cáo theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// </remarks>
        /// <param name="id">ID của báo cáo cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của báo cáo.</returns>
        /// <response code="200">Trả về thông tin báo cáo.</response>
        /// <response code="404">Không tìm thấy báo cáo.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetReportById(int id)
        {
            var result = await _reportService.GetReportByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một báo cáo.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Resident  
        ///  
        /// Người dùng có thể báo cáo các vấn đề phát hiện tại khu vực chung.
        /// Dữ liệu yêu cầu gồm: ID đối tượng khu vực chung, tiêu đề, mô tả, file đính kèm (tùy chọn).
        /// </remarks>
        /// <param name="dto">Thông tin báo cáo cần tạo.</param>
        /// <returns>Thông tin báo cáo đã tạo.</returns>
        /// <response code="201">Báo cáo được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc đối tượng khu vực chung không hoạt động.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Resident))]
        [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateReport([FromForm] ReportCreateDto dto)
        {
            var result = await _reportService.CreateReportAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin báo cáo theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Resident (chỉ cập nhật được báo cáo của chính mình)  
        ///  
        /// Cập nhật thông tin như: tiêu đề, mô tả, đối tượng khu vực chung, trạng thái.
        /// </remarks>
        /// <param name="id">ID của báo cáo cần cập nhật.</param>
        /// <param name="dto">Thông tin báo cáo cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật báo cáo thành công.</response>
        /// <response code="404">Không tìm thấy báo cáo hoặc đối tượng khu vực chung.</response>
        /// <response code="403">Không có quyền cập nhật báo cáo này.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Resident))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> UpdateReport(int id, ReportUpdateDto dto)
        {
            var result = await _reportService.UpdateReportAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một báo cáo theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Resident (chỉ xóa được báo cáo của chính mình)  
        ///  
        /// Xóa báo cáo sẽ xóa cả các file đính kèm liên quan.
        /// </remarks>
        /// <param name="id">ID của báo cáo cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Báo cáo được xóa thành công.</response>
        /// <response code="404">Không tìm thấy báo cáo.</response>
        /// <response code="403">Không có quyền xóa báo cáo này.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Resident))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteReport(int id)
        {
            var result = await _reportService.DeleteReportAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt báo cáo.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, Admin  
        ///  
        /// Chuyển trạng thái của báo cáo từ Inactive sang Active.
        /// </remarks>
        /// <param name="id">ID của báo cáo cần kích hoạt.</param>
        /// <returns>Thông báo kích hoạt thành công.</returns>
        /// <response code="200">Kích hoạt báo cáo thành công.</response>
        /// <response code="404">Không tìm thấy báo cáo.</response>
        /// <response code="400">Báo cáo đã ở trạng thái hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ActivateReport(int id)
        {
            var result = await _reportService.ActivateReportAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa báo cáo.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager, Admin  
        ///  
        /// Chuyển trạng thái của báo cáo từ Active sang Inactive.
        /// </remarks>
        /// <param name="id">ID của báo cáo cần vô hiệu hóa.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
        /// <response code="200">Vô hiệu hóa báo cáo thành công.</response>
        /// <response code="404">Không tìm thấy báo cáo.</response>
        /// <response code="400">Báo cáo đã ở trạng thái ngưng hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeactivateReport(int id)
        {
            var result = await _reportService.DeactivateReportAsync(id);
            return Ok(result);
        }
    }
}