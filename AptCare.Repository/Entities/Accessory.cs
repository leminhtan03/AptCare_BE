using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Repository.Entities
{
    public class Accessory
    {
        [Key]
        public int AccessoryId { get; set; }

        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public ActiveStatus Status { get; set; }

        public ICollection<InvoiceItem> InvoiceItems { get; set; }
    }
}
