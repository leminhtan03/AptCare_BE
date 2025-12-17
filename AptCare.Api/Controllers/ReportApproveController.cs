using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class ReportApproveController : BaseApiController
    {
        private readonly IReportApprovalService _reportApprovalService;
        public ReportApproveController(IReportApprovalService reportApprovalService)
        {
            _reportApprovalService = reportApprovalService;
        }
        /// <summary>
        /// Phê duyệt, từ chối hoặc chuyển tiếp báo cáo lên cấp cao hơn.
        /// </summary>
        /// <remarks>
        /// <b>Workflow:</b>
        /// <list type="number">
        ///   <item><b>Technician tạo InspectionReport</b> → Tự động tạo approval cho <b>TechnicianLead</b></item>
        ///   <item><b>TechnicianLead</b> có 2 lựa chọn:
        ///     <list type="bullet">
        ///       <item><b>Approve &amp; Escalate</b> (<c>EscalateToHigherLevel = true</c>) → Tạo approval mới cho <b>Manager</b></item>
        ///       <item><b>Reject</b> (<c>Status = Rejected</c>) → Kết thúc, yêu cầu Technician sửa lại</item>
        ///     </list>
        ///   </item>
        ///   <item><b>Manager</b> approve cuối cùng (<c>Status = Approved</c>) → Hoàn tất</item>
        /// </list>
        /// <br/>
        /// <b>Tham số:</b>
        /// <list type="bullet">
        ///   <item><c>ReportId</c>: ID của báo cáo</item>
        ///   <item><c>ReportType</c>: "InspectionReport" hoặc "RepairReport"</item>
        ///   <item><c>Status</c>:
        ///     <list type="bullet">
        ///       <item><b>"Approved"</b> (2) - Phê duyệt</item>
        ///       <item><b>"Rejected"</b> (3) - Từ chối</item>
        ///     </list>
        ///   </item>
        ///   <item><c>EscalateToHigherLevel</c>:
        ///     <list type="bullet">
        ///       <item><c>true</c> - Chuyển lên Manager (chỉ TechnicianLead)</item>
        ///       <item><c>false</c> - Approve/Reject ở cấp hiện tại</item>
        ///     </list>
        ///   </item>
        ///   <item><c>Comment</c> (tùy chọn): Ghi chú kèm theo khi phê duyệt</item>
        /// </list>
        /// <br/>
        /// <b>Ví dụ:</b>
        /// <br/>
        /// <b>TechnicianLead escalate lên Manager:</b>
        /// <code>
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 2,
        ///   "escalateToHigherLevel": true,
        ///   "comment": "Báo cáo hợp lệ, chuyển lên Manager xác nhận chi phí"
        /// }
        /// </code>
        /// <br/>
        /// <b>Manager approve cuối cùng:</b>
        /// <code>
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 2,
        ///   "escalateToHigherLevel": false,
        ///   "comment": "Đã kiểm tra và phê duyệt toàn bộ"
        /// }
        /// </code>
        /// <br/>
        /// <b>TechnicianLead reject:</b>
        /// <code>
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 3,
        ///   "escalateToHigherLevel": false,
        ///   "comment": "Thiếu ảnh chụp hiện trường, yêu cầu bổ sung"
        /// }
        /// </code>
        /// </remarks>
        /// <param name="dto">Thông tin phê duyệt báo cáo bao gồm ReportId, ReportType, Status (enum), EscalateToHigherLevel và Comment (tùy chọn)</param>
        /// <returns>Thông báo xác nhận báo cáo đã được xử lý thành công.</returns>
        /// <response code="200">Phê duyệt báo cáo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc trạng thái không được phép.</response>
        /// <response code="403">Không đủ quyền thực hiện thao tác này.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("approve-report")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveReportAsync([FromBody] ApproveReportCreateDto dto)
        {
            var result = await _reportApprovalService.ApproveReportAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Cư dân phê duyệt báo cáo sửa chữa sau khi hoàn thành công việc.
        /// </summary>
        /// <remarks>
        /// <b>Mục đích:</b>
        /// <list type="bullet">
        ///   <item>Cho phép cư dân xác nhận rằng công việc sửa chữa đã được hoàn thành đúng yêu cầu</item>
        ///   <item>Chỉ cư dân thuộc căn hộ có yêu cầu sửa chữa mới có quyền phê duyệt</item>
        ///   <item>Báo cáo phải có trạng thái <b>Pending</b> (chờ phê duyệt của cư dân)</item>
        /// </list>
        /// <br/>
        /// <b>Workflow:</b>
        /// <list type="number">
        ///   <item>Kỹ thuật viên hoàn thành công việc sửa chữa và tạo <b>RepairReport</b></item>
        ///   <item>Hệ thống tự động tạo approval với <c>Role = Resident</c> và <c>Status = Pending</c></item>
        ///   <item>Cư dân kiểm tra công việc tại căn hộ</item>
        ///   <item>Cư dân gọi API này để xác nhận đã hài lòng với kết quả</item>
        ///   <item>Trạng thái approval chuyển sang <b>ResidentApproved</b> (4)</item>
        /// </list>
        /// <br/>
        /// <b>Điều kiện:</b>
        /// <list type="bullet">
        ///   <item>User phải có role <b>Resident</b></item>
        ///   <item>User phải thuộc căn hộ có yêu cầu sửa chữa (<c>UserApartment.Status = Active</c>)</item>
        ///   <item>Báo cáo sửa chữa phải tồn tại</item>
        ///   <item>Approval của resident phải ở trạng thái <b>Pending</b></item>
        /// </list>
        /// <br/>
        /// <b>Ví dụ request:</b>
        /// <code>
        /// POST /api/ReportApprove/resident/approve/15
        /// </code>
        /// </remarks>
        /// <param name="repairReportId">ID của báo cáo sửa chữa cần phê duyệt</param>
        /// <returns>Trả về true nếu phê duyệt thành công</returns>
        /// <response code="200">Cư dân đã phê duyệt báo cáo sửa chữa thành công</response>
        /// <response code="404">Không tìm thấy báo cáo sửa chữa hoặc không tìm thấy approval pending</response>
        /// <response code="400">Cư dân không thuộc căn hộ của yêu cầu sửa chữa này</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền Resident</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost("resident/approve/{repairReportId}")]
        [Authorize(Roles = nameof(AccountRole.Resident))]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResidentApproveRepairReportAsync([FromRoute] int repairReportId)
        {
            var result = await _reportApprovalService.ResidentApproveRepairReportAsync(repairReportId);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra trạng thái phê duyệt của cư dân cho báo cáo sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Mục đích:</b>
        /// <list type="bullet">
        ///   <item>Cho phép kiểm tra xem cư dân đã phê duyệt báo cáo sửa chữa chưa</item>
        ///   <item>Hỗ trợ UI hiển thị trạng thái approval trên giao diện</item>
        ///   <item>Tránh gọi API approve nhiều lần khi đã được phê duyệt</item>
        /// </list>
        /// <br/>
        /// <b>Kết quả trả về:</b>
        /// <list type="bullet">
        ///   <item><c>true</c> - Báo cáo đã được cư dân phê duyệt (<c>Status = ResidentApproved</c>)</item>
        ///   <item><c>false</c> - Báo cáo chưa được phê duyệt (<c>Status = Pending</c>)</item>
        /// </list>
        /// <br/>
        /// <b>Use case:</b>
        /// <list type="number">
        ///   <item><b>Mobile App:</b> Hiển thị nút "Phê duyệt" hoặc "Đã phê duyệt" dựa trên kết quả</item>
        ///   <item><b>Web Dashboard:</b> Hiển thị badge trạng thái approval của cư dân</item>
        ///   <item><b>Notification:</b> Kiểm tra trước khi gửi reminder cho cư dân</item>
        /// </list>
        /// <br/>
        /// <b>Ví dụ request:</b>
        /// <code>
        /// GET /api/ReportApprove/resident/check/15
        /// </code>
        /// <br/>
        /// <b>Ví dụ response (đã phê duyệt):</b>
        /// <code>
        /// true
        /// </code>
        /// <br/>
        /// <b>Ví dụ response (chưa phê duyệt):</b>
        /// <code>
        /// false
        /// </code>
        /// </remarks>
        /// <param name="repairReportId">ID của báo cáo sửa chữa cần kiểm tra</param>
        /// <returns>True nếu đã được phê duyệt, False nếu chưa được phê duyệt</returns>
        /// <response code="200">Trả về trạng thái phê duyệt (true/false)</response>
        /// <response code="404">Không tìm thấy báo cáo sửa chữa hoặc không tìm thấy resident approval</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("resident/check/{repairReportId}")]
        [Authorize]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckResidentApproveRepairReportAsync([FromRoute] int repairReportId)
        {
            var result = await _reportApprovalService.CheckResidentApproveRepairReportAsync(repairReportId);
            return Ok(result);
        }
    }
}
