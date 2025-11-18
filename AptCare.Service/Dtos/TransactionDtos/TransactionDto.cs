using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Service.Dtos;
using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.TransactionDtos
{
    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = null!;
        public int InvoiceId { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public PaymentProvider Provider { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public long? PayOSOrderCode { get; set; }
        public string? PayOSCheckoutUrl { get; set; }
        public string? PayOSTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public MediaDto? AttachedFile { get; set; }
    }
}