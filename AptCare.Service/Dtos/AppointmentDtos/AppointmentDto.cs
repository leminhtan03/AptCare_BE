using AptCare.Repository.Enum;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AppointmentDtos
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int RepairRequestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Note { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<UserBasicDto> Technicians { get; set; } = null!;
    }

    public class AppointmentScheduleDto
    {
        public DateOnly Date { get; set; }
        public List<AppointmentDto> Appointments { get; set; } = null!;
    }
}
