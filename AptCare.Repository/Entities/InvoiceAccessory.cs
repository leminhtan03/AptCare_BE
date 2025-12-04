using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class InvoiceAccessory
    {
        [Key]
        public int InvoiceAccessoryId { get; set; }

        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        [ForeignKey("Accessory")]

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
