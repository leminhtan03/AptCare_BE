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
        /// **Workflow:**
        /// 
        /// 1. **Technician tạo InspectionReport** → Tự động tạo approval cho **TechnicianLead**
        /// 2. **TechnicianLead** có 2 lựa chọn:
        ///    - **Approve & Escalate** (`EscalateToHigherLevel = true`) → Tạo approval mới cho **Manager**
        ///    - **Reject** (`Status = Rejected`) → Kết thúc, yêu cầu Technician sửa lại
        /// 3. **Manager** approve cuối cùng (`Status = Approved`) → Hoàn tất
        /// 
        /// **Parameters:**
        /// - `ReportId`: ID của báo cáo
        /// - `ReportType`: "InspectionReport" hoặc "RepairReport"
        /// - `Status`: 
        ///   - `Approved` (2) - Phê duyệt
        ///   - `Rejected` (3) - Từ chối
        /// - `EscalateToHigherLevel`: 
        ///   - `true` - Chuyển lên Manager (chỉ TechnicianLead)
        ///   - `false` - Approve/Reject ở cấp hiện tại
        /// 
        /// **Examples:**
        /// 
        /// **TechnicianLead escalate lên Manager:**
        /// ```json
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 2,
        ///   "escalateToHigherLevel": true,
        ///   "comment": "Báo cáo hợp lệ, chuyển lên Manager xác nhận chi phí"
        /// }
        /// ```
        /// 
        /// **Manager approve cuối cùng:**
        /// ```json
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 2,
        ///   "escalateToHigherLevel": false,
        ///   "comment": "Đã kiểm tra và phê duyệt toàn bộ"
        /// }
        /// ```
        /// 
        /// **TechnicianLead reject:**
        /// ```json
        /// {
        ///   "reportId": 5,
        ///   "reportType": "InspectionReport",
        ///   "status": 3,
        ///   "escalateToHigherLevel": false,
        ///   "comment": "Thiếu ảnh chụp hiện trường, yêu cầu bổ sung"
        /// }
        /// ```
        /// </remarks>
        /// <param name="dto">Thông tin phê duyệt báo cáo bao gồm ReportId, ReportType, Role (enum), Status (enum) và Comment (tùy chọn)</param>
        /// <returns>Thông báo xác nhận báo cáo đã được xử lý thành công</returns>
        [HttpPost("approve-report")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
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
