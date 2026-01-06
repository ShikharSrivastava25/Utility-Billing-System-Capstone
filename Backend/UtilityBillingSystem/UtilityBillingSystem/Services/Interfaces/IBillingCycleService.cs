using UtilityBillingSystem.Models.Dto.BillingCycle;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IBillingCycleService
    {
        Task<IEnumerable<BillingCycleDto>> GetBillingCyclesAsync();
        Task<BillingCycleDto> GetBillingCycleByIdAsync(string id);
        Task<BillingCycleDto> CreateBillingCycleAsync(BillingCycleDto dto, string currentUserEmail);
        Task<BillingCycleDto> UpdateBillingCycleAsync(string id, BillingCycleDto dto, string currentUserEmail);
        Task DeleteBillingCycleAsync(string id, string currentUserEmail);
    }
}

