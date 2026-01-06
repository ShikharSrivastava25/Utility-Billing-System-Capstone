using UtilityBillingSystem.Models.Dto.MeterReading;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class MeterReadingServiceTests : BaseServiceTest
    {
        private readonly MeterReadingService _service;

        public MeterReadingServiceTests() : base()
        {
            _service = new MeterReadingService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task CreateMeterReadingAsync_WithCurrentLessThanPrevious_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var previousReading = await CreateMeterReadingAsync(connection.Id, 100m, 500m, status: "Billed");

            var dto = new MeterReadingRequestDto
            {
                ConnectionId = connection.Id,
                CurrentReading = 300m,
                ReadingDate = DateTime.UtcNow
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateMeterReadingAsync(dto, "test@example.com"));
            Assert.Contains("Current reading", ex.Message);
            Assert.Contains("must be greater than or equal to previous reading", ex.Message);
        }

        [Fact]
        public async Task CreateMeterReadingAsync_WithInactiveConnection_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync(status: "Inactive");
            var dto = new MeterReadingRequestDto
            {
                ConnectionId = connection.Id,
                CurrentReading = 500m,
                ReadingDate = DateTime.UtcNow
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateMeterReadingAsync(dto, "test@example.com"));
            Assert.Contains("Connection is not active", ex.Message);
        }

        [Fact]
        public async Task UpdateMeterReadingAsync_WithBilledReading_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var reading = await CreateMeterReadingAsync(connection.Id, 100m, 200m, status: "Billed");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateMeterReadingAsync(reading.Id, 300m, "test@example.com"));
            Assert.Contains("Cannot edit a reading that has already been billed", ex.Message);
        }

    }
}

