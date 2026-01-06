using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.UtilityType;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class UtilityTypeControllerTests : BaseControllerTest
    {
        public UtilityTypeControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateUtilityType_ShouldCreateUtilityType_WhenAdminCreates()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var adminClient = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            var utilityTypeDto = new UtilityTypeDto
            {
                Name = "Gas",
                Description = "Natural Gas Utility",
                Status = "Enabled"
            };
            var response = await adminClient.PostAsJsonAsync("/api/utilitytype", utilityTypeDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdUtilityType = await DeserializeResponseAsync<UtilityTypeDto>(response);
            Assert.NotNull(createdUtilityType);
            Assert.Equal("Gas", createdUtilityType.Name);
            Assert.Equal("Natural Gas Utility", createdUtilityType.Description);
            Assert.Equal("Enabled", createdUtilityType.Status);
        }

        [Fact]
        public async Task GetUtilityTypes_ShouldReturnList_WhenAuthenticated()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var utilityType1 = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var utilityType2 = await CreateTestUtilityTypeAsync(factory, "Water");
            var consumerClient = CreateAuthenticatedClient(factory, consumer.Id, consumer.Email!, consumer.FullName, "Consumer");
            var response = await consumerClient.GetAsync("/api/utilitytype");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var utilityTypes = await DeserializeResponseAsync<List<UtilityTypeDto>>(response);
            Assert.NotNull(utilityTypes);
            Assert.True(utilityTypes.Count >= 2);
            Assert.Contains(utilityTypes, u => u.Name == "Electricity");
            Assert.Contains(utilityTypes, u => u.Name == "Water");
        }
    }
}
