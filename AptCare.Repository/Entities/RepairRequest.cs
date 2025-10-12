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

        public int UserId { get; set; }
        public int? ApartmentId { get; set; }
        public int? CommonAreaId { get; set; }
        public int? ParentRequestId { get; set; }
        public int? TechniqueId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public bool IsEmergency { get; set; }
        public string Object { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptanceTime { get; set; }
        public string Status { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [ForeignKey(nameof(ApartmentId))]
        public Apartment Apartment { get; set; }

        [ForeignKey(nameof(CommonAreaId))]
        public CommonArea CommonArea { get; set; }

        [ForeignKey(nameof(ParentRequestId))]
        public RepairRequest ParentRequest { get; set; }

        [ForeignKey(nameof(TechniqueId))]
        public Technique Technique { get; set; }

        [ForeignKey(nameof(MaintenanceRequestId))]
        public MaintenanceRequest MaintenanceRequest { get; set; }

        public ICollection<RepairReport> RepairReports { get; set; }
        public ICollection<RequestTracking> RequestTrackings { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
    }
}
