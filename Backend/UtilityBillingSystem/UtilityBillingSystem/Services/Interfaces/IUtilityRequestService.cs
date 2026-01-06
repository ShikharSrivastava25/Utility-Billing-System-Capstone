using UtilityBillingSystem.Models.Dto.UtilityRequest;
using UtilityBillingSystem.Models.Dto.Connection;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IUtilityRequestService
    {
        Task<IEnumerable<UtilityRequestDto>> GetRequestsAsync();
        Task<IEnumerable<UtilityRequestDto>> GetRequestsForUserAsync(string userId);
        Task<UtilityRequestDto> GetRequestByIdAsync(string id);
        Task<UtilityRequestDto> CreateRequestAsync(UtilityRequestDto dto, string userId, bool isAdmin);
        Task<ConnectionDto> ApproveRequestAsync(string id, ApproveRequestDto dto, string currentUserEmail);
        Task<UtilityRequestDto> RejectRequestAsync(string id, string currentUserEmail);
    }
}

