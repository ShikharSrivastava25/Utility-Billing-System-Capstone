using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;

namespace UtilityBillingSystem.Tests.Base
{
    public class BaseControllerTest : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly WebApplicationFactory<Program> _factory;
        private const string TestJwtKey = "THIS_IS_A_VERY_SECURE_KEY_123456";
        private const string TestJwtIssuer = "Chubb";
        private const string TestJwtAudience = "Market Users";

        public BaseControllerTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        protected HttpClient CreateClientWithInMemoryDb()
        {
            var (client, _) = CreateClientWithInMemoryDbAndFactory();
            return client;
        }

        protected async Task<(HttpClient Client, WebApplicationFactory<Program> Factory)> CreateClientWithInMemoryDbAndFactoryAsync()
        {
            var dbName = Guid.NewGuid().ToString(); // Unique per test
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Jwt:Key", TestJwtKey },
                        { "Jwt:Issuer", TestJwtIssuer },
                        { "Jwt:Audience", TestJwtAudience }
                    });
                });
                builder.ConfigureServices(services =>
                {
                    // Remove the real DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Register InMemory database for testing
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

            // Seed roles after factory is created and client is built
            var client = factory.CreateClient();
            await SeedRolesAsync(factory);

            return (client, factory);
        }

        protected (HttpClient Client, WebApplicationFactory<Program> Factory) CreateClientWithInMemoryDbAndFactory()
        {
            return CreateClientWithInMemoryDbAndFactoryAsync().GetAwaiter().GetResult();
        }

        private async Task SeedRolesAsync(WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            string[] roleNames = { "Admin", "Billing Officer", "Account Officer", "Consumer" };
            
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        protected async Task<User> CreateTestUserAsync(WebApplicationFactory<Program> factory, string email, string password, string fullName, string role)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure role exists
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create user
            var user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Status = "Active"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign role
            await userManager.AddToRoleAsync(user, role);

            return user;
        }

        protected string GenerateJwtToken(string userId, string email, string fullName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: TestJwtIssuer,
                audience: TestJwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected HttpClient CreateAuthenticatedClient(string userId, string email, string fullName, string role)
        {
            var client = CreateClientWithInMemoryDb();
            var token = GenerateJwtToken(userId, email, fullName, role);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory, string userId, string email, string fullName, string role)
        {
            var client = factory.CreateClient();
            var token = GenerateJwtToken(userId, email, fullName, role);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        protected async Task<UtilityType> CreateTestUtilityTypeAsync(WebApplicationFactory<Program> factory, string name, string? billingCycleId = null)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var utilityType = new UtilityType
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = $"Test {name}",
                Status = "Enabled",
                BillingCycleId = billingCycleId
            };
            context.UtilityTypes.Add(utilityType);
            await context.SaveChangesAsync();
            return utilityType;
        }

        protected async Task<Tariff> CreateTestTariffAsync(WebApplicationFactory<Program> factory, string utilityTypeId, string name, decimal baseRate = 5.0m, decimal fixedCharge = 100.0m, decimal taxPercentage = 18.0m)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tariff = new Tariff
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                UtilityTypeId = utilityTypeId,
                BaseRate = baseRate,
                FixedCharge = fixedCharge,
                TaxPercentage = taxPercentage,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            context.Tariffs.Add(tariff);
            await context.SaveChangesAsync();
            return tariff;
        }

        protected async Task<Connection> CreateTestConnectionAsync(WebApplicationFactory<Program> factory, string userId, string utilityTypeId, string tariffId, string meterNumber)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = new Connection
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UtilityTypeId = utilityTypeId,
                TariffId = tariffId,
                MeterNumber = meterNumber,
                Status = "Active"
            };
            context.Connections.Add(connection);
            await context.SaveChangesAsync();
            return connection;
        }

        protected async Task<MeterReading> CreateTestMeterReadingAsync(WebApplicationFactory<Program> factory, string connectionId, decimal previousReading, decimal currentReading, string recordedBy, string? billingCycleId = null, string status = "ReadyForBilling", DateTime? readingDate = null)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Get TariffId from connection
            var connection = await context.Connections.FindAsync(connectionId);
            if (connection == null)
            {
                throw new InvalidOperationException($"Connection {connectionId} not found. Cannot create meter reading without TariffId.");
            }
            
            var reading = new MeterReading
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                PreviousReading = previousReading,
                CurrentReading = currentReading,
                Consumption = currentReading - previousReading,
                ReadingDate = readingDate ?? DateTime.UtcNow,
                Status = status,
                RecordedBy = recordedBy,
                BillingCycleId = billingCycleId ?? Guid.NewGuid().ToString(),
                TariffId = connection.TariffId,
                CreatedAt = DateTime.UtcNow
            };
            context.MeterReadings.Add(reading);
            await context.SaveChangesAsync();
            return reading;
        }

        protected async Task<Bill> CreateTestBillAsync(WebApplicationFactory<Program> factory, string connectionId, string readingId, decimal consumption, decimal baseAmount, decimal taxAmount, decimal totalAmount, string status = "Generated", DateTime? generationDate = null, DateTime? dueDate = null)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bill = new Bill
            {
                Id = Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                BillingPeriod = DateTime.UtcNow.ToString("MMMM yyyy"),
                GenerationDate = generationDate ?? DateTime.UtcNow,
                DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
                PreviousReading = 0,
                CurrentReading = 0,
                Consumption = consumption,
                BaseAmount = baseAmount,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = status
            };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();
            return bill;
        }

        protected async Task<Payment> CreateTestPaymentAsync(WebApplicationFactory<Program> factory, string billId, decimal amount, string paymentMethod, DateTime? paymentDate = null)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var payment = new Payment
            {
                Id = Guid.NewGuid().ToString(),
                BillId = billId,
                PaymentDate = paymentDate ?? DateTime.UtcNow,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Status = "Completed"
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            return payment;
        }

        protected async Task<BillingCycle> CreateTestBillingCycleAsync(WebApplicationFactory<Program> factory, string name, int generationDay, int dueDateOffset, int gracePeriod)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var billingCycle = new BillingCycle
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                GenerationDay = generationDay,
                DueDateOffset = dueDateOffset,
                GracePeriod = gracePeriod,
                IsActive = true
            };
            context.BillingCycles.Add(billingCycle);
            await context.SaveChangesAsync();
            return billingCycle;
        }

    }
}

