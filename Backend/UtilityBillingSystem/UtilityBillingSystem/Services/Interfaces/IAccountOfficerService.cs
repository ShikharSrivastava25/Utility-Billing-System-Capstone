using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Models.Dto.Report;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IAccountOfficerService
    {
        Task<AccountOfficerDashboardDto> GetDashboardSummaryAsync();
        Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueByBillingDateAsync(
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int? month = null, 
            int? year = null);
        Task<IEnumerable<RecentPaymentDto>> GetRecentPaymentsAsync(int count = 5);
        Task<IEnumerable<OutstandingByUtilityDto>> GetOutstandingByUtilityAsync();
        Task<PagedResult<PaymentAuditDto>> GetAllPaymentsAsync(int page, int pageSize);
        Task<PagedResult<OutstandingBillDto>> GetOutstandingBillsAsync(string? statusFilter, int page, int pageSize);
        Task<PagedResult<ConsumerBillingSummaryDto>> GetConsumerBillingSummaryAsync(int page, int pageSize);
    }
}
