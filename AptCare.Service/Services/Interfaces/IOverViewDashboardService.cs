
using AptCare.Service.Dtos.DashboardDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IOverViewDashboardService
    {
        // TechLead Dashboard
        Task<TechLeadKpiDto> GetTechLeadKpiAsync();
        Task<MonthlyRequestChartDto> GetTechLeadMonthlyRequestsAsync(int year);
        Task<IssueBreakdownDto> GetTechLeadIssueBreakdownAsync(int month, int year);
        Task<YearlyMaintenanceChartDto> GetTechLeadYearlyMaintenanceAsync();
        Task<MaintenanceHealthDto> GetTechLeadMaintenanceHealthAsync(int year);

        // Manager Dashboard
        Task<ManagerKpiDto> GetManagerKpiAsync();
        Task<MonthlyRevenueChartDto> GetManagerMonthlyRevenueAsync(int year);
        Task<TopAccessoriesDto> GetManagerTopAccessoriesAsync(DateTime? fromDate, DateTime? toDate);

        // Admin Dashboard
        Task<AdminKpiDto> GetAdminKpiAsync();
        Task<AppointmentVolumeChartDto> GetAdminMonthlyAppointmentsAsync(int month, int year);
        Task<IssueBreakdownDto> GetAdminIssueBreakdownAsync(int month, int year);
        Task<MonthlyRevenueChartDto> GetAdminMonthlyRevenueAsync(int year);
        Task<TopAccessoriesDto> GetAdminTopAccessoriesAsync(DateTime? fromDate, DateTime? toDate);
        Task<MonthlyMaintenanceChartDto> GetAdminMonthlyMaintenanceAsync(int year);
        Task<MaintenanceHealthDto> GetAdminMaintenanceHealthAsync(int month, int year);
    }
}
