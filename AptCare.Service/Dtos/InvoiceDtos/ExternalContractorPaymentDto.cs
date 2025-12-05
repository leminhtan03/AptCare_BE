using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.InvoiceDtos
{
    public class ExternalContractorPaymentConfirmDto
    {
        [Required]
        public int InvoiceId { get; set; }
        public string? Note { get; set; }
        public IFormFile? PaymentReceipt { get; set; }
    }
}