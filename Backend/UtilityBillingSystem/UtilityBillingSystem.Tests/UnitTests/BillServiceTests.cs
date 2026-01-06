using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class BillServiceTests : BaseServiceTest
    {
        private readonly BillService _service;

        public BillServiceTests() : base()
        {
            _service = new BillService(Context, MockAuditLogService.Object);
        }

        [Fact]
        public async Task GenerateBillAsync_WithValidReading_GeneratesBill()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var reading = await CreateMeterReadingAsync(
                connection.Id, 
                0m, 
                100m, 
                status: "ReadyForBilling",
                tariffId: connection.TariffId,
                billingCycleId: connection.UtilityType.BillingCycleId);

            // Act
            var result = await _service.GenerateBillAsync(reading.Id, "test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(connection.Id, result.ConnectionId);
            Assert.Equal(100m, result.Consumption);
            Assert.Equal("Generated", result.Status);
            Assert.True(result.TotalAmount > 0);

            // Verify reading status updated
            await Context.Entry(reading).ReloadAsync();
            Assert.Equal("Billed", reading.Status);
        }

        [Fact]
        public async Task GenerateBillAsync_WithAlreadyBilledReading_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var reading = await CreateMeterReadingAsync(connection.Id, 0m, 100m, status: "Billed");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateBillAsync(reading.Id, "test@example.com"));
            Assert.Contains("Reading is not ready for billing", ex.Message);
        }

    }
}

