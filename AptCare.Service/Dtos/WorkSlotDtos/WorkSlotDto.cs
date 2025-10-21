using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Dtos.AppointmentDtos;

namespace AptCare.Service.Dtos.WorkSlotDtos
{
    public class WorkSlotDto
    {
        public DateOnly Date { get; set; }
        public List<SlotWorkDto> Slots { get; set; } = null!;       
    }

    public class SlotWorkDto
    {
        public int SlotId { get; set; }
        public List<TechnicianWorkSlotDto> TechnicianWorkSlots { get; set; } = null!;
    }

    public class TechnicianWorkSlotDto
    {
        public int WorkSlotId { get; set; }
        public string Status { get; set; } = null!;
        public UserBasicDto Technician { get; set; } = null!;
        public List<AppointmentDto>? Appointments { get; set; }
    }
}
