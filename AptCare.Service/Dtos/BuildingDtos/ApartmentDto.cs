using AptCare.Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentDto
    {
        public int ApartmentId { get; set; }
        public int FloorId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Floor { get; set; } = null!;
    }
}
