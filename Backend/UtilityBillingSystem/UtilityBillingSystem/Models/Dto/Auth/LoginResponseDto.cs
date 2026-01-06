using UtilityBillingSystem.Models.Dto.User;

namespace UtilityBillingSystem.Models.Dto.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }
}

