using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.IssueDto
{
    public class IssueListItemDto
    {
        public int IssueId { get; set; }
        public int TechniqueId { get; set; }
        public string TechniqueName { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsEmergency { get; set; }
        public int RequiredTechnician { get; set; }
        public double EstimatedDuration { get; set; }
        public string Status { get; set; } = "Active";
    }
}
