using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.DashboardDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class OverViewDashboardService : BaseService<OverViewDashboardService>, IOverViewDashboardService
    {
        public OverViewDashboardService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork, 
            ILogger<OverViewDashboardService> logger, 
            IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        #region TechLead Dashboard

        public async Task<TechLeadKpiDto> GetTechLeadKpiAsync()
        {
            var today = DateTime.Today;
            var requestRepo = _unitOfWork.GetRepository<RepairRequest>();

            var todayRequests = await requestRepo.GetListAsync(
                predicate: r => r.CreatedAt.Date == today,
                include: i => i.Include(r => r.RequestTrackings)
            );

            var kpi = new TechLeadKpiDto
            {
                TotalToday = todayRequests.Count,
                EmergencyToday = todayRequests.Count(r => r.IsEmergency)
            };

            var statusGroups = todayRequests
                .GroupBy(r => r.RequestTrackings.OrderByDescending(t => t.UpdatedAt).FirstOrDefault()?.Status ?? RequestStatus.Pending)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            kpi.PendingToday = statusGroups.GetValueOrDefault(RequestStatus.Pending.ToString(), 0);
            kpi.InProgressToday = statusGroups.GetValueOrDefault(RequestStatus.InProgress.ToString(), 0);
            kpi.CompletedToday = statusGroups.GetValueOrDefault(RequestStatus.Completed.ToString(), 0);
            kpi.CancelledToday = statusGroups.GetValueOrDefault(RequestStatus.Cancelled.ToString(), 0);
            kpi.StatusBreakdown = statusGroups;

            return kpi;
        }

        public async Task<MonthlyRequestChartDto> GetTechLeadMonthlyRequestsAsync(int year)
        {
            var requestRepo = _unitOfWork.GetRepository<RepairRequest>();

            var yearRequests = await requestRepo.GetListAsync(
                predicate: r => r.CreatedAt.Year == year,
                include: i => i.Include(r => r.RequestTrackings)
            );

            var monthlyData = Enumerable.Range(1, 12).Select(month =>
            {
                var monthRequests = yearRequests.Where(r => r.CreatedAt.Month == month).ToList();
                
                return new MonthlyRequestData
                {
                    Month = month,
                    TotalRequests = monthRequests.Count,
                    CompletedAccepted = monthRequests.Count(r => 
                        r.RequestTrackings.Any(t => 
                            t.Status == RequestStatus.Completed && 
                            t.UpdatedAt.Month == month))
                };
            }).ToList();

            return new MonthlyRequestChartDto { Data = monthlyData };
        }

        public async Task<IssueBreakdownDto> GetTechLeadIssueBreakdownAsync(int month, int year)
        {
            var requestRepo = _unitOfWork.GetRepository<RepairRequest>();

            var monthRequests = await requestRepo.GetListAsync(
                predicate: r => r.CreatedAt.Year == year && r.CreatedAt.Month == month,
                include: i => i.Include(r => r.Issue)
            );

            var total = monthRequests.Count;
            if (total == 0) return new IssueBreakdownDto();

            var issueGroups = monthRequests
                .Where(r => r.Issue != null)
                .GroupBy(r => r.Issue!.Name)
                .Select(g => new IssueBreakdownData
                {
                    IssueType = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((decimal)g.Count() / total * 100, 2)
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            return new IssueBreakdownDto { Data = issueGroups };
        }

        public async Task<YearlyMaintenanceChartDto> GetTechLeadYearlyMaintenanceAsync()
        {
            var scheduleRepo = _unitOfWork.GetRepository<MaintenanceSchedule>();
            var taskRepo = _unitOfWork.GetRepository<RepairRequestTask>();

            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 2, 3).ToList();

            var yearlyData = new List<YearlyMaintenanceData>();

            foreach (var year in years)
            {
                var yearStart = new DateTime(year, 1, 1);
                var yearEnd = new DateTime(year, 12, 31);

                var maintenanceTasks = await taskRepo.GetListAsync(
                    predicate: t => t.CompletedAt.HasValue && 
                                  t.CompletedAt.Value >= yearStart && 
                                  t.CompletedAt.Value <= yearEnd &&
                                  t.MaintenanceTask != null,
                    include: i => i.Include(t => t.RepairRequest)
                );

                var totalMaintained = maintenanceTasks.Count;
                var okObjects = maintenanceTasks.Count(t => 
                    t.Status == TaskCompletionStatus.Completed && 
                    t.InspectionResult?.ToLower().Contains("ok") == true);
                var needFix = maintenanceTasks.Count(t => 
                    t.InspectionResult?.ToLower().Contains("repair") == true ||
                    t.InspectionResult?.ToLower().Contains("replacement") == true);
                var failed = maintenanceTasks.Count(t => t.Status == TaskCompletionStatus.Failed);

                yearlyData.Add(new YearlyMaintenanceData
                {
                    Year = year,
                    TotalMaintainedObjects = totalMaintained,
                    OkObjects = okObjects,
                    NeedFixOrReplace = needFix,
                    FailedMaintenance = failed
                });
            }

            return new YearlyMaintenanceChartDto { Data = yearlyData };
        }

        public async Task<MaintenanceHealthDto> GetTechLeadMaintenanceHealthAsync(int year)
        {
            var taskRepo = _unitOfWork.GetRepository<RepairRequestTask>();

            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);

            var tasks = await taskRepo.GetListAsync(
                predicate: t => t.CompletedAt.HasValue && 
                              t.CompletedAt.Value >= yearStart && 
                              t.CompletedAt.Value <= yearEnd &&
                              t.MaintenanceTask != null
            );

            var total = tasks.Count;
            if (total == 0) return new MaintenanceHealthDto();

            var okCount = tasks.Count(t => 
                t.Status == TaskCompletionStatus.Completed && 
                t.InspectionResult?.ToLower().Contains("ok") == true);
            var needRepairCount = tasks.Count(t => 
                t.InspectionResult?.ToLower().Contains("repair") == true ||
                t.InspectionResult?.ToLower().Contains("replacement") == true);
            var failedCount = tasks.Count(t => t.Status == TaskCompletionStatus.Failed);

            return new MaintenanceHealthDto
            {
                TotalObjects = total,
                OkPercentage = Math.Round((decimal)okCount / total * 100, 2),
                NeedRepairPercentage = Math.Round((decimal)needRepairCount / total * 100, 2),
                FailedPercentage = Math.Round((decimal)failedCount / total * 100, 2)
            };
        }

        #endregion

        #region Manager Dashboard

        public async Task<ManagerKpiDto> GetManagerKpiAsync()
        {
            var requestRepo = _unitOfWork.GetRepository<RepairRequest>();
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var requests = await requestRepo.GetListAsync(
                include: i => i.Include(r => r.RequestTrackings)
            );

            var completedThisMonth = requests.Count(r => 
                r.RequestTrackings.Any(t => 
                    t.Status == RequestStatus.Completed && 
                    t.UpdatedAt.Month == currentMonth && 
                    t.UpdatedAt.Year == currentYear));

            var inProgress = requests.Count(r => 
                r.RequestTrackings.OrderByDescending(t => t.UpdatedAt)
                    .FirstOrDefault()?.Status == RequestStatus.InProgress);

            var waitingApproval = requests.Count(r => 
                r.RequestTrackings.OrderByDescending(t => t.UpdatedAt)
                    .FirstOrDefault()?.Status == RequestStatus.WaitingManagerApproval);

            return new ManagerKpiDto
            {
                CompletedThisMonth = completedThisMonth,
                InProgressTotal = inProgress,
                WaitingManagerApproval = waitingApproval
            };
        }

        public async Task<MonthlyRevenueChartDto> GetManagerMonthlyRevenueAsync(int year)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();

            var paidInvoices = await invoiceRepo.GetListAsync(
                predicate: i => i.Status == InvoiceStatus.Paid && 
                              i.CreatedAt.Year == year,
                include: i => i.Include(inv => inv.Transactions)
            );

            var monthlyData = Enumerable.Range(1, 12).Select(month =>
            {
                var monthInvoices = paidInvoices.Where(i => i.CreatedAt.Month == month).ToList();

                var paidAmount = monthInvoices
                    .Where(i => i.IsChargeable)
                    .Sum(i => i.TotalAmount);

                var refundAmount = monthInvoices
                    .Where(i => !i.IsChargeable)
                    .Sum(i => i.TotalAmount);

                return new MonthlyRevenueData
                {
                    Month = month,
                    PaidInvoicesAmount = paidAmount,
                    RefundAmount = refundAmount
                };
            }).ToList();

            return new MonthlyRevenueChartDto { Data = monthlyData };
        }

        public async Task<TopAccessoriesDto> GetManagerTopAccessoriesAsync(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Now.AddMonths(-3);
            var to = toDate ?? DateTime.Now;

            var invoiceAccessoryRepo = _unitOfWork.GetRepository<InvoiceAccessory>();

            var accessories = await invoiceAccessoryRepo.GetListAsync(
                predicate: ia => ia.Invoice.CreatedAt >= from && ia.Invoice.CreatedAt <= to,
                include: i => i.Include(ia => ia.Invoice)
                              .Include(ia => ia.Accessory)
            );

            var topAccessories = accessories
                .GroupBy(ia => new { ia.AccessoryId, ia.Name })
                .Select(g => new AccessoryUsageData
                {
                    AccessoryName = g.Key.Name,
                    TotalQuantity = g.Sum(ia => ia.Quantity),
                    UsageCount = g.Count()
                })
                .OrderByDescending(a => a.TotalQuantity)
                .Take(7)
                .ToList();

            return new TopAccessoriesDto { Data = topAccessories };
        }

        #endregion

        #region Admin Dashboard

        public async Task<AdminKpiDto> GetAdminKpiAsync()
        {
            var today = DateTime.Today;
            var requestRepo = _unitOfWork.GetRepository<RepairRequest>();
            var appointmentRepo = _unitOfWork.GetRepository<Appointment>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();

            // Repair Requests
            var requests = await requestRepo.GetListAsync(
                include: i => i.Include(r => r.RequestTrackings)
            );

            var requestsByStatus = requests
                .GroupBy(r => r.RequestTrackings.OrderByDescending(t => t.UpdatedAt).FirstOrDefault()?.Status ?? RequestStatus.Pending)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var emergencyToday = requests.Count(r => r.IsEmergency && r.CreatedAt.Date == today);

            // Appointments
            var appointments = await appointmentRepo.GetListAsync(
                include: i => i.Include(a => a.AppointmentTrackings)
            );

            var appointmentsByStatus = appointments
                .GroupBy(a => a.AppointmentTrackings.OrderByDescending(t => t.UpdatedAt).FirstOrDefault()?.Status ?? AppointmentStatus.Pending)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            // Financial
            var invoices = await invoiceRepo.GetListAsync();
            var transactions = await transactionRepo.GetListAsync();

            var awaitingPayment = invoices
                .Where(i => i.Status == InvoiceStatus.AwaitingPayment)
                .Sum(i => i.TotalAmount);

            var failedAmount = transactions
                .Where(t => t.Status == TransactionStatus.Failed)
                .Sum(t => t.Amount);

            var paidAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            var invoicesByStatus = invoices
                .GroupBy(i => i.Status)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return new AdminKpiDto
            {
                RequestsByStatus = requestsByStatus,
                EmergencyToday = emergencyToday,
                AppointmentsByStatus = appointmentsByStatus,
                TotalAwaitingPaymentAmount = awaitingPayment,
                TotalFailedAmount = failedAmount,
                TotalPaidAmount = paidAmount,
                InvoicesByStatus = invoicesByStatus
            };
        }

        public async Task<AppointmentVolumeChartDto> GetAdminMonthlyAppointmentsAsync(int month, int year)
        {
            var appointmentRepo = _unitOfWork.GetRepository<Appointment>();

            var monthAppointments = await appointmentRepo.GetListAsync(
                predicate: a => a.StartTime.Year == year && a.StartTime.Month == month,
                include: i => i.Include(a => a.AppointmentTrackings)
            );

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var dailyData = Enumerable.Range(1, daysInMonth).Select(day =>
            {
                var dayAppointments = monthAppointments
                    .Where(a => a.StartTime.Day == day)
                    .ToList();

                return new AppointmentVolumeData
                {
                    Day = day,
                    TotalAppointments = dayAppointments.Count,
                    CompletedAppointments = dayAppointments.Count(a => 
                        a.AppointmentTrackings.Any(t => t.Status == AppointmentStatus.Completed))
                };
            }).ToList();

            return new AppointmentVolumeChartDto { Data = dailyData };
        }

        public async Task<IssueBreakdownDto> GetAdminIssueBreakdownAsync(int month, int year)
        {
            return await GetTechLeadIssueBreakdownAsync(month, year);
        }

        public async Task<MonthlyRevenueChartDto> GetAdminMonthlyRevenueAsync(int year)
        {
            return await GetManagerMonthlyRevenueAsync(year);
        }

        public async Task<TopAccessoriesDto> GetAdminTopAccessoriesAsync(DateTime? fromDate, DateTime? toDate)
        {
            return await GetManagerTopAccessoriesAsync(fromDate, toDate);
        }

        public async Task<MonthlyMaintenanceChartDto> GetAdminMonthlyMaintenanceAsync(int year)
        {
            var taskRepo = _unitOfWork.GetRepository<RepairRequestTask>();

            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);

            var tasks = await taskRepo.GetListAsync(
                predicate: t => t.CompletedAt.HasValue && 
                              t.CompletedAt.Value >= yearStart && 
                              t.CompletedAt.Value <= yearEnd &&
                              t.MaintenanceTask != null
            );

            var monthlyData = Enumerable.Range(1, 12).Select(month =>
            {
                var monthTasks = tasks.Where(t => t.CompletedAt!.Value.Month == month).ToList();

                return new MonthlyMaintenanceData
                {
                    Month = month,
                    TotalMaintainedObjects = monthTasks.Count,
                    OkObjects = monthTasks.Count(t => 
                        t.Status == TaskCompletionStatus.Completed && 
                        t.InspectionResult?.ToLower().Contains("ok") == true),
                    NeedFixOrReplace = monthTasks.Count(t => 
                        t.InspectionResult?.ToLower().Contains("repair") == true ||
                        t.InspectionResult?.ToLower().Contains("replacement") == true),
                    FailedMaintenance = monthTasks.Count(t => t.Status == TaskCompletionStatus.Failed)
                };
            }).ToList();

            return new MonthlyMaintenanceChartDto { Data = monthlyData };
        }

        public async Task<MaintenanceHealthDto> GetAdminMaintenanceHealthAsync(int month, int year)
        {
            var taskRepo = _unitOfWork.GetRepository<RepairRequestTask>();

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var tasks = await taskRepo.GetListAsync(
                predicate: t => t.CompletedAt.HasValue && 
                              t.CompletedAt.Value >= monthStart && 
                              t.CompletedAt.Value <= monthEnd &&
                              t.MaintenanceTask != null
            );

            var total = tasks.Count;
            if (total == 0) return new MaintenanceHealthDto();

            var okCount = tasks.Count(t => 
                t.Status == TaskCompletionStatus.Completed && 
                t.InspectionResult?.ToLower().Contains("ok") == true);
            var needRepairCount = tasks.Count(t => 
                t.InspectionResult?.ToLower().Contains("repair") == true ||
                t.InspectionResult?.ToLower().Contains("replacement") == true);
            var failedCount = tasks.Count(t => t.Status == TaskCompletionStatus.Failed);

            return new MaintenanceHealthDto
            {
                TotalObjects = total,
                OkPercentage = Math.Round((decimal)okCount / total * 100, 2),
                NeedRepairPercentage = Math.Round((decimal)needRepairCount / total * 100, 2),
                FailedPercentage = Math.Round((decimal)failedCount / total * 100, 2)
            };
        }

        #endregion
    }
}
