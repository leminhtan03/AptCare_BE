using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class InvoiceItem
    {
        [Key]
        public int InvoiceItemId { get; set; }

        public int InvoiceId { get; set; }

        public int AccessoryId { get; set; }
        public string ItemName { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; }


        [ForeignKey(nameof(AccessoryId))]
        public Accessory Accessory { get; set; }
    }
}
