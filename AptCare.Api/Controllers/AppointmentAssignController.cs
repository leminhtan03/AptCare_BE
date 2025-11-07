using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AptCare.Api.Controllers;
using AptCare.Service.Dtos.AppointmentAssignDtos;
using AptCare.Service.Services.Interfaces;
using AptCare.Repository.Enum.AccountUserEnum;

public class AppointmentAssignController : BaseApiController
{
    private readonly IAppointmentAssignService _appointmentAssignService;

    public AppointmentAssignController(IAppointmentAssignService appointmentAssignService)
    {
        _appointmentAssignService = appointmentAssignService;
    }

    /// <summary>
    /// Phân công kỹ thuật viên cho một lịch hẹn.
    /// </summary>
    /// <remarks>
    /// **Chỉ role:** TechnicianLead  
    /// - Kiểm tra kỹ thuật viên tồn tại và chưa được phân công cho lịch này.  
    /// - Gán thời gian ước tính theo lịch hẹn.  
    /// </remarks>
    /// <param name="appointmentId">ID lịch hẹn</param>
    /// <param name="userIds">ID các kỹ thuật viên</param>
    [HttpPost("assign")]
    [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignAppointment(int appointmentId, IEnumerable<int> userIds)
    {
        var result = await _appointmentAssignService.AssignAppointmentAsync(appointmentId, userIds);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật lịch phân công của kỹ thuật viên.
    /// </summary>
    /// <remarks>
    /// **Chỉ role:** Technician (chính chủ lịch phân công)  
    /// Cho phép cập nhật các thông tin như **thời gian ước tính**, **ghi chú**, **trạng thái công việc** (Pending → InProgress → Completed / Canceled).  
    /// </remarks>
    /// <param name="id">ID lịch phân công</param>
    /// <param name="dto">Dữ liệu cập nhật</param>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{nameof(AccountRole.Technician)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAppointmentAssign(int id, [FromBody] AppointmentAssignUpdateDto dto)
    {
        var result = await _appointmentAssignService.UpdateAppointmentAssignAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Gợi ý danh sách kỹ thuật viên phù hợp cho lịch hẹn.
    /// </summary>
    /// <remarks>
    /// **Chỉ role:** TechnicianLead  
    /// - Ưu tiên kỹ thuật viên có kỹ năng phù hợp, ca làm việc trùng khớp, và ít lịch trong ngày/tháng.  
    /// - Với lịch khẩn (Emergency), sẽ ưu tiên người đang trực.  
    /// </remarks>
    /// <param name="appointmentId">ID lịch hẹn</param>
    /// <param name="techniqueId">ID kỹ thuật (tùy chọn)</param>
    [HttpGet("suggest")]
    [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
    [ProducesResponseType(typeof(IEnumerable<SuggestedTechnicianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuggestTechnicians([FromQuery] int appointmentId, [FromQuery] int? techniqueId)
    {
        var result = await _appointmentAssignService.SuggestTechniciansForAppointment(appointmentId, techniqueId);
        return Ok(result);
    }
    /// <summary>
    /// Xác nhận hoặc hủy xác nhận việc phân công kỹ thuật viên cho lịch hẹn.
    /// </summary>
    /// <remarks>
    /// **Chỉ role:** TechnicianLead  
    /// - Cho phép xác nhận hoặc hủy xác nhận các lịch phân công đã được tạo cho một appointment.  
    /// - Thường dùng sau khi đã phân công xong và cần confirm lại trước khi technician bắt đầu làm việc.  
    /// </remarks>
    /// <param name="appointmentId">ID lịch hẹn cần xác nhận phân công</param>
    /// <param name="isConfirmed">True để xác nhận, False để hủy xác nhận</param>
    /// <returns>Thông báo kết quả xác nhận</returns>
    [HttpPut("confirm-assigned/{appointmentId}")]
    [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmAssignment(int appointmentId, [FromQuery] bool isConfirmed)
    {
        var result = await _appointmentAssignService.ConfirmAssignmentAsync(appointmentId, isConfirmed);
        return Ok(result);
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAppointmentAssign(CancleAssignDto dto)
    {
        var result = await _appointmentAssignService.CancleAssignmentAsync(dto);
        return Ok(result);
    }

}
