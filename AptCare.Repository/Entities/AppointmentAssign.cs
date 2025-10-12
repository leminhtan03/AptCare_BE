using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class AppointmentAssign
    {
        [Key]
        public int AppointmentAssignId { get; set; }

        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public DateTime AssignedAt { get; set; }


        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
