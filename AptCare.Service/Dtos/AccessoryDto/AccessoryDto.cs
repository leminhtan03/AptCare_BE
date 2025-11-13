using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class AccessoryDto
    {
        public int AccessoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Descrption { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = null!;
    }
}
