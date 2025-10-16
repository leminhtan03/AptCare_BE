using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class RequestTracking
    {
        [Key]
        public int RequestTrackingId { get; set; }

        public int RepairRequestId { get; set; }
        public RequestStatus Status { get; set; }
        public string? Note { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; } = null!;

        [ForeignKey(nameof(UpdatedBy))]
        public User UpdatedByUser { get; set; } = null!;
    }
}
