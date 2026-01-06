using UtilityBillingSystem.Models.Dto.Tariff;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class TariffServiceTests : BaseServiceTest
    {
        private readonly TariffService _service;

        public TariffServiceTests() : base()
        {
            _service = new TariffService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task CreateTariffAsync_WithInvalidUtilityType_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new TariffDto
            {
                Name = "Test",
                UtilityTypeId = Guid.NewGuid().ToString(),
                BaseRate = 5.0m,
                FixedCharge = 100.0m,
                TaxPercentage = 18.0m
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateTariffAsync(dto, "admin@example.com"));
            Assert.Contains("Utility type not found", ex.Message);
        }

        [Fact]
        public async Task DeleteTariffAsync_WithExistingConnections_ThrowsInvalidOperationException()
        {
            // Arrange
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var tariff = await CreateTariffAsync(utilityType.Id);
            var connection = await CreateFullConnectionAsync(utilityTypeId: utilityType.Id, tariffId: tariff.Id);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteTariffAsync(tariff.Id, "admin@example.com"));
            Assert.Contains("Cannot delete tariff with existing connections", ex.Message);
        }

    }
}

