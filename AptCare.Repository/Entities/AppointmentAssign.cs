using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class AppointmentAssign
    {
        [Key]
        public int AppointmentAssignId { get; set; }

        [ForeignKey("Technician")]
        public int TechnicianId { get; set; }
        public User Technician { get; set; } = null!;

        [ForeignKey("Appointment")]
        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;
        [Column(TypeName = "timestamp without time zone")]
        public DateTime AssignedAt { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime EstimatedStartTime { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime EstimatedEndTime { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ActualStartTime { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ActualEndTime { get; set; }
        public WorkOrderStatus Status { get; set; }
    }
}
