using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.UserDtos;

namespace AptCare.Service.Dtos.WorkSlotDtos
{
    public class WorkSlotDto
    {
        public DateOnly Date { get; set; }
        public List<SlotDto> Slots { get; set; } = null!;       
    }

    public class SlotDto
    {
        public string Slot { get; set; } = null!;
        public List<TechnicianWorkSlotDto> TechnicianWorkSlots { get; set; } = null!;
    }

    public class TechnicianWorkSlotDto
    {
        public int WorkSlotId { get; set; }
        public string Status { get; set; } = null!;
        public UserDto Technician { get; set; } = null!;
    }
}
