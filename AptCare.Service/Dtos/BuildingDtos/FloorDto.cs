using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class FloorDto
    {
        public int FloorId { get; set; }
        public string BuildingCode { get; set; } = null!;
        public int FloorNumber { get; set; } 
        public string Status { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<ApartmentDto>? Apartments { get; set; }
        public List<CommonAreaDto>? CommonAreas { get; set; }
    }
}
