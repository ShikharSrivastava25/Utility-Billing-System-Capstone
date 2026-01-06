using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class UserServiceTests : BaseServiceTest
    {
        private readonly UserManager<Models.Core.User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserService _service;

        public UserServiceTests() : base()
        {
            _userManager = CreateUserManager();
            _roleManager = CreateRoleManager();
            SeedRolesAsync(_roleManager).Wait();
            _service = new UserService(_userManager, _roleManager, MockAuditLogService.Object, Context);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithDeletedUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            var user = await CreateUserAsync(email: "test@example.com", status: "Deleted");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetUserByIdAsync(user.Id));
            Assert.Contains("User not found", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingUser = new Models.Core.User
            {
                UserName = "existing@example.com",
                Email = "existing@example.com",
                FullName = "Existing User",
                Status = "Active",
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(existingUser, "Test@123");
            Assert.True(createResult.Succeeded);
            
            var createDto = new CreateUserDto
            {
                Name = "New User",
                Email = "existing@example.com",
                Password = "Test@123",
                Role = "Consumer"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateUserAsync(createDto, "admin@example.com"));
            Assert.Contains("Email already exists", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_DeactivatingUser_DeactivatesConnections()
        {
            // Arrange
            var user = await CreateUserAsync(email: "test@example.com");
            await _userManager.AddToRoleAsync(user, "Consumer");
            var connection = await CreateFullConnectionAsync(userId: user.Id, status: "Active");
            var currentUserId = Guid.NewGuid().ToString();

            var updateDto = new UpdateUserDto
            {
                Name = user.FullName,
                Email = user.Email,
                Role = "Consumer",
                Status = "Inactive"
            };

            // Act
            await _service.UpdateUserAsync(user.Id, updateDto, currentUserId);

            // Assert
            await Context.Entry(connection).ReloadAsync();
            Assert.Equal("Inactive", connection.Status);
        }

        [Fact]
        public async Task DeleteUserAsync_WithUnpaidBills_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = await CreateUserAsync();
            var connection = await CreateFullConnectionAsync(userId: user.Id);
            var bill = await CreateBillAsync(connection.Id, 100m, 500m, 90m, 590m, status: "Generated");
            var currentUserId = Guid.NewGuid().ToString();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteUserAsync(user.Id, currentUserId));
            Assert.Contains("Cannot delete user with unpaid bills", ex.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_WithActiveConnections_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = await CreateUserAsync();
            var connection = await CreateFullConnectionAsync(userId: user.Id, status: "Active");
            var currentUserId = Guid.NewGuid().ToString();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteUserAsync(user.Id, currentUserId));
            Assert.Contains("Cannot delete user with active connections", ex.Message);
        }

    }
}

