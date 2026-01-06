using UtilityBillingSystem.Models.Dto.Payment;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> RecordPaymentAsync(string billId, CreatePaymentDto dto, string userId, string userEmail);
        Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryForUserAsync(
            string userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null);
        Task<decimal> GetOutstandingBalanceForUserAsync(string userId);
        Task<decimal> GetMonthlySpendingForUserAsync(string userId, DateTime monthDateUtc);
        Task<IEnumerable<(string MonthLabel, decimal TotalConsumption)>> GetMonthlyConsumptionForUserAsync(string userId);
    }
}


