using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class RepairReport
    {
        [Key]
        public int RepairReportId { get; set; }

        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        [Required]
        public string Description { get; set; } = null!;
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<ReportApproval>? ReportApprovals { get; set; }
    }
}
