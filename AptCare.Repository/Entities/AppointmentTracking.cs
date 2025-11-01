using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class AppointmentTracking
    {
        [Key]
        public int TrackingId { get; set; }
        public int AppointmentId { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Note { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        [ForeignKey(nameof(UpdatedBy))]
        public User UpdatedByUser { get; set; } = null!;
    }
}
