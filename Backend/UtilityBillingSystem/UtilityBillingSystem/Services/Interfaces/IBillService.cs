using UtilityBillingSystem.Models.Dto.Bill;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IBillService
    {
        Task<IEnumerable<PendingBillDto>> GetPendingBillsAsync();
        Task<BillDto> GenerateBillAsync(string readingId, string userEmail);
        Task<BillGenerationResponseDto> GenerateBillsBatchAsync(List<string> readingIds, string userEmail);
        Task<IEnumerable<BillDetailDto>> GetBillsByConnectionAsync(string connectionId);
        Task<BillDetailDto?> GetBillByIdAsync(string id);
        Task<IEnumerable<BillDetailDto>> GetBillsForUserAsync(string userId);
        Task<BillDetailDto?> GetBillForUserByIdAsync(string billId, string userId);
        Task<int> GetDueBillsCountForUserAsync(string userId);
    }
}


