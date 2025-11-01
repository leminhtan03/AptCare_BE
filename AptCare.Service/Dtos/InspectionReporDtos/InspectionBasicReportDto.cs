using AptCare.Repository.Enum;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InspectionReporDtos
{
    public class InspectionBasicReportDto
    {
        public int InspectionReportId { get; set; }
        public FaultType FaultOwner { get; set; }
        public SolutionType SolutionType { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AreaName { get; set; }
        public TechnicanDto Technican { get; set; }

        public List<MediaDto>? Medias { get; set; }

    }
}
