using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }
        [ForeignKey("RepairRequest")]
        public int RepairRequestId { get; set; }
        public RepairRequest RepairRequest { get; set; } = null!;
        public bool IsChargeable { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceType Type { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public InvoiceStatus Status { get; set; }

        public ICollection<InvoiceAccessory>? InvoiceAccessories { get; set; }

        public ICollection<InvoiceService>? InvoiceServices { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }
}
