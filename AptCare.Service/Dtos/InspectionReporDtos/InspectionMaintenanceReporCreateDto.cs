using AptCare.Repository.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InspectionReporDtos
{
    public class InspectionMaintenanceReporCreateDto
    {
        [Required]
        public int AppointmentId { get; set; }
        [Required]
        public SolutionType SolutionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public List<IFormFile>? Files { get; set; }
        public List<RequestTaskStatusUpdateDto> UpdatedTasks { get; set; } = null!;
    }

    public class RequestTaskStatusUpdateDto
    {
        public int RepairRequestTaskId { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        public TaskCompletionStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú kỹ thuật viên không được vượt quá 1000 ký tự.")]
        public string? TechnicianNote { get; set; }

        [Required]
        [MaxLength(100, ErrorMessage = "Kết quả kiểm tra không được vượt quá 100 ký tự.")]
        public string InspectionResult { get; set; } = null!;
    }
}
