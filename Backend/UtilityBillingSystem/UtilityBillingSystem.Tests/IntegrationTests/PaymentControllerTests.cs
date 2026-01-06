using System.Net;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class PaymentControllerTests : BaseControllerTest
    {
        public PaymentControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetMyPayments_ShouldReturnPaymentHistory_WhenConsumerAuthenticated()
        {
            var (factory, consumer, _, bill, _) = await SetupPaymentTestDataAsync();
            var client = CreateAuthenticatedClient(factory, consumer.Id, consumer.Email!, consumer.FullName, "Consumer");
            var response = await client.GetAsync("/api/payment/my-payments");
           
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payments = await DeserializeResponseAsync<List<PaymentHistoryDto>>(response);
            Assert.NotNull(payments);
            
            Assert.Single(payments);
            Assert.Equal(3068.0m, payments[0].Amount);
            Assert.Equal("Cash", payments[0].PaymentMethod);
        }
        private async Task<(WebApplicationFactory<Program> Factory, User Consumer, Connection Connection, Bill Bill, UtilityType UtilityType)> SetupPaymentTestDataAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff", 5.0m, 100.0m, 18.0m);
            var connection = await CreateTestConnectionAsync(factory, consumer.Id, utilityType.Id, tariff.Id, "MTR-001");
            var reading = await CreateTestMeterReadingAsync(factory, connection.Id, 1000, 1500, "billing@test.com", null, "Billed");
            var bill = await CreateTestBillAsync(factory, connection.Id, reading.Id, 500, 2600, 468, 3068, "Paid");
            await CreateTestPaymentAsync(factory, bill.Id, 3068, "Cash");
            return (factory, consumer, connection, bill, utilityType);
        }
    }
}
