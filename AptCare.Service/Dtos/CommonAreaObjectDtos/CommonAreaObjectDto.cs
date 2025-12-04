using AptCare.Repository.Enum;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.CommonAreaObjectDtos
{
    public class CommonAreaObjectDto
    {
        public int CommonAreaObjectId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ActiveStatus Status { get; set; }
        public CommonAreaDto CommonArea { get; set; } = null!; 
        public CommonAreaObjectTypeDto CommonAreaObjectType { get; set; } = null!;
    }
}
