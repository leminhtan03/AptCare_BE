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
        public SlotTime Slot { get; set; }
        public WorkSlotStatus Status { get; set; }
        [Required]
        [ForeignKey("Technician")]
        public int TechnicianId { get; set; }

        public User Technician { get; set; }
        public ICollection<WorkSlotStatusTracking>? WorkSlotStatusTrackings { get; set; }
    }
}
