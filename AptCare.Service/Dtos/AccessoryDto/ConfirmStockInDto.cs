using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class ConfirmStockInDto
    {
        public int StockTransactionId { get; set; }
        public bool IsConfirm { get; set; }
        public IFormFile? VerificationFile { get; set; }
        public string? Note { get; set; }
    }
}
