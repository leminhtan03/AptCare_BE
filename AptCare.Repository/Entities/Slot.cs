using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Slot
    {
        [Key]
        public int SlotId { get; set; }

        [Required]
        public TimeSpan FromTime { get; set; }
        [Required]
        public TimeSpan ToTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public ActiveStatus Status { get; set; }

        public ICollection<WorkSlot>? WorkSlots { get; set; }
    }
}
