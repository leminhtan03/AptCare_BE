using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class InvoiceService
    {
        [Key]
        public int InvoiceServiceId { get; set; }

        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
    
        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
