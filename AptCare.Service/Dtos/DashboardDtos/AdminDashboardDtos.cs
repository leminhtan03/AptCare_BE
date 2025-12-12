namespace AptCare.Service.Dtos.DashboardDtos
{
    // KPI Card
    public class AdminKpiDto
    {
        // Repair Requests
        public Dictionary<string, int> RequestsByStatus { get; set; } = new();
        public int EmergencyToday { get; set; }

        // Appointments
        public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();

        // Financial
        public decimal TotalAwaitingPaymentAmount { get; set; }
        public decimal TotalFailedAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public Dictionary<string, int> InvoicesByStatus { get; set; } = new();
    }

    // Chart 1: Appointment Volume
    public class AppointmentVolumeChartDto
    {
        public List<AppointmentVolumeData> Data { get; set; } = new();
    }

    public class AppointmentVolumeData
    {
        public int Day { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
    }

    // Chart 5: Monthly Maintenance
    public class MonthlyMaintenanceChartDto
    {
        public List<MonthlyMaintenanceData> Data { get; set; } = new();
    }

    public class MonthlyMaintenanceData
    {
        public int Month { get; set; }
        public int TotalMaintainedObjects { get; set; }
        public int OkObjects { get; set; }
        public int NeedFixOrReplace { get; set; }
        public int FailedMaintenance { get; set; }
    }
}