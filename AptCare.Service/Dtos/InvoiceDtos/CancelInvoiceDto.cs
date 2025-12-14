using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.InvoiceDtos
{
    public class CancelInvoiceDto
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;
    }
}