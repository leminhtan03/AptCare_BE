using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class RepairRequest
    {
        [Key]
        public int RepairRequestId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [ForeignKey("Apartment")]
        public int? ApartmentId { get; set; }
        public Apartment? Apartment { get; set; }

        [ForeignKey("ParentRequest")]
        public int? ParentRequestId { get; set; }
        public RepairRequest? ParentRequest { get; set; }

        [ForeignKey("Issue")]
        public int? IssueId { get; set; }
        public Issue? Issue { get; set; }

        [ForeignKey("MaintenanceRequest")]
        public int? MaintenanceRequestId { get; set; }
        public MaintenanceRequest? MaintenanceRequest { get; set; }

        [Required]
        [MaxLength(100)]
        public string Object { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;
        public bool IsEmergency { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public DateOnly? AcceptanceTime { get; set; }

        public ICollection<RepairRequest>? ChildRequests { get; set; }
        public ICollection<Appointment>? Appointments { get; set; }
        public ICollection<RequestTracking>? RequestTrackings { get; set; }
        public ICollection<Feedback>? Feedbacks { get; set; }
        public ICollection<Invoice>? Invoices { get; set; }
        public ICollection<Contract>? Contracts { get; set; }
    }
}
