namespace AptCare.Service.Dtos.MaintenanceScheduleDtos
{
    public class MaintenanceTrackingHistoryDto
    {
        public int MaintenanceTrackingHistoryId { get; set; }
        public string Field { get; set; } = null!;
        public string OldValue { get; set; } = null!;
        public string NewValue { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedByUserName { get; set; } = null!;
    }
}
