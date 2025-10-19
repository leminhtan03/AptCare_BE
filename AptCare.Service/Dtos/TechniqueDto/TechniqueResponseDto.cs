using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.TechniqueDto
{
    public class TechniqueResponseDto
    {
        public int TechniqueId { get; set; }
        public string TechniqueName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
