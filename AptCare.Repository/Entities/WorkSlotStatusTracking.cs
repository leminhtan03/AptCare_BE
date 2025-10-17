
using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class WorkSlotStatusTracking
    {
        [Key]
        public int WorkSlotStatusTrackingId { get; set; }
        [Required]
        [ForeignKey("WorkSlot")]
        public int WorkSlotId { get; set; }
        public WorkSlot WorkSlot { get; set; } = null!;
        public DateTime StatusChangeTime { get; set; }
        public WorkSlotStatus PreviousStatus { get; set; }
        public WorkSlotStatus NewStatus { get; set; }
    }
}
