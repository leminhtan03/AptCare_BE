using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class ReportApproval
    {
        [Key]
        public int ReportApprovalId { get; set; }
        public int? InspectionReportId { get; set; }
        public int? RepairReportId { get; set; }
        public int UserId { get; set; }
        public AccountRole Role { get; set; }
        public ReportStatus Status { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(InspectionReportId))]
        public InspectionReport? InspectionReport { get; set; }

        [ForeignKey(nameof(RepairReportId))]
        public RepairReport? RepairReport { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
