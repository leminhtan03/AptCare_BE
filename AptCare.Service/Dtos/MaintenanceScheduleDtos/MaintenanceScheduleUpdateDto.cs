
namespace AptCare.Service.Dtos.MaintenanceScheduleDtos
{
    public class MaintenanceScheduleUpdateDto
    {
        public string? Description { get; set; }
        public int? FrequencyInDays { get; set; }
        public DateOnly? NextScheduledDate { get; set; }
        public TimeSpan? TimePreference { get; set; }
        public int? RequiredTechniqueId { get; set; }
        public int? RequiredTechnicians { get; set; }
        public double? EstimatedDuration { get; set; }
    }
}
