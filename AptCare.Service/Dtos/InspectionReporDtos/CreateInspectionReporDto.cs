using AptCare.Repository.Enum;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.InspectionReporDtos
{
    public class CreateInspectionReporDto
    {
        [Required]
        public int AppointmentId { get; set; }
        [Required]
        public FaultType FaultOwner { get; set; }
        [Required]
        public SolutionType SolutionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;

    }
}
