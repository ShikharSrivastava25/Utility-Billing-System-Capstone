using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UtilityBillingSystem.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.BillingCycle;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class BillingCycleControllerTests : BaseControllerTest
    {
        public BillingCycleControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateBillingCycle_ShouldCreateCycle_WhenAdminCreates()
        {
            var (factory, admin, adminClient) = await SetupAdminAsync();
            var billingCycleDto = new BillingCycleDto
            {
                Name = "Monthly Cycle",
                GenerationDay = 15,
                DueDateOffset = 30,
                GracePeriod = 7,
                IsActive = true
            };
            var response = await adminClient.PostAsJsonAsync("/api/billingcycle", billingCycleDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdCycle = await DeserializeResponseAsync<BillingCycleDto>(response);
            Assert.NotNull(createdCycle);
            Assert.Equal("Monthly Cycle", createdCycle.Name);
            Assert.Equal(15, createdCycle.GenerationDay);
            Assert.Equal(30, createdCycle.DueDateOffset);
            Assert.Equal(7, createdCycle.GracePeriod);
            Assert.True(createdCycle.IsActive);
        }

        [Fact]
        public async Task GetBillingCycles_ShouldReturnList_WhenBillingOfficerAuthenticated()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var billingOfficer = await CreateTestUserAsync(factory, "billing@test.com", "Billing@123", "Billing Officer", "Billing Officer");
            var cycle1 = await CreateTestBillingCycleAsync(factory, "Monthly Cycle", 15, 30, 7);
            var cycle2 = await CreateTestBillingCycleAsync(factory, "Bi-Monthly Cycle", 1, 60, 10);
            var billingOfficerClient = CreateAuthenticatedClient(factory, billingOfficer.Id, billingOfficer.Email!, billingOfficer.FullName, "Billing Officer");
            var response = await billingOfficerClient.GetAsync("/api/billingcycle");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cycles = await DeserializeResponseAsync<List<BillingCycleDto>>(response);
            Assert.NotNull(cycles);
            Assert.True(cycles.Count >= 2);
            Assert.Contains(cycles, c => c.Name == "Monthly Cycle");
            Assert.Contains(cycles, c => c.Name == "Bi-Monthly Cycle");
        }

        private async Task<(WebApplicationFactory<Program> Factory, User Admin, HttpClient AdminClient)> SetupAdminAsync()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var adminClient = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            return (factory, admin, adminClient);
        }
    }
}
