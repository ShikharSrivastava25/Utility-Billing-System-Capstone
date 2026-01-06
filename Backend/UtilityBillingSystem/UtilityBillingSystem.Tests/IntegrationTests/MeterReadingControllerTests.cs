using System.Net;
using System.Net.Http.Json;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.MeterReading;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class MeterReadingControllerTests : BaseControllerTest
    {
        public MeterReadingControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateMeterReading_ShouldCalculateConsumption_WhenValidReadingProvided()
        {
            var (factory, billingOfficer, _, billingCycle, _, _, connection) = await SetupTestDataAsync();
            var client = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            var readingDto = new MeterReadingRequestDto
            {
                ConnectionId = connection.Id,
                CurrentReading = 1500.0m,
                ReadingDate = DateTime.UtcNow
            };
            var response = await client.PostAsJsonAsync("/api/meterreading", readingDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdReading = await DeserializeResponseAsync<MeterReadingResponseDto>(response);
            Assert.NotNull(createdReading);
            Assert.Equal(connection.Id, createdReading.ConnectionId);
            Assert.Equal(1500.0m, createdReading.CurrentReading);
            Assert.Equal(1500.0m, createdReading.Consumption);
            Assert.Equal("ReadyForBilling", createdReading.Status);
        }

        [Fact]
        public async Task CreateMeterReading_ShouldReturnBadRequest_WhenCurrentReadingLessThanPreviousReading()
        {
            var (factory, billingOfficer, _, billingCycle, _, _, connection) = await SetupTestDataAsync();
            await CreateTestMeterReadingAsync(factory, connection.Id, 0, 1000, "billing@test.com", billingCycle.Id, "Billed");
            var client = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            var readingDto = new MeterReadingRequestDto
            {
                ConnectionId = connection.Id,
                CurrentReading = 500.0m,
                ReadingDate = DateTime.UtcNow
            };
            var response = await client.PostAsJsonAsync("/api/meterreading", readingDto);
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("must be greater than or equal to previous reading", content);
        }

        private async Task<(WebApplicationFactory<Program> Factory, User BillingOfficer, User Consumer, BillingCycle BillingCycle, UtilityType UtilityType, Tariff Tariff, Connection Connection)> SetupTestDataAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var billingOfficer = await CreateTestUserAsync(factory, "billing@test.com", "Billing@123", "Billing Officer", "Billing Officer");
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var billingCycle = await CreateTestBillingCycleAsync(factory, "Monthly", 15, 30, 7);
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity", billingCycle.Id);
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff");
            var connection = await CreateTestConnectionAsync(factory, consumer.Id, utilityType.Id, tariff.Id, "MTR-001");
            return (factory, billingOfficer, consumer, billingCycle, utilityType, tariff, connection);
        }
    }
}
