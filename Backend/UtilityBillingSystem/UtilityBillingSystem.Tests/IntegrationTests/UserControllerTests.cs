using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Tests.Base;
using Microsoft.Extensions.DependencyInjection;
namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class UserControllerTests : BaseControllerTest
    {
        public UserControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task CreateUser_ShouldCreateUser_WhenValidDataProvided()
        {
            var (factory, adminClient, _) = await SetupTestWithAdminAsync();
            var createUserDto = new CreateUserDto
            {
                Name = "New User",
                Email = "newuser@test.com",
                Password = "NewUser@123",
                Role = "Billing Officer",
                Status = "Active"
            };
            var response = await adminClient.PostAsJsonAsync("/api/user", createUserDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdUser = await DeserializeResponseAsync<UserDto>(response);
            Assert.NotNull(createdUser);
            Assert.Equal("newuser@test.com", createdUser.Email);
            Assert.Equal("New User", createdUser.Name);
            Assert.Equal("Billing Officer", createdUser.Role);
        }

        [Fact]
        public async Task GetMyProfile_ShouldReturnConsumerProfile_WhenConsumerAuthenticated()
        {
            var (factory, consumerClient, consumer) = await SetupTestWithConsumerAsync();
            var response = await consumerClient.GetAsync("/api/user/profile");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var profile = await DeserializeResponseAsync<UserDto>(response);
            Assert.NotNull(profile);
            Assert.Equal(consumer.Id, profile.Id);
            Assert.Equal(consumer.Email, profile.Email);
            Assert.Equal(consumer.FullName, profile.Name);
            Assert.Equal("Consumer", profile.Role);
        }

        private async Task<(WebApplicationFactory<Program> Factory, HttpClient AdminClient, User Admin)> SetupTestWithAdminAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var admin = await CreateTestUserAsync(factory, "admin@test.com", "Admin@123", "Admin User", "Admin");
            var adminClient = CreateAuthenticatedClient(factory, admin.Id, admin.Email!, admin.FullName, "Admin");
            return (factory, adminClient, admin);
        }
        private async Task<(WebApplicationFactory<Program> Factory, HttpClient ConsumerClient, User Consumer)> SetupTestWithConsumerAsync()
        {
            var (_, factory) = CreateClientWithInMemoryDbAndFactory();
            var consumer = await CreateTestUserAsync(factory, "consumer@test.com", "Consumer@123", "Consumer User", "Consumer");
            var consumerClient = CreateAuthenticatedClient(factory, consumer.Id, consumer.Email!, consumer.FullName, "Consumer");
            return (factory, consumerClient, consumer);
        }
    }
}
