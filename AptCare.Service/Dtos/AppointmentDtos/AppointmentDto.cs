using AptCare.Repository.Enum;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
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
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Note { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public RepairRequestBasicDto RepairRequest { get; set; } = null!;
        public List<UserBasicDto> Technicians { get; set; } = null!;
    }

    public class ResidentAppointmentScheduleDto
    {
        public DateOnly Date { get; set; }
        public List<AppointmentDto> Appointments { get; set; } = null!;
    }

    public class TechnicianAppointmentScheduleDto
    {
        public DateOnly Date { get; set; }

        public List<SlotAppointmentDto> Slots { get; set; } = null!;
    }
    public class SlotAppointmentDto
    {
        public int SlotId { get; set; }
        public List<AppointmentDto> Appointments { get; set; } = null!;
    }
}
