using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.DashboardDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class DashboardController : BaseApiController
    {
        private readonly IOverViewDashboardService _dashboardService;

        public DashboardController(IOverViewDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        #region TechLead Dashboard

        /// <summary>
        /// Lấy KPI nhanh cho TechLead - Repair Request hôm nay theo status.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead  
        ///   
        /// **Trả về:**
        /// - Tổng số request hôm nay
        /// - Số Pending (chờ phân công)
        /// - Số InProgress
        /// - Số Completed
        /// - Số Cancelled
        /// - Số Emergency hôm nay
        /// - Stacked bar breakdown theo status
        /// </remarks>
        /// <returns>KPI card data cho TechLead.</returns>
        [HttpGet("techlead/kpi")]
        [Authorize(Roles = nameof(AccountRole.TechnicianLead))]
        [ProducesResponseType(typeof(TechLeadKpiDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TechLeadKpiDto>> GetTechLeadKpi()
        {
            var result = await _dashboardService.GetTechLeadKpiAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ Request theo tháng và đã nghiệm thu cho TechLead.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead  
        ///   
        /// **Chart type:** Column chart  
        /// - Trục X: Tháng (1-12)
        /// - Trục Y: Số repair request
        /// - 2 series: TotalRequests và CompletedAccepted
        /// </remarks>
        /// <param name="year">Năm cần xem (mặc định: năm hiện tại).</param>
        /// <returns>Dữ liệu biểu đồ monthly requests.</returns>
        [HttpGet("techlead/monthly-requests")]
        [Authorize(Roles = nameof(AccountRole.TechnicianLead))]
        [ProducesResponseType(typeof(MonthlyRequestChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MonthlyRequestChartDto>> GetTechLeadMonthlyRequests([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetTechLeadMonthlyRequestsAsync(targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ Donut phân loại issue trong tháng cho TechLead.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead  
        ///   
        /// **Chart type:** Donut chart  
        /// - Mỗi lát là IssueType (Điện, Nước, Thang máy, Internet, Khác...)
        /// - Data: Số request có issue đó trong tháng
        /// </remarks>
        /// <param name="month">Tháng cần xem (1-12).</param>
        /// <param name="year">Năm cần xem.</param>
        /// <returns>Dữ liệu breakdown issue.</returns>
        [HttpGet("techlead/issue-breakdown")]
        [Authorize(Roles = nameof(AccountRole.TechnicianLead))]
        [ProducesResponseType(typeof(IssueBreakdownDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IssueBreakdownDto>> GetTechLeadIssueBreakdown(
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetTechLeadIssueBreakdownAsync(targetMonth, targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ tổng hợp bảo trì theo năm cho TechLead.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead  
        ///   
        /// **Chart type:** Grouped column chart  
        /// - Trục X: Năm (3 năm gần nhất)
        /// - Trục Y: Số object bảo trì
        /// - 4 cột: TotalMaintained, Ok, NeedFix, Failed
        /// </remarks>
        /// <returns>Dữ liệu yearly maintenance.</returns>
        [HttpGet("techlead/yearly-maintenance")]
        [Authorize(Roles = nameof(AccountRole.TechnicianLead))]
        [ProducesResponseType(typeof(YearlyMaintenanceChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<YearlyMaintenanceChartDto>> GetTechLeadYearlyMaintenance()
        {
            var result = await _dashboardService.GetTechLeadYearlyMaintenanceAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ Donut % tình trạng object sau bảo trì cho TechLead.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** TechnicianLead  
        ///   
        /// **Chart type:** Donut chart  
        /// - 3 lát: % Ok, % NeedRepair, % Failed
        /// </remarks>
        /// <param name="year">Năm cần xem (mặc định: năm hiện tại).</param>
        /// <returns>Dữ liệu maintenance health percentage.</returns>
        [HttpGet("techlead/maintenance-health")]
        [Authorize(Roles = nameof(AccountRole.TechnicianLead))]
        [ProducesResponseType(typeof(MaintenanceHealthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MaintenanceHealthDto>> GetTechLeadMaintenanceHealth([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetTechLeadMaintenanceHealthAsync(targetYear);
            return Ok(result);
        }

        #endregion

        #region Manager Dashboard

        /// <summary>
        /// Lấy KPI nhanh cho Manager.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///   
        /// **Trả về:**
        /// - Số Completed tháng này
        /// - Tổng số request đang xử lý (InProgress)
        /// - Số WaitingManagerApproval (cần phê duyệt)
        /// </remarks>
        /// <returns>KPI card data cho Manager.</returns>
        [HttpGet("manager/kpi")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(ManagerKpiDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ManagerKpiDto>> GetManagerKpi()
        {
            var result = await _dashboardService.GetManagerKpiAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ doanh thu theo tháng cho Manager.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///   
        /// **Chart type:** Column chart  
        /// - Trục X: Tháng
        /// - Trục Y: Số tiền (VND)
        /// - Series: PaidInvoicesAmount, RefundAmount
        /// </remarks>
        /// <param name="year">Năm cần xem (mặc định: năm hiện tại).</param>
        /// <returns>Dữ liệu monthly revenue.</returns>
        [HttpGet("manager/monthly-revenue")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(MonthlyRevenueChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MonthlyRevenueChartDto>> GetManagerMonthlyRevenue([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetManagerMonthlyRevenueAsync(targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy Top 7 accessories được dùng nhiều nhất cho Manager.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///   
        /// **Chart type:** Horizontal bar chart  
        /// - Trục Y: Tên phụ kiện (Top 7)
        /// - Trục X: Số lượng đã dùng
        /// </remarks>
        /// <param name="fromDate">Từ ngày (mặc định: 3 tháng trước).</param>
        /// <param name="toDate">Đến ngày (mặc định: hôm nay).</param>
        /// <returns>Dữ liệu top accessories.</returns>
        [HttpGet("manager/top-accessories")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(TopAccessoriesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TopAccessoriesDto>> GetManagerTopAccessories(
            [FromQuery] DateTime? fromDate, 
            [FromQuery] DateTime? toDate)
        {
            var result = await _dashboardService.GetManagerTopAccessoriesAsync(fromDate, toDate);
            return Ok(result);
        }

        #endregion

        #region Admin Dashboard

        /// <summary>
        /// Lấy KPI tổng quan hệ thống cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// **Trả về:**
        /// - Request by status (toàn hệ thống)
        /// - Emergency today
        /// - Appointments by status
        /// - Tổng tiền: AwaitingPayment, Failed, Paid
        /// - Invoices by status
        /// </remarks>
        /// <returns>KPI card data cho Admin.</returns>
        [HttpGet("admin/kpi")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(AdminKpiDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AdminKpiDto>> GetAdminKpi()
        {
            var result = await _dashboardService.GetAdminKpiAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ lịch hẹn trong tháng cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// **Chart type:** Column chart  
        /// - Trục X: Ngày trong tháng (1-31)
        /// - Trục Y: Số appointment
        /// - 2 series: Total, Completed
        /// </remarks>
        /// <param name="month">Tháng cần xem.</param>
        /// <param name="year">Năm cần xem.</param>
        /// <returns>Dữ liệu appointment volume.</returns>
        [HttpGet("admin/monthly-appointments")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(AppointmentVolumeChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AppointmentVolumeChartDto>> GetAdminMonthlyAppointments(
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetAdminMonthlyAppointmentsAsync(targetMonth, targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ Donut phân loại issue trong tháng cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// Tương tự TechLead nhưng dữ liệu toàn hệ thống.
        /// </remarks>
        [HttpGet("admin/issue-breakdown")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(IssueBreakdownDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IssueBreakdownDto>> GetAdminIssueBreakdown(
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetAdminIssueBreakdownAsync(targetMonth, targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ doanh thu theo tháng cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// Tương tự Manager nhưng góc nhìn toàn hệ thống.
        /// </remarks>
        [HttpGet("admin/monthly-revenue")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(MonthlyRevenueChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MonthlyRevenueChartDto>> GetAdminMonthlyRevenue([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetAdminMonthlyRevenueAsync(targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy Top 7 accessories cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// Tương tự Manager.
        /// </remarks>
        [HttpGet("admin/top-accessories")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(TopAccessoriesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TopAccessoriesDto>> GetAdminTopAccessories(
            [FromQuery] DateTime? fromDate, 
            [FromQuery] DateTime? toDate)
        {
            var result = await _dashboardService.GetAdminTopAccessoriesAsync(fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ bảo trì theo tháng cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// **Chart type:** Grouped column chart  
        /// - Trục X: Tháng (1-12)
        /// - Trục Y: Số object
        /// - 4 cột: Total, Ok, NeedFix, Failed
        /// </remarks>
        /// <param name="year">Năm cần xem.</param>
        /// <returns>Dữ liệu monthly maintenance.</returns>
        [HttpGet("admin/monthly-maintenance")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(MonthlyMaintenanceChartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MonthlyMaintenanceChartDto>> GetAdminMonthlyMaintenance([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetAdminMonthlyMaintenanceAsync(targetYear);
            return Ok(result);
        }

        /// <summary>
        /// Lấy biểu đồ Donut % tình trạng object sau bảo trì cho Admin.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Admin  
        ///   
        /// Chi tiết hơn TechLead (theo tháng thay vì năm).
        /// </remarks>
        /// <param name="month">Tháng cần xem.</param>
        /// <param name="year">Năm cần xem.</param>
        /// <returns>Dữ liệu maintenance health.</returns>
        [HttpGet("admin/maintenance-health")]
        [Authorize(Roles = nameof(AccountRole.Admin))]
        [ProducesResponseType(typeof(MaintenanceHealthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MaintenanceHealthDto>> GetAdminMaintenanceHealth(
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _dashboardService.GetAdminMaintenanceHealthAsync(targetMonth, targetYear);
            return Ok(result);
        }

        #endregion
    }
}