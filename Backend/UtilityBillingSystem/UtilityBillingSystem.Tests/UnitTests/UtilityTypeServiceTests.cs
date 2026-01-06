using UtilityBillingSystem.Models.Dto.UtilityType;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class UtilityTypeServiceTests : BaseServiceTest
    {
        private readonly UtilityTypeService _service;

        public UtilityTypeServiceTests() : base()
        {
            _service = new UtilityTypeService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task CreateUtilityTypeAsync_WithInactiveBillingCycle_ThrowsInvalidOperationException()
        {
            // Arrange
            var billingCycle = await CreateBillingCycleAsync(isActive: false);
            var dto = new UtilityTypeDto
            {
                Name = "Internet",
                BillingCycleId = billingCycle.Id
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateUtilityTypeAsync(dto, "admin@example.com"));
            Assert.Contains("Cannot assign an inactive billing cycle", ex.Message);
        }

        [Fact]
        public async Task UpdateUtilityTypeAsync_DisablingWithActiveConnections_ThrowsInvalidOperationException()
        {
            // Arrange
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var connection = await CreateFullConnectionAsync(utilityTypeId: utilityType.Id, status: "Active");

            var dto = new UtilityTypeDto
            {
                Id = utilityType.Id,
                Name = utilityType.Name,
                Status = "Disabled"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateUtilityTypeAsync(utilityType.Id, dto, "admin@example.com"));
            Assert.Contains("Cannot disable utility type with active connections", ex.Message);
        }

        [Fact]
        public async Task DeleteUtilityTypeAsync_WithExistingConnections_ThrowsInvalidOperationException()
        {
            // Arrange
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var connection = await CreateFullConnectionAsync(utilityTypeId: utilityType.Id);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteUtilityTypeAsync(utilityType.Id, "admin@example.com"));
            Assert.Contains("Cannot delete utility type with existing connections", ex.Message);
        }

    }
}

