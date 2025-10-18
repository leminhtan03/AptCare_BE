using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.IssueDto
{
    public sealed class IssueUpdateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int TechniqueId { get; set; }
        public bool IsEmergency { get; set; }
        public int RequiredTechnician { get; set; }
        public int EstimatedDuration { get; set; }
        public string Status { get; set; } = "Active";
    }
}
