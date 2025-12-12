namespace AptCare.Service.Dtos.DashboardDtos
{
    // KPI Card
    public class TechLeadKpiDto
    {
        public int TotalToday { get; set; }
        public int PendingToday { get; set; }
        public int InProgressToday { get; set; }
        public int CompletedToday { get; set; }
        public int CancelledToday { get; set; }
        public int EmergencyToday { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    }

    // Chart 1: Monthly Requests
    public class MonthlyRequestChartDto
    {
        public List<MonthlyRequestData> Data { get; set; } = new();
    }

    public class MonthlyRequestData
    {
        public int Month { get; set; }
        public int TotalRequests { get; set; }
        public int CompletedAccepted { get; set; }
    }

    // Chart 2: Issue Breakdown
    public class IssueBreakdownDto
    {
        public List<IssueBreakdownData> Data { get; set; } = new();
    }

    public class IssueBreakdownData
    {
        public string IssueType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    // Chart 3: Yearly Maintenance
    public class YearlyMaintenanceChartDto
    {
        public List<YearlyMaintenanceData> Data { get; set; } = new();
    }

    public class YearlyMaintenanceData
    {
        public int Year { get; set; }
        public int TotalMaintainedObjects { get; set; }
        public int OkObjects { get; set; }
        public int NeedFixOrReplace { get; set; }
        public int FailedMaintenance { get; set; }
    }

    // Chart 4: Maintenance Health (Donut)
    public class MaintenanceHealthDto
    {
        public decimal OkPercentage { get; set; }
        public decimal NeedRepairPercentage { get; set; }
        public decimal FailedPercentage { get; set; }
        public int TotalObjects { get; set; }
    }
}