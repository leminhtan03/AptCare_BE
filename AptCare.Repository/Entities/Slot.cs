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
        public string FromTime { get; set; } = null!;
        [Required]
        public string ToTime { get; set; } = null!;
        public DateTime LastUpdated { get; set; }
        public ActiveStatus Status { get; set; }

        public ICollection<WorkSlot>? WorkSlots { get; set; }
    }
}
