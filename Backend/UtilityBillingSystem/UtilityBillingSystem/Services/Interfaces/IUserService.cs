using UtilityBillingSystem.Models.Dto.User;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetUsersAsync();
        Task<UserDto> GetUserByIdAsync(string id);
        Task<UserDto> CreateUserAsync(CreateUserDto dto, string currentUserId);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserDto dto, string currentUserId);
        Task DeleteUserAsync(string id, string currentUserId);
    }
}

