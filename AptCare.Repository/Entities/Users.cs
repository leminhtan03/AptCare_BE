
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AptCare.Repository.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(256)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        [StringLength(20)]
        // [Index(IsUnique = true)]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [MaxLength(50)]
        public string? CitizenshipIdentity { get; set; }

        [Column(TypeName = "timestamp without time zone")]
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
        public ICollection<AppointmentTracking>? AppointmentTrackings { get; set; }
        public ICollection<RequestTracking>? RequestTrackings { get; set; }
    }
}
