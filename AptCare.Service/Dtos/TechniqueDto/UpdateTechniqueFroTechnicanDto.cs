using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.TechniqueDto
{
    public class UpdateTechniqueFroTechnicanDto
    {
        public int TechnicianId { get; set; }
        public List<int> TechniqueIds { get; set; } = new List<int>();
    }
}
