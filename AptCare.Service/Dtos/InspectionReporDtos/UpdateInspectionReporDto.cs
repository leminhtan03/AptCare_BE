using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.InspectionReporDtos
{
    public class UpdateInspectionReporDto
    {
        public FaultType FaultOwner { get; set; }
        public SolutionType SolutionType { get; set; }
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        public string Solution { get; set; } = null!;
    }
}
