using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InvoiceDtos
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int RepairRequestId { get; set; }
        public bool IsChargeable { get; set; }
        public decimal TotalAmount { get; set; }
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public List<InvoiceAccessoryDto>? Accessories { get; set; }
        public List<InvoiceServiceDto>? Services { get; set; }

        public List<MediaDto>? Medias { get; set; }
    }

    public class InvoiceAccessoryDto
    {
        public int InvoiceAccessoryId { get; set; }
        public int? AccessoryId { get; set; }
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public InvoiceAccessorySourceType SourceType { get; set; }

    }

    public class InvoiceServiceDto
    {
        public int InvoiceServiceId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
