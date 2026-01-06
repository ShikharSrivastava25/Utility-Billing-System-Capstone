using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Auth;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class AuthServiceTests : BaseServiceTest
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly AuthService _service;

        public AuthServiceTests() : base()
        {
            // Setup UserManager and RoleManager with InMemory database
            var userStore = new UserStore<User, IdentityRole, AppDbContext>(
                Context, 
                new IdentityErrorDescriber());
            
            _userManager = new UserManager<User>(
                userStore,
                null,
                new PasswordHasher<User>(),
                new List<IUserValidator<User>> { new UserValidator<User>() },
                new List<IPasswordValidator<User>> { new PasswordValidator<User>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                null);

            var roleStore = new RoleStore<IdentityRole, AppDbContext>(
                Context,
                new IdentityErrorDescriber());
            
            _roleManager = new RoleManager<IdentityRole>(
                roleStore,
                new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null
             );

            var configDict = new Dictionary<string, string?>
            {
                { "Jwt:Key", "THIS_IS_A_VERY_SECURE_KEY_123456789012345678901234" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            _service = new AuthService(_userManager, _configuration);

            // Seed Consumer role
            SeedRolesAsync().Wait();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "Consumer", "Admin", "Billing Officer", "Account Officer" };
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
        {
            // Arrange
            var user = new User
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                FullName = "Test User",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, "Test@123");
            Assert.True(createResult.Succeeded);
            await _userManager.AddToRoleAsync(user, "Consumer");

            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Test@123"
            };

            // Act
            var result = await _service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(user.Id, result.User.Id);
            Assert.Equal("test@example.com", result.User.Email);
            Assert.Equal("Test User", result.User.Name);
            Assert.Equal("Consumer", result.User.Role);
            Assert.Equal("Active", result.User.Status);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "nonexistent@example.com",
                Password = "Test@123"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.LoginAsync(loginDto));
            Assert.Contains("Invalid email or password", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var user = new User
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                FullName = "Test User",
                Status = "Inactive",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, "Test@123");
            Assert.True(createResult.Succeeded);
            await _userManager.AddToRoleAsync(user, "Consumer");

            var loginDto = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Test@123"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.LoginAsync(loginDto));
            Assert.Contains("Account is inactive", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingUser = new User
            {
                UserName = "existing@example.com",
                Email = "existing@example.com",
                FullName = "Existing User",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(existingUser, "Test@123");
            Assert.True(createResult.Succeeded);
            
            var registerDto = new RegisterRequestDto
            {
                Email = "existing@example.com",
                Password = "Test@123",
                Name = "New User"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterAsync(registerDto));
            Assert.Contains("Email already exists", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_AssignsConsumerRole()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "newuser@example.com",
                Password = "Test@123",
                Name = "New User"
            };

            // Act
            await _service.RegisterAsync(registerDto);

            // Assert
            var createdUser = await _userManager.FindByEmailAsync("newuser@example.com");
            Assert.NotNull(createdUser);
            var roles = await _userManager.GetRolesAsync(createdUser);
            Assert.Contains("Consumer", roles);
            Assert.Single(roles); // Should only have Consumer role
        }

        [Fact]
        public async Task LoginAsync_WithAdminRole_ReturnsAdminRoleInToken()
        {
            // Arrange
            var user = new User
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
                FullName = "Admin User",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, "Test@123");
            Assert.True(createResult.Succeeded);
            await _userManager.AddToRoleAsync(user, "Admin");

            var loginDto = new LoginRequestDto
            {
                Email = "admin@example.com",
                Password = "Test@123"
            };

            // Act
            var result = await _service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Admin", result.User.Role);
            Assert.NotNull(result.Token);
            
            // Verify token contains Admin role claim
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.Token);
            var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal("Admin", roleClaim.Value);
        }

        [Fact]
        public async Task LoginAsync_WithBillingOfficerRole_ReturnsBillingOfficerRoleInToken()
        {
            // Arrange
            var user = new User
            {
                UserName = "billing@example.com",
                Email = "billing@example.com",
                FullName = "Billing Officer",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, "Test@123");
            Assert.True(createResult.Succeeded);
            await _userManager.AddToRoleAsync(user, "Billing Officer");

            var loginDto = new LoginRequestDto
            {
                Email = "billing@example.com",
                Password = "Test@123"
            };

            // Act
            var result = await _service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Billing Officer", result.User.Role);
            
            // Verify token contains Billing Officer role claim
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.Token);
            var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal("Billing Officer", roleClaim.Value);
        }

        [Fact]
        public async Task LoginAsync_WithNoRole_DefaultsToConsumer()
        {
            // Arrange
            var user = new User
            {
                UserName = "norole@example.com",
                Email = "norole@example.com",
                FullName = "No Role User",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, "Test@123");
            Assert.True(createResult.Succeeded);
            // Intentionally not assigning any role

            var loginDto = new LoginRequestDto
            {
                Email = "norole@example.com",
                Password = "Test@123"
            };

            // Act
            var result = await _service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Consumer", result.User.Role); // Should default to Consumer
            
            // Verify token contains Consumer role claim
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.Token);
            var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal("Consumer", roleClaim.Value);
        }
    }
}

