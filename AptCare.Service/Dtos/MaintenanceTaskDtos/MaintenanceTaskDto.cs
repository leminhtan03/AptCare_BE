using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.MaintenanceTaskDtos
{
    public class MaintenanceTaskDto
    {
        public int MaintenanceTaskId { get; set; }
        public int CommonAreaObjectTypeId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public string? RequiredTools { get; set; }
        public int DisplayOrder { get; set; }
        public double EstimatedDurationMinutes { get; set; }
        public string Status { get; set; } = null!;
        public CommonAreaObjectTypeDto? CommonAreaObjectType { get; set; }
    }
}
