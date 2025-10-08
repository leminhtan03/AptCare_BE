using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.WorkSlotDtos
{
    public class WorkSlotCreateFromDateToDateDto
    {
        [Required]
        public int TechnicianId { get; set; }

        [Required]
        public DateOnly FromDate { get; set; }

        [Required]
        public DateOnly ToDate { get; set; }

        [Required]
        public SlotTime Slot { get; set; }
    }

    public class WorkSlotCreateDateSlotDto
    {
        [Required]
        public int TechnicianId { get; set; }

        [Required]
        public List<DateSlotCreateDto> DateSlots { get; set; } = null!;
    }

    public class DateSlotCreateDto
    {        
        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public SlotTime Slot { get; set; }
    }
}
