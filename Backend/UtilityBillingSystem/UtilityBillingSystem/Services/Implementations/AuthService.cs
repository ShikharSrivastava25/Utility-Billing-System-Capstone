using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Auth;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;

        public AuthService(UserManager<User> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new UnauthorizedAccessException("Invalid email or password");

            if (user.Status != "Active")
                throw new UnauthorizedAccessException("Account is inactive");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Consumer";

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? ""));

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                expires: DateTime.UtcNow.AddHours(2),
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? "",
                Role = role,
                Status = user.Status
            };

            return new LoginResponseDto
            {
                Token = tokenString,
                User = userDto
            };
        }

        public async Task RegisterAsync(RegisterRequestDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                throw new InvalidOperationException("Email already exists");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.Name,
                Status = "Active"
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Assign Consumer role by default
            await _userManager.AddToRoleAsync(user, "Consumer");
        }
    }
}

