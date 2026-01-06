using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Tariff;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class TariffControllerTests : BaseControllerTest
    {
        public TariffControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateTariff_ShouldCreateTariff_WhenAdminCreates()
        {
            var (factory, adminClient, utilityType) = await SetupTestWithAdminAsync();
            var tariffDto = new TariffDto
            {
                Name = "Premium Tariff",
                UtilityTypeId = utilityType.Id,
                BaseRate = 8.0m,
                FixedCharge = 150.0m,
                TaxPercentage = 18.0m,
                IsActive = true
            };
            var response = await adminClient.PostAsJsonAsync("/api/tariff", tariffDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdTariff = await DeserializeResponseAsync<TariffDto>(response);
            Assert.NotNull(createdTariff);
            Assert.Equal("Premium Tariff", createdTariff.Name);
            Assert.Equal(8.0m, createdTariff.BaseRate);
            Assert.Equal(150.0m, createdTariff.FixedCharge);
            Assert.Equal(18.0m, createdTariff.TaxPercentage);
            Assert.Equal(utilityType.Id, createdTariff.UtilityTypeId);
        }

        [Fact]
        public async Task GetTariffs_ShouldReturnList_WhenBillingOfficerAuthenticated()
        {
            var (factory, billingOfficerClient, utilityType) = await SetupTestWithBillingOfficerAsync();
            var tariff1 = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff", 5.0m, 100.0m, 18.0m);
            var tariff2 = await CreateTestTariffAsync(factory, utilityType.Id, "Premium Tariff", 8.0m, 150.0m, 18.0m);
            var response = await billingOfficerClient.GetAsync("/api/tariff");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tariffs = await DeserializeResponseAsync<List<TariffDto>>(response);
            Assert.NotNull(tariffs);
            Assert.True(tariffs.Count >= 2);
            Assert.Contains(tariffs, t => t.Name == "Standard Tariff");
            Assert.Contains(tariffs, t => t.Name == "Premium Tariff");
        }

        private async Task<(WebApplicationFactory<Program> Factory, HttpClient AdminClient, UtilityType UtilityType)> SetupTestWithAdminAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var adminClient = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            return (factory, adminClient, utilityType);
        }
        private async Task<(WebApplicationFactory<Program> Factory, HttpClient BillingOfficerClient, UtilityType UtilityType)> SetupTestWithBillingOfficerAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var billingOfficer = await CreateTestUserAsync(factory, "billing@test.com", "Billing@123", "Billing Officer", "Billing Officer");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var billingOfficerClient = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            return (factory, billingOfficerClient, utilityType);
        }
    }
}
