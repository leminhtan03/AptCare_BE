using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class AccessoryCreateDto
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Descrption { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Quantity { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
