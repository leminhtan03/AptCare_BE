using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        public int RequestId { get; set; }
        public int ReportId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Note { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(RequestId))]
        public MaintenanceRequest MaintenanceRequest { get; set; }

        [ForeignKey(nameof(ReportId))]
        public Report Report { get; set; }

        public ICollection<AppointmentAssign> AppointmentAssigns { get; set; }
        public ICollection<InspectionReport> InspectionReports { get; set; }
    }
}
