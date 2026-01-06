using System.Net;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Models.Dto.Consumer;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class ReportControllerTests : BaseControllerTest
    {
        public ReportControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetReportSummary_ShouldReturnSummary_WhenAdminAuthenticated()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var consumer1 = await CreateTestUserAsync(factory, "consumer1@test.com", "Consumer@123", "Consumer 1", "Consumer");
            var consumer2 = await CreateTestUserAsync(factory, "consumer2@test.com", "Consumer@123", "Consumer 2", "Consumer");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff", 5.0m, 100.0m, 18.0m);
            var connection1 = await CreateTestConnectionAsync(factory, consumer1.Id, utilityType.Id, tariff.Id, "MTR-001");
            var connection2 = await CreateTestConnectionAsync(factory, consumer2.Id, utilityType.Id, tariff.Id, "MTR-002");
            var reading1 = await CreateTestMeterReadingAsync(factory, connection1.Id, 1000, 1500, "billing@test.com", null, "Billed");
            var reading2 = await CreateTestMeterReadingAsync(factory, connection2.Id, 2000, 2500, "billing@test.com", null, "Billed");
            var bill1 = await CreateTestBillAsync(factory, connection1.Id, reading1.Id, 500, 2600, 468, 3068, "Paid");
            var bill2 = await CreateTestBillAsync(factory, connection2.Id, reading2.Id, 500, 2600, 468, 3068, "Due");
            await CreateTestPaymentAsync(factory, bill1.Id, 3068, "Cash");
            var client = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            var response = await client.GetAsync("/api/report/summary");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await DeserializeResponseAsync<ReportSummaryDto>(response);
            Assert.NotNull(summary);
            
            Assert.True(summary.TotalConsumers >= 2);
        }

    }
}
