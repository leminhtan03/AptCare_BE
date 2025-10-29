using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Services.Interfaces;
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
        /// Phê duyệt hoặc từ chối báo cáo dựa trên thông tin được cung cấp.
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Cho phép người dùng có quyền phê duyệt hoặc từ chối báo cáo.  
        /// - Quá trình phê duyệt dựa trên vai trò của người dùng và loại báo cáo.  
        /// - Ghi nhận nhận xét (tùy chọn) khi phê duyệt hoặc từ chối.
        /// 
        /// **Ràng buộc:**  
        /// - `Status`: Chỉ chấp nhận giá trị enum `ReportStatus`:
        ///   - `1` = Pending (Đang chờ)
        ///   - `2` = Approved (Đã phê duyệt)
        ///   - `3` = Rejected (Đã từ chối)
        /// - 'ReportType': Chấp nhận các loại báo cáo hợp lệ như 'RepairReport', 'InspectionReport'
        /// </remarks>
        /// <param name="dto">Thông tin phê duyệt báo cáo bao gồm ReportId, ReportType, Role (enum), Status (enum) và Comment (tùy chọn)</param>
        /// <returns>Thông báo xác nhận báo cáo đã được xử lý thành công</returns>
        [HttpPost("approve-report")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveReportAsync([FromBody] ApproveReportCreateDto dto)
        {
            var result = await _reportApprovalService.ApproveReportAsync(dto);
            return Ok(result);
        }
    }
}
