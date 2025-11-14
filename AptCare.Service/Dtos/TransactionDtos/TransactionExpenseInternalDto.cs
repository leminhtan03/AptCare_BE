using Microsoft.AspNetCore.Http;
namespace AptCare.Service.Dtos.TransactionDtos
{
    public class TransactionExpenseInternalDto
    {
        public int InvoiceId { get; set; }
        public string? Note { get; set; }
        public IFormFile? ProofFile { get; set; }
    }
}
