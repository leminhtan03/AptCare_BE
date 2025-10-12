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

        public int RepairRequestId { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public ICollection<ReportApproval> ReportApprovals { get; set; }
    }
}
