using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.CommonAreaObjectTypeDtos
{
    public class CommonAreaObjectTypeDto
    {
        public int CommonAreaObjectTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public string? Description { get; set; }
        public ActiveStatus Status { get; set; }
    }
}
