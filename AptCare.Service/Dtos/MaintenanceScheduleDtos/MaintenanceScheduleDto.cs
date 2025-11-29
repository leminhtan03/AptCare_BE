
using AptCare.Service.Dtos.CommonAreaObjectDtos;
using AptCare.Service.Dtos.TechniqueDto;

namespace AptCare.Service.Dtos.MaintenanceScheduleDtos
{
    public class MaintenanceScheduleDto
    {
        public int MaintenanceScheduleId { get; set; }
        public int CommonAreaObjectId { get; set; }
        public string Description { get; set; } = null!;
        public int FrequencyInDays { get; set; }
        public DateOnly NextScheduledDate { get; set; }
        public DateOnly? LastMaintenanceDate { get; set; }
        public TimeSpan TimePreference { get; set; }
        public int? RequiredTechniqueId { get; set; }
        public int RequiredTechnicians { get; set; }
        public double EstimatedDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public CommonAreaObjectBasicDto? CommonAreaObject { get; set; }
        public TechniqueResponseDto? RequiredTechnique { get; set; }
    }






}