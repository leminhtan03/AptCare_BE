using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.TransactionDtos
{
    public class TransactionExpenseDepositDto
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public IFormFile ContractorInvoiceFile { get; set; } = null!;
    }
}
