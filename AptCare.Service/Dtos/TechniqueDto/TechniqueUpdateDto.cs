using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.TechniqueDto
{

    public class TechniqueUpdateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
    }
}
