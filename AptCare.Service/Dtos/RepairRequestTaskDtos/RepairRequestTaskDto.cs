using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairRequestTaskDtos
{
    public class RepairRequestTaskDto
    {
        public int RepairRequestTaskId { get; set; }
        public int RepairRequestId { get; set; }
        public int? MaintenanceTaskId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public string Status { get; set; } = null!;
        public string? TechnicianNote { get; set; }
        public string? InspectionResult { get; set; }
        public DateTime? CompletedAt { get; set; }
        public UserBasicDto? CompletedBy { get; set; }
    }
}
