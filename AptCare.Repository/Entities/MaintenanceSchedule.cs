using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using AptCare.Repository.Enum;

namespace AptCare.Repository.Entities
{
    public class MaintenanceSchedule
    {
        [Key]
        public int MaintenanceScheduleId { get; set; }

        [ForeignKey("CommonAreaObject")]
        public int CommonAreaObjectId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string? Description { get; set; }
        public int FrequencyInDays { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime NextScheduledDate { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? LastMaintenanceDate { get; set; }
        public string TimePreference { get; set; }
        public int RequiredTechnicians { get; set; } = 1;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
        public CommonAreaObject CommonAreaObject { get; set; } = null!;
        public ICollection<MaintenanceTrackingHistory>? MaintenanceTrackingHistories { get; set; }
        public ICollection<RepairRequest>? GeneratedRepairRequests { get; set; }
    }
}
