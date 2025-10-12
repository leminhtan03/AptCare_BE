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

        public int RepairRequestId { get; set; }
        public bool IsChargeable { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; }

        public ICollection<InvoiceItem> InvoiceItems { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}
