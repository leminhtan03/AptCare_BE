namespace AptCare.Service.Dtos.DashboardDtos
{
    // KPI Card
    public class ManagerKpiDto
    {
        public int CompletedThisMonth { get; set; }
        public int InProgressTotal { get; set; }
        public int WaitingManagerApproval { get; set; }
    }

    // Chart 1: Monthly Revenue
    public class MonthlyRevenueChartDto
    {
        public List<MonthlyRevenueData> Data { get; set; } = new();
    }

    public class MonthlyRevenueData
    {
        public int Month { get; set; }
        public decimal IncomeAmount { get; set; }
        public decimal ExpenseAmount { get; set; }
    }

    // Chart 2: Top Accessories
    public class TopAccessoriesDto
    {
        public List<AccessoryUsageData> Data { get; set; } = new();
    }

    public class AccessoryUsageData
    {
        public string AccessoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public int UsageCount { get; set; }
    }
}