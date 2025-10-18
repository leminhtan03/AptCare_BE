using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.TechniqueDto
{
    public class TechniqueCreateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
