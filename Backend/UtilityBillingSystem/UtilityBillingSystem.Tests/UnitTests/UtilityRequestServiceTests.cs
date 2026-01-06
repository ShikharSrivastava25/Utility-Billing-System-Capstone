using Moq;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Models.Dto.UtilityRequest;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class UtilityRequestServiceTests : BaseServiceTest
    {
        private readonly Mock<IConnectionService> _mockConnectionService;
        private readonly UtilityRequestService _service;

        public UtilityRequestServiceTests() : base()
        {
            _mockConnectionService = new Mock<IConnectionService>();
            _service = new UtilityRequestService(Context, MockAuditLogService.Object, _mockConnectionService.Object, Mapper);
        }

        [Fact]
        public async Task ApproveRequestAsync_WithValidRequest_CreatesConnection()
        {
            // Arrange
            var user = await CreateUserAsync();
            var utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            var tariff = await CreateTariffAsync(utilityType.Id);
            var request = await CreateUtilityRequestAsync(user.Id, utilityType.Id, status: "Pending");

            var approveDto = new ApproveRequestDto
            {
                TariffId = tariff.Id,
                MeterNumber = "M12345"
            };

            var connectionDto = new ConnectionDto
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                UtilityTypeId = utilityType.Id,
                TariffId = tariff.Id,
                MeterNumber = "M12345",
                Status = "Active"
            };

            _mockConnectionService
                .Setup(x => x.CreateConnectionAsync(It.IsAny<ConnectionDto>(), It.IsAny<string>()))
                .ReturnsAsync(connectionDto);

            // Act
            var result = await _service.ApproveRequestAsync(request.Id, approveDto, "admin@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("M12345", result.MeterNumber);
            Assert.Equal(user.Id, result.UserId);

            // Verify request status updated
            await Context.Entry(request).ReloadAsync();
            Assert.Equal("Approved", request.Status);
        }

        [Fact]
        public async Task ApproveRequestAsync_WithDisabledUtility_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = await CreateUserAsync();
            var utilityType = await CreateUtilityTypeAsync(status: "Disabled");
            var tariff = await CreateTariffAsync(utilityType.Id);
            var request = await CreateUtilityRequestAsync(user.Id, utilityType.Id, status: "Pending");

            var approveDto = new ApproveRequestDto
            {
                TariffId = tariff.Id,
                MeterNumber = "M12345"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveRequestAsync(request.Id, approveDto, "admin@example.com"));
            Assert.Contains("Cannot approve request for a disabled utility type", ex.Message);
        }

    }
}

