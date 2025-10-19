using AptCare.Repository.Enum;
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

        public int RepairRequestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Note { get; set; } = null!;
        public AppointmentStatus Status { get; set; } 
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; } = null!;

        public RepairReport RepairReport { get; set; } = null!;

        public ICollection<AppointmentAssign>? AppointmentAssigns { get; set; }
        public ICollection<InspectionReport>? InspectionReports { get; set; }
    }
}
