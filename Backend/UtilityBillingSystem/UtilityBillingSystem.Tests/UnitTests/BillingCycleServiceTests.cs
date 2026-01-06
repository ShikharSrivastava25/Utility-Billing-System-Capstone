using UtilityBillingSystem.Models.Dto.BillingCycle;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class BillingCycleServiceTests : BaseServiceTest
    {
        private readonly BillingCycleService _service;

        public BillingCycleServiceTests() : base()
        {
            _service = new BillingCycleService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task UpdateBillingCycleAsync_WithInvalidGenerationDay_ThrowsArgumentException()
        {
            // Arrange
            var cycle = await CreateBillingCycleAsync();
            var dto = new BillingCycleDto
            {
                Id = cycle.Id,
                Name = cycle.Name,
                GenerationDay = 35, // Invalid: > 28
                DueDateOffset = cycle.DueDateOffset,
                GracePeriod = cycle.GracePeriod,
                IsActive = cycle.IsActive
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateBillingCycleAsync(cycle.Id, dto, "admin@example.com"));
            Assert.Contains("Generation day must be between 1 and 28", ex.Message);
        }

        [Fact]
        public async Task DeleteBillingCycleAsync_WithAssignedUtilityTypes_ThrowsInvalidOperationException()
        {
            // Arrange
            var cycle = await CreateBillingCycleAsync();
            var utilityType = await CreateUtilityTypeAsync(billingCycleId: cycle.Id);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteBillingCycleAsync(cycle.Id, "admin@example.com"));
            Assert.Contains("Cannot delete billing cycle that is assigned to utility types", ex.Message);
        }

    }
}

