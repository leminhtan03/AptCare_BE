using AptCare.Repository.Enum.AccountUserEnum;
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
        /// Dùng để kỹ thuật viên cập nhật trạng thái hoàn thành nhiệm vụ trong quá trình sửa chữa.
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
    }
}