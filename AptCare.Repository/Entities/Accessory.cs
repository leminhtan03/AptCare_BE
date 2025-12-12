using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Repository.Entities
{
    public class Accessory
    {
        [Key]
        public int AccessoryId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Descrption { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public ActiveStatus Status { get; set; }

        public ICollection<InvoiceAccessory>? InvoiceAccessories { get; set; }
        public ICollection<AccessoryStockTransaction>? StockTransactions { get; set; }
    }
}
