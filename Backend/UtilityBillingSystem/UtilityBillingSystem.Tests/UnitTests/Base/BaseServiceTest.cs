using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Moq;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Mappings;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Tests.UnitTests.Base
{
    public abstract class BaseServiceTest : IDisposable
    {
        protected readonly AppDbContext Context;
        protected readonly Mock<IAuditLogService> MockAuditLogService;
        protected readonly IMapper Mapper;

        protected BaseServiceTest()
        {
            // Create InMemory database with unique name for each test
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new AppDbContext(options);
            MockAuditLogService = new Mock<IAuditLogService>();

            MockAuditLogService
                .Setup(x => x.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Configure AutoMapper for tests
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            Mapper = mapperConfig.CreateMapper();
        }

        protected UserManager<User> CreateUserManager()
        {
            var userStore = new UserStore<User, IdentityRole, AppDbContext>(
                Context,
                new IdentityErrorDescriber());

            return new UserManager<User>(
                userStore,
                null,
                new PasswordHasher<User>(),
                new List<IUserValidator<User>> { new UserValidator<User>() },
                new List<IPasswordValidator<User>> { new PasswordValidator<User>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                null);
        }

        protected RoleManager<IdentityRole> CreateRoleManager()
        {
            var roleStore = new RoleStore<IdentityRole, AppDbContext>(
                Context,
                new IdentityErrorDescriber());

            return new RoleManager<IdentityRole>(
                roleStore,
                new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null
                );
        }

        protected async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "Billing Officer", "Account Officer", "Consumer" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        protected async Task<User> CreateUserAsync(
            string? email = null,
            string? fullName = null,
            string status = "Active",
            string? userId = null)
        {
            var user = new User
            {
                Id = userId ?? Guid.NewGuid().ToString(),
                UserName = email ?? "test@example.com",
                Email = email ?? "test@example.com",
                FullName = fullName ?? "Test User",
                Status = status,
                EmailConfirmed = true
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        protected async Task<BillingCycle> CreateBillingCycleAsync(
            string? name = null,
            int generationDay = 1,
            int dueDateOffset = 30,
            int gracePeriod = 7,
            bool isActive = true,
            string? billingCycleId = null)
        {
            var billingCycle = new BillingCycle
            {
                Id = billingCycleId ?? Guid.NewGuid().ToString(),
                Name = name ?? "Monthly",
                GenerationDay = generationDay,
                DueDateOffset = dueDateOffset,
                GracePeriod = gracePeriod,
                IsActive = isActive
            };

            Context.BillingCycles.Add(billingCycle);
            await Context.SaveChangesAsync();
            return billingCycle;
        }

        protected async Task<UtilityType> CreateUtilityTypeAsync(
            string? name = null,
            string? billingCycleId = null,
            string status = "Enabled",
            string? utilityTypeId = null)
        {
            var utilityType = new UtilityType
            {
                Id = utilityTypeId ?? Guid.NewGuid().ToString(),
                Name = name ?? "Electricity",
                Description = $"Test {name ?? "Electricity"}",
                Status = status,
                BillingCycleId = billingCycleId
            };

            Context.UtilityTypes.Add(utilityType);
            await Context.SaveChangesAsync();

            // Load billing cycle if provided
            if (billingCycleId != null)
            {
                await Context.Entry(utilityType)
                    .Reference(u => u.BillingCycle)
                    .LoadAsync();
            }

            return utilityType;
        }

        protected async Task<UtilityType> CreateUtilityTypeWithBillingCycleAsync(
            string? utilityName = null,
            string? billingCycleName = null,
            int generationDay = 1,
            int dueDateOffset = 30,
            int gracePeriod = 7)
        {
            var billingCycle = await CreateBillingCycleAsync(
                name: billingCycleName,
                generationDay: generationDay,
                dueDateOffset: dueDateOffset,
                gracePeriod: gracePeriod);

            var utilityType = await CreateUtilityTypeAsync(
                name: utilityName,
                billingCycleId: billingCycle.Id);

            return utilityType;
        }

        protected async Task<Tariff> CreateTariffAsync(
            string utilityTypeId,
            string? name = null,
            decimal baseRate = 5.0m,
            decimal fixedCharge = 100.0m,
            decimal taxPercentage = 18.0m,
            bool isActive = true,
            string? tariffId = null)
        {
            var tariff = new Tariff
            {
                Id = tariffId ?? Guid.NewGuid().ToString(),
                Name = name ?? "Standard",
                UtilityTypeId = utilityTypeId,
                BaseRate = baseRate,
                FixedCharge = fixedCharge,
                TaxPercentage = taxPercentage,
                CreatedAt = DateTime.UtcNow,
                IsActive = isActive
            };

            Context.Tariffs.Add(tariff);
            await Context.SaveChangesAsync();
            return tariff;
        }

        protected async Task<Connection> CreateConnectionAsync(
            string userId,
            string utilityTypeId,
            string tariffId,
            string? meterNumber = null,
            string status = "Active",
            string? connectionId = null)
        {
            var connection = new Connection
            {
                Id = connectionId ?? Guid.NewGuid().ToString(),
                UserId = userId,
                UtilityTypeId = utilityTypeId,
                TariffId = tariffId,
                MeterNumber = meterNumber ?? $"M{Guid.NewGuid().ToString().Substring(0, 8)}",
                Status = status
            };

            Context.Connections.Add(connection);
            await Context.SaveChangesAsync();

            // Load related entities
            await Context.Entry(connection).Reference(c => c.User).LoadAsync();
            await Context.Entry(connection).Reference(c => c.UtilityType).LoadAsync();
            await Context.Entry(connection).Reference(c => c.Tariff).LoadAsync();

            return connection;
        }

        protected async Task<Connection> CreateFullConnectionAsync(
            string? userId = null,
            string? utilityTypeId = null,
            string? tariffId = null,
            string? meterNumber = null,
            string status = "Active")
        {
            // Create user if not provided
            var user = userId != null 
                ? await Context.Users.FindAsync(userId) 
                : await CreateUserAsync();

            if (user == null)
            {
                user = await CreateUserAsync();
            }

            // Create utility type with billing cycle if not provided
            UtilityType utilityType;
            if (utilityTypeId != null)
            {
                utilityType = await Context.UtilityTypes.FindAsync(utilityTypeId);
                if (utilityType == null)
                {
                    utilityType = await CreateUtilityTypeWithBillingCycleAsync();
                }
            }
            else
            {
                utilityType = await CreateUtilityTypeWithBillingCycleAsync();
            }

            // Create tariff if not provided
            Tariff tariff;
            if (tariffId != null)
            {
                tariff = await Context.Tariffs.FindAsync(tariffId);
                if (tariff == null || tariff.UtilityTypeId != utilityType.Id)
                {
                    tariff = await CreateTariffAsync(utilityType.Id);
                }
            }
            else
            {
                tariff = await CreateTariffAsync(utilityType.Id);
            }

            return await CreateConnectionAsync(
                user.Id,
                utilityType.Id,
                tariff.Id,
                meterNumber,
                status);
        }

        protected async Task<MeterReading> CreateMeterReadingAsync(
            string connectionId,
            decimal previousReading,
            decimal currentReading,
            string? recordedBy = null,
            string? billingCycleId = null,
            string status = "ReadyForBilling",
            DateTime? readingDate = null,
            string? tariffId = null,
            string? readingId = null)
        {
            // Get TariffId from connection if not provided
            if (string.IsNullOrEmpty(tariffId))
            {
                var connection = await Context.Connections.FindAsync(connectionId);
                if (connection != null)
                {
                    tariffId = connection.TariffId;
                }
                else
                {
                    throw new InvalidOperationException($"Connection {connectionId} not found. Cannot create meter reading without TariffId.");
                }
            }

            var reading = new MeterReading
            {
                Id = readingId ?? Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                PreviousReading = previousReading,
                CurrentReading = currentReading,
                Consumption = currentReading - previousReading,
                ReadingDate = readingDate ?? DateTime.UtcNow,
                Status = status,
                RecordedBy = recordedBy ?? "test@example.com",
                BillingCycleId = billingCycleId ?? Guid.NewGuid().ToString(),
                TariffId = tariffId,
                CreatedAt = DateTime.UtcNow
            };

            Context.MeterReadings.Add(reading);
            await Context.SaveChangesAsync();

            await Context.Entry(reading).Reference(mr => mr.Connection).LoadAsync();

            return reading;
        }

        protected async Task<Bill> CreateBillAsync(
            string connectionId,
            decimal consumption,
            decimal baseAmount,
            decimal taxAmount,
            decimal totalAmount,
            string status = "Generated",
            DateTime? generationDate = null,
            DateTime? dueDate = null,
            string? billingPeriod = null,
            string? billId = null)
        {
            var bill = new Bill
            {
                Id = billId ?? Guid.NewGuid().ToString(),
                ConnectionId = connectionId,
                BillingPeriod = billingPeriod ?? DateTime.UtcNow.ToString("MMMM yyyy"),
                GenerationDate = generationDate ?? DateTime.UtcNow,
                DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
                PreviousReading = 0,
                CurrentReading = 0,
                Consumption = consumption,
                BaseAmount = baseAmount,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = status,
                PenaltyAmount = 0
            };

            Context.Bills.Add(bill);
            await Context.SaveChangesAsync();

            await Context.Entry(bill).Reference(b => b.Connection).LoadAsync();

            return bill;
        }

        protected async Task<Payment> CreatePaymentAsync(
            string billId,
            decimal amount,
            string paymentMethod = "Cash",
            DateTime? paymentDate = null,
            string? receiptNumber = null,
            string? upiId = null,
            string status = "Completed",
            string? paymentId = null)
        {
            var payment = new Payment
            {
                Id = paymentId ?? Guid.NewGuid().ToString(),
                BillId = billId,
                PaymentDate = paymentDate ?? DateTime.UtcNow,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ReceiptNumber = receiptNumber,
                UpiId = upiId,
                Status = status
            };

            Context.Payments.Add(payment);
            await Context.SaveChangesAsync();

            return payment;
        }

        protected async Task<UtilityRequest> CreateUtilityRequestAsync(
            string userId,
            string utilityTypeId,
            string status = "Pending",
            DateTime? requestDate = null,
            DateTime? decisionDate = null,
            string? requestId = null)
        {
            var request = new UtilityRequest
            {
                Id = requestId ?? Guid.NewGuid().ToString(),
                UserId = userId,
                UtilityTypeId = utilityTypeId,
                Status = status,
                RequestDate = requestDate ?? DateTime.UtcNow,
                DecisionDate = decisionDate
            };

            Context.UtilityRequests.Add(request);
            await Context.SaveChangesAsync();

            await Context.Entry(request).Reference(ur => ur.User).LoadAsync();
            await Context.Entry(request).Reference(ur => ur.UtilityType).LoadAsync();

            return request;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}

