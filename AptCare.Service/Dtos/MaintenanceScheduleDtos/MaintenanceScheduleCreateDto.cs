
namespace AptCare.Service.Dtos.MaintenanceScheduleDtos
{
    public class MaintenanceScheduleCreateDto
    {
        public int CommonAreaObjectId { get; set; }
        public string Description { get; set; } = null!;
        public int FrequencyInDays { get; set; }
        public DateOnly NextScheduledDate { get; set; }
        public TimeSpan TimePreference { get; set; }
        public int? RequiredTechniqueId { get; set; }
        public int RequiredTechnicians { get; set; } = 1;
        public double EstimatedDuration { get; set; }
    }
}
