using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class StockOutAccessoryDto
    {
        public int Quantity { get; set; }
        public int? RepairRequestId { get; set; }
        public int? InvoiceId { get; set; }
        public string? Note { get; set; }
    }
}
