using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Bill;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Tests.Base;

namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class BillControllerTests : BaseControllerTest
    {
        public BillControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GenerateBill_ShouldCreateBill_WhenValidReadingProvided()
        {
            var (factory, billingOfficer, consumer, billingCycle, utilityType, tariff, connection) = await SetupTestDataAsync();
            var reading = await CreateTestMeterReadingAsync(factory, connection.Id, 1000, 1500, "billing@test.com", billingCycle.Id);
            var billingOfficerClient = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            var generateBillDto = new GenerateBillRequestDto
            {
                ReadingId = reading.Id
            };
            var response = await billingOfficerClient.PostAsJsonAsync("/api/bill/generate", generateBillDto);
           
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var bill = await DeserializeResponseAsync<BillDto>(response);
            Assert.NotNull(bill);
            Assert.Equal(connection.Id, bill.ConnectionId);
            Assert.Equal(500.0m, bill.Consumption);
            Assert.Equal(3068.0m, bill.TotalAmount);
            Assert.Equal("Generated", bill.Status);
        }

        [Fact]
        public async Task PayBill_ShouldUpdateBillStatus_WhenPaymentRecorded()
        {
            var (factory, billingOfficer, consumer, billingCycle, utilityType, tariff, connection) = await SetupTestDataAsync();
            var reading = await CreateTestMeterReadingAsync(factory, connection.Id, 1000, 1500, "billing@test.com", billingCycle.Id);
            var bill = await CreateTestBillAsync(factory, connection.Id, reading.Id, 500, 2600, 468, 3068);
            var consumerClient = CreateAuthenticatedClient(factory, consumer.Id, consumer.Email!, consumer.FullName, "Consumer");
            var paymentDto = new CreatePaymentDto
            {
                PaymentMethod = "Cash",
                ReceiptNumber = "RCP-001"
            };
            var response = await consumerClient.PostAsJsonAsync($"/api/bill/{bill.Id}/pay", paymentDto);
           
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payment = await DeserializeResponseAsync<PaymentDto>(response);
            Assert.NotNull(payment);
            Assert.Equal(bill.TotalAmount, payment.Amount);
            Assert.Equal("Completed", payment.Status);
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedBill = await context.Bills.FindAsync(bill.Id);
            Assert.NotNull(updatedBill);
            Assert.Equal("Paid", updatedBill.Status);
        }

        private async Task<(WebApplicationFactory<Program> Factory, User BillingOfficer, User Consumer, BillingCycle BillingCycle, UtilityType UtilityType, Tariff Tariff, Connection Connection)> SetupTestDataAsync(decimal baseRate = 5.0m, decimal fixedCharge = 100.0m, decimal taxPercentage = 18.0m)
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var billingOfficer = await CreateTestUserAsync(factory, "billing@test.com", "Billing@123", "Billing Officer", "Billing Officer");
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var billingCycle = await CreateTestBillingCycleAsync(factory, "Monthly", 15, 30, 7);
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity", billingCycle.Id);
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff", baseRate, fixedCharge, taxPercentage);
            var connection = await CreateTestConnectionAsync(factory, consumer.Id, utilityType.Id, tariff.Id, "MTR-001");
            return (factory, billingOfficer, consumer, billingCycle, utilityType, tariff, connection);
        }
    }
}
