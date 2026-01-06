using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Models.Dto.Consumer;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportSummaryDto> GetReportSummaryAsync();
        Task<IEnumerable<OverdueBillDto>> GetOverdueBillsAsync();
        Task<IEnumerable<ConsumptionDataDto>> GetConsumptionByUtilityAsync();
        Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<IEnumerable<AverageConsumptionDto>> GetAverageConsumptionAsync();
        Task<IEnumerable<ConnectionsByUtilityDto>> GetConnectionsByUtilityAsync();
        Task<IEnumerable<object>> GetMyConsumptionAsync(string userId, string? utilityTypeId = null);
        Task<ConsumerDashboardResponse> GetConsumerDashboardAsync(string userId);
    }
}

