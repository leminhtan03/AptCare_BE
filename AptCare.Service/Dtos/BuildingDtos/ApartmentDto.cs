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
        public string RoomNumber { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Floor { get; set; }
    }
}
