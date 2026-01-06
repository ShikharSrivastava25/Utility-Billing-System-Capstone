using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class ConnectionServiceTests : BaseServiceTest
    {
        private readonly ConnectionService _service;

        public ConnectionServiceTests() : base()
        {
            _service = new ConnectionService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task GetConnectionByIdAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var user1 = await CreateUserAsync();
            var user2 = await CreateUserAsync();
            var connection = await CreateFullConnectionAsync(userId: user1.Id);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetConnectionByIdAsync(connection.Id, user2.Id, isConsumer: true));
            Assert.Contains("You do not have permission to access this connection", ex.Message);
        }

        [Fact]
        public async Task CreateConnectionAsync_WithDuplicateMeterNumber_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = await CreateUserAsync();
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var tariff = await CreateTariffAsync(utilityType.Id);
            var existingConnection = await CreateConnectionAsync(user.Id, utilityType.Id, tariff.Id, meterNumber: "M12345");

            var dto = new ConnectionDto
            {
                UserId = user.Id,
                UtilityTypeId = utilityType.Id,
                TariffId = tariff.Id,
                MeterNumber = "M12345",
                Status = "Active"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateConnectionAsync(dto, "admin@example.com"));
            Assert.Contains("Meter number already exists", ex.Message);
        }

        [Fact]
        public async Task CreateConnectionAsync_WithInactiveUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = await CreateUserAsync(status: "Inactive");
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var tariff = await CreateTariffAsync(utilityType.Id);

            var dto = new ConnectionDto
            {
                UserId = user.Id,
                UtilityTypeId = utilityType.Id,
                TariffId = tariff.Id,
                MeterNumber = "M12345",
                Status = "Active"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateConnectionAsync(dto, "admin@example.com"));
            Assert.Contains("Cannot create connection for an inactive user", ex.Message);
        }

        [Fact]
        public async Task DeleteConnectionAsync_WithExistingBills_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var bill = await CreateBillAsync(connection.Id, 100m, 500m, 90m, 590m);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteConnectionAsync(connection.Id, "admin@example.com"));
            Assert.Contains("Cannot delete connection with existing bills", ex.Message);
        }

    }
}

