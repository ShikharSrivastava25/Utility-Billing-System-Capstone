using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Tests.Base;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class ConnectionControllerTests : BaseControllerTest
    {
        public ConnectionControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateConnection_ShouldCreateConnection_WhenAdminCreates()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff");
            var adminClient = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            var connectionDto = new ConnectionDto
            {
                UserId = consumer.Id,
                UtilityTypeId = utilityType.Id,
                TariffId = tariff.Id,
                MeterNumber = "MTR-001",
                Status = "Active"
            };
            var response = await adminClient.PostAsJsonAsync("/api/connection", connectionDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdConnection = await DeserializeResponseAsync<ConnectionDto>(response);
            Assert.NotNull(createdConnection);
            Assert.Equal("MTR-001", createdConnection.MeterNumber);
            Assert.Equal(consumer.Id, createdConnection.UserId);
            Assert.Equal("Active", createdConnection.Status);
        }

        [Fact]
        public async Task GetMyConnections_ShouldReturnOnlyConsumerConnections_WhenConsumerAuthenticated()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var consumer1 = await CreateTestUserAsync(factory, "consumer1@test.com", "Consumer@123", "Consumer 1", "Consumer");
            var consumer2 = await CreateTestUserAsync(factory, "consumer2@test.com", "Consumer@123", "Consumer 2", "Consumer");
            var utilityType = await CreateTestUtilityTypeAsync(factory, "Electricity");
            var tariff = await CreateTestTariffAsync(factory, utilityType.Id, "Standard Tariff");
            var connection1 = await CreateTestConnectionAsync(factory, consumer1.Id, utilityType.Id, tariff.Id, "MTR-001");
            await CreateTestConnectionAsync(factory, consumer2.Id, utilityType.Id, tariff.Id, "MTR-002");
            var consumer1Client = CreateAuthenticatedClient(factory, consumer1.Id, consumer1.Email!, consumer1.FullName, "Consumer");
            var response = await consumer1Client.GetAsync("/api/connection/my-connections");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var connections = await DeserializeResponseAsync<List<ConnectionDto>>(response);
            Assert.NotNull(connections);
            Assert.Single(connections);
            Assert.Equal(connection1.Id, connections[0].Id);
        }
    }
}
