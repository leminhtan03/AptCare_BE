using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
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

        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public PaymentProvider Provider { get; set; }

        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;

        // PayOS
        public long? OrderCode { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? PayOSTransactionId { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? PaidAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;
    }
}
