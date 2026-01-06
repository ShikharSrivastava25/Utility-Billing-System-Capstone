using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using UtilityBillingSystem.Tests.Base;
using Microsoft.Extensions.DependencyInjection;

namespace UtilityBillingSystem.Tests.IntegrationTests
{
    public class AuthControllerTests : BaseControllerTest
    {
        public AuthControllerTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task Register_ShouldCreateNewConsumer_WhenValidDataProvided()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var registerDto = new RegisterRequestDto
            {
                Email = "testconsumer@example.com",
                Password = "Test@1234",
                Name = "Test Consumer"
            };
            var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("registered successfully", content);
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(registerDto.Email);
            Assert.NotNull(user);
            Assert.Equal("Test Consumer", user.FullName);
            Assert.True(await userManager.IsInRoleAsync(user, "Consumer"));
        }

        [Fact]
        public async Task Login_ShouldReturnJwtToken_WhenValidCredentials()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var email = "testuser@example.com";
            var password = "Test@1234";
            var user = await CreateTestUserAsync(factory, email, password, "Test User", "Consumer");
            var loginDto = new LoginRequestDto
            {
                Email = email,
                Password = password
            };
            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var loginResponse = await DeserializeResponseAsync<LoginResponseDto>(response);
            Assert.NotNull(loginResponse);
            Assert.NotNull(loginResponse.Token);
            Assert.NotEmpty(loginResponse.Token);
            Assert.NotNull(loginResponse.User);
            Assert.Equal(email, loginResponse.User.Email);
            Assert.Equal("Consumer", loginResponse.User.Role);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenInvalidCredentials()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var email = "testuser@example.com";
            var password = "Test@1234";
            await CreateTestUserAsync(factory, email, password, "Test User", "Consumer");
            var loginDto = new LoginRequestDto
            {
                Email = email,
                Password = "WrongPassword123"
            };
            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GenerateBill_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            var (client, factory) = CreateClientWithInMemoryDbAndFactory();
            var generateBillDto = new { ReadingId = Guid.NewGuid().ToString() };
            var response = await client.PostAsJsonAsync("/api/bill/generate", generateBillDto);
            
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
