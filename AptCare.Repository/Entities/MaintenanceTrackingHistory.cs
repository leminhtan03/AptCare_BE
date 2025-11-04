using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class MaintenanceTrackingHistory
    {
        [Key]
        public int MaintenanceTrackingHistoryId { get; set; }

        public int MaintenanceRequestId { get; set; }
        public int UserId { get; set; }
        [Required]
        public string Field { get; set; } = null!;
        [Required]
        public string OldValue { get; set; } = null!;
        [Required]
        public string NewValue { get; set; } = null!;
        [Column(TypeName = "timestamp without time zone")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(MaintenanceRequestId))]
        public MaintenanceRequest MaintenanceRequest { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
