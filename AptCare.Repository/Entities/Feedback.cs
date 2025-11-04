using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int RepairRequestId { get; set; }
        public int UserId { get; set; }
        public int? ParentFeedbackId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(ParentFeedbackId))]
        public Feedback? ParentFeedback { get; set; }
    }
}
