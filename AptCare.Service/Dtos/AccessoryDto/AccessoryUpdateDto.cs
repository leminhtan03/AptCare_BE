using AptCare.Repository.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class AccessoryUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Descrption { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public ActiveStatus Status { get; set; }
        public List<IFormFile>? NewImages { get; set; }
        public List<int>? RemoveMediaIds { get; set; }
    }
}
