using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairRequestTaskDtos
{
    public class RepairRequestTaskStatusUpdateDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        public TaskCompletionStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú kỹ thuật viên không được vượt quá 1000 ký tự.")]
        public string? TechnicianNote { get; set; }

        [Required]
        [MaxLength(100, ErrorMessage = "Kết quả kiểm tra không được vượt quá 100 ký tự.")]
        public string InspectionResult { get; set; } = null!;
    }
}
