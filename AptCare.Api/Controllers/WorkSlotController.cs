using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.WorkSlotDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class WorkSlotController : BaseApiController
    {
        private readonly IWorkSlotService _workSlotService;

        public WorkSlotController(IWorkSlotService workSlotService)
        {
            _workSlotService = workSlotService;
        }

        /// <summary>
        /// Tạo lịch làm việc liên tục từ ngày bắt đầu đến ngày kết thúc (cùng 1 ca làm việc).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Tự động tạo lịch cho kỹ thuật viên trong khoảng thời gian nhất định, theo slot cố định (ví dụ: ca sáng).  
        ///  
        /// **Enum SlotTime:**  
        /// - `Slot1` — 8:00 - 16:00  
        /// - `Slot2` — 16:00 - 24:00  
        /// - `Slot3` — 00:00 - 8:00  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ  
        ///  
        /// **Ràng buộc:**  
        /// - 'FromDate' phải nhỏ hơn hoặc bằng 'ToDate'.  
        /// - Mỗi kỹ thuật viên, ngày và slot chỉ có thể tồn tại một lịch làm việc duy nhất.  
        /// </remarks>
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [HttpPost("from-date-to-date")]
        public async Task<ActionResult> CreateWorkSlotsFromDateToDate([FromBody] WorkSlotCreateFromDateToDateDto dto)
        {
            var result = await _workSlotService.CreateWorkSlotsFromDateToDateAsync(dto);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Tạo lịch làm việc cho danh sách ngày và ca cụ thể.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Cho phép tạo nhiều lịch làm việc không liên tiếp — ví dụ:  
        /// - 01/10/2025 (Slot1)  
        /// - 03/10/2025 (Slot2)  
        /// - 05/10/2025 (Slot3)  
        ///  
        /// **Enum SlotTime:**  
        /// - `Slot1` — 8:00 - 16:00  
        /// - `Slot2` — 16:00 - 24:00  
        /// - `Slot3` — 00:00 - 8:00  
        ///  
        /// **Ràng buộc:** Mỗi kỹ thuật viên, ngày và slot chỉ có thể có 1 lịch duy nhất.
        /// </remarks>
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [HttpPost("date-slot-list")]
        public async Task<ActionResult> CreateWorkSlotsDateSlot([FromBody] WorkSlotCreateDateSlotDto dto)
        {
            var result = await _workSlotService.CreateWorkSlotsDateSlotAsync(dto);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Cập nhật lịch làm việc theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Cập nhật thông tin như: kỹ thuật viên, ngày làm việc, ca làm và trạng thái.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ  
        ///  
        /// **Ràng buộc:** Không thể cập nhật sang lịch đã tồn tại (trùng TechnicianId, Date, Slot).
        /// </remarks>
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateWorkSlot(int id, [FromBody] WorkSlotUpdateDto dto)
        {
            var result = await _workSlotService.UpdateWorkSlotAsync(id, dto);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Xóa lịch làm việc theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Xóa hoàn toàn lịch làm việc của kỹ thuật viên khỏi hệ thống.  
        /// **Lưu ý:** Không thể khôi phục lại lịch đã xóa.
        /// </remarks>
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWorkSlot(int id)
        {
            var result = await _workSlotService.DeleteWorkSlotAsync(id);
            return Ok(new { message = result });
        }

        /// <summary>
        /// Xem lịch làm việc của kỹ thuật viên (có thể lọc theo ID, thời gian, trạng thái).
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead hoặc Manager.  
        ///  
        /// Trả về danh sách lịch làm việc theo khoảng thời gian, nhóm theo ngày và ca làm việc.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ
        /// </remarks>
        //[Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}")]
        [HttpGet("technician-schedule")]
        public async Task<ActionResult> GetTechnicianSchedule(
            [FromQuery] int? technicianId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] WorkSlotStatus? status)
        {
            var result = await _workSlotService.GetTechnicianScheduleAsync(technicianId, fromDate, toDate, status);
            return Ok(result);
        }

        /// <summary>
        /// Xem lịch làm việc cá nhân của kỹ thuật viên đang đăng nhập.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Technician.  
        ///  
        /// Trả về danh sách lịch làm việc của chính người dùng đăng nhập trong khoảng thời gian xác định.  
        ///  
        /// **Enum WorkSlotStatus:**  
        /// - `NotStarted` — Chưa làm  
        /// - `Completed` — Đã làm  
        /// - `Off` — Nghỉ
        /// </remarks>
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [HttpGet("my-schedule")]
        public async Task<ActionResult> GetMySchedule(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] WorkSlotStatus? status)
        {
            var result = await _workSlotService.GetMyScheduleAsync(fromDate, toDate, status);
            return Ok(result);
        }
    }
}
