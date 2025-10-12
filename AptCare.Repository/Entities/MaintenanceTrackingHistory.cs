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
        public string Field { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(MaintenanceRequestId))]
        public MaintenanceRequest MaintenanceRequest { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
