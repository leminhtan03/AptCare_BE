using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class WorkSlot
    {
        [Key]
        public int WorkSlotId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        [ForeignKey("Slot")]
        public int SlotId { get; set; }
        public Slot Slot { get; set; } = null!;

        public WorkSlotStatus Status { get; set; }

        [Required]
        [ForeignKey("Technician")]
        public int TechnicianId { get; set; }
        public User Technician { get; set; } = null!;

        public ICollection<WorkSlotStatusTracking>? WorkSlotStatusTrackings { get; set; }
    }
}
