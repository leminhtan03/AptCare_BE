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
    public class CommonAreaDto
    {
        public int CommonAreaId { get; set; }
        public int? FloorId { get; set; }
        public string? Floor { get; set; }
        public string AreaCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = null!;
    }
}
