
using AptCare.Repository.Enum;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class AccessoryStockTransactionDto
    {
        public int StockTransactionId { get; set; }
        public int AccessoryId { get; set; }
        public string? AccessoryName { get; set; }
        public int Quantity { get; set; }
        public StockTransactionType Type { get; set; }
        public StockTransactionStatus Status { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Note { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? TransactionId { get; set; }
        public int? InvoiceId { get; set; }
        public ICollection<MediaDto>? medias { get; set; }
    }
}