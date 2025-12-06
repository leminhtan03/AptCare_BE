using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class RepairRequestTaskController : BaseApiController
    {
        private readonly IRepairRequestTaskService _repairRequestTaskService;

        public RepairRequestTaskController(IRepairRequestTaskService repairRequestTaskService)
        {
            _repairRequestTaskService = repairRequestTaskService;
        }

        /// <summary>
        /// Lấy danh sách nhiệm vụ sửa chữa theo ID yêu cầu sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Kĩ thuật viên, kĩ thuật viên trưởng, ban quản lí.<br/>
        /// Dùng để hiển thị danh sách checklist nhiệm vụ trong một yêu cầu sửa chữa.
        /// </remarks>
        /// <param name="repairRequestId">ID của yêu cầu sửa chữa cần lấy danh sách nhiệm vụ.</param>
        /// <returns>
        /// <b>RepairRequestTaskDto[]:</b>
        /// <ul>
        ///   <li><b>RepairRequestTaskId</b>: ID nhiệm vụ sửa chữa.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ.</li>
        ///   <li><b>Status</b>: Trạng thái nhiệm vụ (Pending/InProgress/Completed).</li>
        ///   <li><b>InspectionResult</b>: Kết quả kiểm tra.</li>
        ///   <li><b>CompletedAt</b>: Thời gian hoàn thành.</li>
        ///   <li><b>CompletedBy</b>: Người hoàn thành.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách nhiệm vụ sửa chữa.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("by-repair-request")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)},{nameof(AccountRole.TechnicianLead)},{nameof(AccountRole.Manager)}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<RepairRequestTaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetRepairRequestTasksByRepairRequestId(int repairRequestId)
        {
            var result = await _repairRequestTaskService.GetRepairRequestTasksByRepairRequestIdAsync(repairRequestId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhiệm vụ sửa chữa theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ sửa chữa cần lấy thông tin.</param>
        /// <returns>
        /// <b>RepairRequestTaskDto:</b>
        /// <ul>
        ///   <li><b>RepairRequestTaskId</b>: ID nhiệm vụ sửa chữa.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>Status</b>: Trạng thái nhiệm vụ.</li>
        ///   <li><b>TechnicianNote</b>: Ghi chú của kỹ thuật viên.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về thông tin nhiệm vụ sửa chữa.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ sửa chữa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(RepairRequestTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetRepairRequestTaskById(int id)
        {
            var result = await _repairRequestTaskService.GetRepairRequestTaskByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật trạng thái nhiệm vụ sửa chữa (dành cho kỹ thuật viên).
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// Dùng để kỹ thuật viên cập nhật trạng thái hoàn thành nhiệm vụ trong quá trình sửa chữa.<br/><br/>
        /// <b>Giá trị Status (TaskCompletionStatus):</b>
        /// <ul>
        ///   <li><b>1</b> - Pending: Chưa thực hiện</li>
        ///   <li><b>2</b> - Completed: Đã hoàn thành</li>
        ///   <li><b>3</b> - Failed: Thất bại</li>
        /// </ul>
        /// <b>Giá trị InspectionResult gợi ý:</b>
        /// <ul>
        ///   <li><b>OK</b>: Hoạt động tốt, không có vấn đề</li>
        ///   <li><b>Need Repair</b>: Cần sửa chữa</li>
        ///   <li><b>Need Replacement</b>: Cần thay thế</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ sửa chữa cần cập nhật trạng thái.</param>
        /// <param name="dto">Dữ liệu cập nhật trạng thái.</param>
        /// <returns>Thông báo cập nhật trạng thái thành công.</returns>
        /// <response code="200">Cập nhật trạng thái nhiệm vụ sửa chữa thành công.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ sửa chữa.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateRepairRequestTaskStatus(int id, RepairRequestTaskStatusUpdateDto dto)
        {
            var result = await _repairRequestTaskService.UpdateRepairRequestTaskStatusAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật hàng loạt trạng thái nhiệm vụ sửa chữa theo ID yêu cầu sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Technician, TechnicianLead<br/>
        /// Dùng để kỹ thuật viên cập nhật trạng thái cho nhiều nhiệm vụ cùng lúc trước khi tạo báo cáo kiểm tra.<br/>
        /// <b>Lưu ý:</b> Tất cả nhiệm vụ trong yêu cầu sửa chữa phải được cập nhật và hoàn thành.<br/><br/>
        /// <b>Giá trị Status (TaskCompletionStatus):</b>
        /// <ul>
        ///   <li><b>1</b> - Pending: Chưa thực hiện</li>
        ///   <li><b>2</b> - Completed: Đã hoàn thành </li>
        ///   <li><b>3</b> - Failed: Thất bại</li>
        /// </ul>
        /// <b>Giá trị InspectionResult gợi ý:</b>
        /// <ul>
        ///   <li><b>OK</b>: Hoạt động tốt, không có vấn đề</li>
        ///   <li><b>Need Repair</b>: Cần sửa chữa</li>
        ///   <li><b>Need Replacement</b>: Cần thay thế</li>
        /// </ul>
        /// <b>Request Body Example:</b>
        /// <code>
        /// [
        ///   {
        ///     "repairRequestTaskId": 1,
        ///     "status": Completed,
        ///     "technicianNote": "Đã thay thế van nước bị hỏng. Kiểm tra không còn rò rỉ.",
        ///     "inspectionResult": "OK"
        ///   },
        ///   {
        ///     "repairRequestTaskId": 2,
        ///     "status": Failed,
        ///     "technicianNote": "Phát hiện ống bị nứt, cần thay thế mới.",
        ///     "inspectionResult": "Cần thay thế"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <param name="repairRequestId">ID của yêu cầu sửa chữa cần cập nhật nhiệm vụ.</param>
        /// <param name="updatedTasks">Danh sách nhiệm vụ cần cập nhật trạng thái.</param>
        /// <returns>Thông báo cập nhật trạng thái hàng loạt thành công.</returns>
        /// <response code="200">Cập nhật trạng thái nhiệm vụ sửa chữa thành công.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc thiếu nhiệm vụ chưa cập nhật.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("repair-request/{repairRequestId}/batch-update")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateRepairRequestTasksStatus(
            int repairRequestId, 
            [FromBody] List<RequestTaskStatusUpdateDto> updatedTasks)
        {
            var result = await _repairRequestTaskService.UpdateRepairRequestTasksStatusAsync(repairRequestId, updatedTasks);
            return Ok(result);
        }
    }
}