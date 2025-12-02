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
    }
}
