using AptCare.Repository.Enum;
using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.AccessoryDto
{
    public class AccessoryUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Descrption { get; set; }
        public decimal Price { get; set; }
        public ActiveStatus Status { get; set; }
        public List<IFormFile>? NewImages { get; set; }
        public List<int>? RemoveMediaIds { get; set; }
    }
}
