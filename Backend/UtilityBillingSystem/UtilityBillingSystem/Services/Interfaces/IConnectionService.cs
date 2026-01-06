using UtilityBillingSystem.Models.Dto.Connection;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IConnectionService
    {
        Task<IEnumerable<ConnectionDto>> GetConnectionsAsync();
        Task<IEnumerable<ConnectionDto>> GetConnectionsForUserAsync(string userId);
        Task<ConnectionDto> GetConnectionByIdAsync(string id, string? userId = null, bool isConsumer = false);
        Task<ConnectionDto> CreateConnectionAsync(ConnectionDto dto, string currentUserEmail);
        Task<ConnectionDto> UpdateConnectionAsync(string id, ConnectionDto dto, string currentUserEmail);
        Task DeleteConnectionAsync(string id, string currentUserEmail);
    }
}

