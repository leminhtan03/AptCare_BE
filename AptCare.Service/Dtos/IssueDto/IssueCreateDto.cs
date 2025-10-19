using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.IssueDto
{
    public class IssueCreateDto
    {
        public int TechniqueId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsEmergency { get; set; }
        public int RequiredTechnician { get; set; } = 1;
        public int EstimatedDuration { get; set; } = 1; // giờ
    }

}
