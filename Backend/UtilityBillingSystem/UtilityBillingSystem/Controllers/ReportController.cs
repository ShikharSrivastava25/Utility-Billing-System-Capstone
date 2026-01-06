using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Models.Dto.Consumer;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly IAccountOfficerService _accountOfficerService;

        public ReportController(IReportService reportService, IAccountOfficerService accountOfficerService)
        {
            _reportService = reportService;
            _accountOfficerService = accountOfficerService;
        }

        [HttpGet("summary")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ReportSummaryDto>> GetReportSummary()
        {
            var summary = await _reportService.GetReportSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("consumption")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<IEnumerable<ConsumptionDataDto>>> GetConsumptionByUtility()
        {
            var consumptionData = await _reportService.GetConsumptionByUtilityAsync();
            return Ok(consumptionData);
        }

        [HttpGet("average-consumption")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<IEnumerable<AverageConsumptionDto>>> GetAverageConsumption()
        {
            var averageConsumption = await _reportService.GetAverageConsumptionAsync();
            return Ok(averageConsumption);
        }

        [HttpGet("connections-by-utility")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ConnectionsByUtilityDto>>> GetConnectionsByUtility()
        {
            var connectionsByUtility = await _reportService.GetConnectionsByUtilityAsync();
            return Ok(connectionsByUtility);
        }

        [HttpGet("my-consumption")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyConsumption(string? utilityTypeId = null)
        {
            var consumption = await _reportService.GetMyConsumptionAsync(CurrentUserId, utilityTypeId);
            return Ok(consumption);
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<ConsumerDashboardResponse>> GetConsumerDashboard()
        {
            var dashboard = await _reportService.GetConsumerDashboardAsync(CurrentUserId);
            return Ok(dashboard);
        }

        // Account Officer Dashboard Endpoints
        [HttpGet("dashboard/summary")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<AccountOfficerDashboardDto>> GetAccountOfficerDashboardSummary()
        {
            var summary = await _accountOfficerService.GetDashboardSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("monthly-revenue-by-billing")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<IEnumerable<MonthlyRevenueDto>>> GetMonthlyRevenueByBillingDate(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? month = null,
            int? year = null)
        {
            var monthlyRevenue = await _accountOfficerService.GetMonthlyRevenueByBillingDateAsync(startDate, endDate, month, year);
            return Ok(monthlyRevenue);
        }

        [HttpGet("dashboard/recent-payments")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<IEnumerable<RecentPaymentDto>>> GetRecentPayments(int count = 5)
        {
            var recentPayments = await _accountOfficerService.GetRecentPaymentsAsync(count);
            return Ok(recentPayments);
        }

        [HttpGet("dashboard/outstanding-by-utility")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<IEnumerable<OutstandingByUtilityDto>>> GetOutstandingByUtility()
        {
            var outstanding = await _accountOfficerService.GetOutstandingByUtilityAsync();
            return Ok(outstanding);
        }
    }
}
