using UtilityBillingSystem.Models.Dto.UtilityType;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IUtilityTypeService
    {
        Task<IEnumerable<UtilityTypeDto>> GetUtilityTypesAsync();
        Task<IEnumerable<UtilityTypeDto>> GetUtilityTypesForUserAsync(string userId);
        Task<UtilityTypeDto> GetUtilityTypeByIdAsync(string id);
        Task<UtilityTypeDto> CreateUtilityTypeAsync(UtilityTypeDto dto, string currentUserEmail);
        Task<UtilityTypeDto> UpdateUtilityTypeAsync(string id, UtilityTypeDto dto, string currentUserEmail);
        Task DeleteUtilityTypeAsync(string id, string currentUserEmail);
    }
}

