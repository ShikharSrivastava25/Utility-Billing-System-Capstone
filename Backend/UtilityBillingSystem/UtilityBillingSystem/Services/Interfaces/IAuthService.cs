using UtilityBillingSystem.Models.Dto.Auth;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
        Task RegisterAsync(RegisterRequestDto dto);
    }
}

