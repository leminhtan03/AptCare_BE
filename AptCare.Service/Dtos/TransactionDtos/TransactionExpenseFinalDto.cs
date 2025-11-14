using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.TransactionDtos
{
    public class TransactionExpenseFinalDto
    {
        public int InvoiceId { get; set; }
        public string? Note { get; set; }
        public IFormFile ContractorInvoiceFile { get; set; } = null!;
    }
}
