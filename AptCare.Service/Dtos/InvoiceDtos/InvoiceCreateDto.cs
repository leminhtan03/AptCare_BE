using AptCare.Repository.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InvoiceDtos
{
    public class InvoiceInternalCreateDto
    {
        public int RepairRequestId { get; set; }
        public bool IsChargeable { get; set; }
        public List<InvoiceAccessoryInternalCreateDto>? AvailableAccessories { get; set; }
        public List<InvoiceAccessoryPurchaseCreateDto>? AccessoriesToPurchase { get; set; }

        public List<ServiceCreateDto>? Services { get; set; }
    }

    public class InvoiceExternalCreateDto
    {
        public int RepairRequestId { get; set; }
        public bool IsChargeable { get; set; }
        public List<InvoiceAccessoryExternalCreateDto>? Accessories { get; set; }
        public List<ServiceCreateDto>? Services { get; set; }
    }
    public class InvoiceAccessoryInternalCreateDto
    {
        public int AccessoryId { get; set; }
        public int Quantity { get; set; }
    }
    public class InvoiceAccessoryPurchaseCreateDto
    {
        public int? AccessoryId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public int Quantity { get; set; }

        [Required]
        public decimal PurchasePrice { get; set; }
    }

    public class InvoiceAccessoryExternalCreateDto
    {
        [Required]
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ServiceCreateDto
    {
        [Required]
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
