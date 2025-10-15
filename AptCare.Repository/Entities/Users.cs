
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System.ComponentModel.DataAnnotations;
namespace AptCare.Repository.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(256)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(256)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(20)]
        [StringLength(20)]
        // [Index(IsUnique = true)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        // [Index(IsUnique = true)]
        public string Email { get; set; }

        [MaxLength(50)]
        public string CitizenshipIdentity { get; set; }

        public DateTime? Birthday { get; set; }

        public ActiveStatus Status { get; set; }

        public Account? Account { get; set; }

        public ICollection<UserApartment>? UserApartments { get; set; }
        public ICollection<Report>? Reports { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
        public ICollection<TechnicianTechnique>? TechnicianTechniques { get; set; }
        public ICollection<WorkSlot>? WorkSlots { get; set; }
        public ICollection<Message>? Messages { get; set; }
        public ICollection<ConversationParticipant>? ConversationParticipants { get; set; }
        public ICollection<RepairRequest>? RepairRequests { get; set; }
        public ICollection<AppointmentAssign>? AppointmentAssigns { get; set; }

    }
}
