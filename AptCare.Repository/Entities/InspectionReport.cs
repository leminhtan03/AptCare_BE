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
        public string Description { get; set; }
        public string Solution { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
