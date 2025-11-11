using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int InvoiceId { get; set; }

        public TransactionDirection Direction { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public PaymentProvider Provider { get; set; }

        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;

        // PayOS
        public long? PayOSOrderCode { get; set; }
        public string? PayOSCheckoutUrl { get; set; }
        public string? PayOSTransactionId { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;
    }
}
