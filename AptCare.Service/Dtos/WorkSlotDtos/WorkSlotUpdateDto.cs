using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.WorkSlotDtos
{
    public class WorkSlotUpdateDto
    {
        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int SlotId { get; set; }

        [Required]
        public WorkSlotStatus Status { get; set; }

        [Required]
        public int TechnicianId { get; set; }
    }
}
