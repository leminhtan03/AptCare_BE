using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AppointmentAssignDtos
{
    public class AppointmentAssignUpdateDto
    {
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public WorkOrderStatus Status { get; set; }
    }
}
