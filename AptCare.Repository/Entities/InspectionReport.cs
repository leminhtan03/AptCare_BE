using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class InspectionReport
    {
        [Key]
        public int InspectionReportId { get; set; }
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public FaultType FaultOwner { get; set; }
        public SolutionType SolutionType { get; set; }
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        public string Solution { get; set; } = null!;
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
