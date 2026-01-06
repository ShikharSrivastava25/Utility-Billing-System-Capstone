using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class AuthorizationTests : BaseControllerTest
    {
        public AuthorizationTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateUser_ShouldReturnForbidden_WhenConsumerTriesToAccess()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var consumerClient = CreateAuthenticatedClient(factory, consumer.Id, consumer.Email!, consumer.FullName, "Consumer");
            var createUserDto = new CreateUserDto
            {
                Name = "New User",
                Email = "newuser@test.com",
                Password = "NewUser@123",
                Role = "Consumer",
                Status = "Active"
            };
            var response = await consumerClient.PostAsJsonAsync("/api/user", createUserDto);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOutstandingBills_ShouldReturnForbidden_WhenBillingOfficerTriesToAccess()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var billingOfficer = await CreateTestUserAsync(factory, "billing@test.com", "Billing@123", "Billing Officer", "Billing Officer");
            var billingOfficerClient = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            var response = await billingOfficerClient.GetAsync("/api/bill/outstanding");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GenerateBill_ShouldReturnForbidden_WhenAccountOfficerTriesToAccess()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var accountOfficer = await CreateTestUserAsync(factory, "account@test.com", "Account@123", "Account Officer", "Account Officer");
            var accountOfficerClient = CreateAuthenticatedClient(factory, accountOfficer.Id, accountOfficer.Email!, accountOfficer.FullName, "Account Officer");
            var generateBillDto = new { ReadingId = Guid.NewGuid().ToString() };
            var response = await accountOfficerClient.PostAsJsonAsync("/api/bill/generate", generateBillDto);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
