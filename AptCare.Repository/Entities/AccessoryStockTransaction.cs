using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class AccessoryStockTransaction
    {
        [Key]
        public int StockTransactionId { get; set; }

        [ForeignKey("Accessory")]
        public int AccessoryId { get; set; }
        public Accessory Accessory { get; set; } = null!;

        public int Quantity { get; set; }
        public StockTransactionType Type { get; set; }
        public StockTransactionStatus Status { get; set; }
        public string? Note { get; set; }
        public int CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User CreatedByUser { get; set; } = null!;
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public int? ApprovedBy { get; set; }
        [ForeignKey("ApprovedBy")]
        public User? ApprovedByUser { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ApprovedAt { get; set; }

        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }

        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }
}
