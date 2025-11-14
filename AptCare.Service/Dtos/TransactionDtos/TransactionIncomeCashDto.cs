using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.TransactionDtos
{
    public class TransactionIncomeCashDto
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public IFormFile? ReceiptFile { get; set; }
    }
}
