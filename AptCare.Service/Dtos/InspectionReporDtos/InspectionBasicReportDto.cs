using AptCare.Repository.Enum;
using AptCare.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public string Description { get; set; } = null!;
        public string Solution { get; set; } = null!;
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AreaName { get; set; }
        public TechnicanDto Technican { get; set; }
        public List<MediaDto>? Medias { get; set; }


    }
}
