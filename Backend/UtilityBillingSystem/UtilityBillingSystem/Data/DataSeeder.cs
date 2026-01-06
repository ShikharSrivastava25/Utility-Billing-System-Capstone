using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Data
{
    public class DataSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AppDbContext _context;

        public DataSeeder(IServiceProvider serviceProvider, AppDbContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            // Ensure database is created
            if (_context.Database.GetPendingMigrations().Any())
            {
                await _context.Database.MigrateAsync();
            }

            try
            {
                await SeedRolesAndUsersAsync();
                await SeedBillingCyclesAsync();
                await SeedUtilityTypesAsync();
                await SeedTariffsAsync();
                await SeedConnectionsAsync();
                await SeedMeterReadingsAndBillsAsync();
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error seeding data: {ex.Message}");
                throw;
            }
        }

        private async Task SeedRolesAndUsersAsync()
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "Admin", "Billing Officer", "Account Officer", "Consumer" };
            
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin user
            User? adminUser = await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    FullName = "Admin User",
                    Status = "Active"
                };
                var createResult = await userManager.CreateAsync(adminUser, "Test@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
                // Reload the user to ensure it's properly tracked
                adminUser = await userManager.FindByEmailAsync("admin@example.com");
            }
            if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            // Seed Billing Officer user
            User? billingOfficerUser = await userManager.FindByEmailAsync("billing@example.com");
            if (billingOfficerUser == null)
            {
                billingOfficerUser = new User
                {
                    UserName = "billing@example.com",
                    Email = "billing@example.com",
                    EmailConfirmed = true,
                    FullName = "Bill Officer",
                    Status = "Active"
                };
                var createResult = await userManager.CreateAsync(billingOfficerUser, "Test@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create billing officer user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
                // Reload the user to ensure it's properly tracked
                billingOfficerUser = await userManager.FindByEmailAsync("billing@example.com");
            }
            if (billingOfficerUser != null && !await userManager.IsInRoleAsync(billingOfficerUser, "Billing Officer"))
            {
                var roleResult = await userManager.AddToRoleAsync(billingOfficerUser, "Billing Officer");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add Billing Officer role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            // Seed Account Officer user
            User? accountOfficerUser = await userManager.FindByEmailAsync("account@example.com");
            if (accountOfficerUser == null)
            {
                accountOfficerUser = new User
                {
                    UserName = "account@example.com",
                    Email = "account@example.com",
                    EmailConfirmed = true,
                    FullName = "Account Officer",
                    Status = "Active"
                };
                var createResult = await userManager.CreateAsync(accountOfficerUser, "Test@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create account officer user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
                // Reload the user to ensure it's properly tracked
                accountOfficerUser = await userManager.FindByEmailAsync("account@example.com");
            }
            if (accountOfficerUser != null && !await userManager.IsInRoleAsync(accountOfficerUser, "Account Officer"))
            {
                var roleResult = await userManager.AddToRoleAsync(accountOfficerUser, "Account Officer");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add Account Officer role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            // Seed sample Consumer
            User? consumerUser = await userManager.FindByEmailAsync("consumer@example.com");
            if (consumerUser == null)
            {
                consumerUser = new User
                {
                    UserName = "consumer@example.com",
                    Email = "consumer@example.com",
                    EmailConfirmed = true,
                    FullName = "Connie Sumer",
                    Status = "Active"
                };
                var createResult = await userManager.CreateAsync(consumerUser, "Test@123");
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create consumer user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
                // Reload the user to ensure it's properly tracked
                consumerUser = await userManager.FindByEmailAsync("consumer@example.com");
            }
            if (consumerUser != null && !await userManager.IsInRoleAsync(consumerUser, "Consumer"))
            {
                var roleResult = await userManager.AddToRoleAsync(consumerUser, "Consumer");
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to add Consumer role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task SeedUtilityTypesAsync()
        {
            if (!await _context.UtilityTypes.AnyAsync())
            {
                // Get the active billing cycle
                var billingCycle = await _context.BillingCycles.FirstOrDefaultAsync(bc => bc.IsActive);
                var billingCycleId = billingCycle?.Id;

                var utilities = new[]
                {
                    new UtilityType { Name = "Electricity", Description = "Power supply services for residential and commercial use.", Status = "Enabled", BillingCycleId = billingCycleId },
                    new UtilityType { Name = "Water", Description = "Municipal water supply and sanitation services.", Status = "Enabled", BillingCycleId = billingCycleId },
                    new UtilityType { Name = "Gas", Description = "Natural gas for heating, cooking, and industrial purposes.", Status = "Disabled", BillingCycleId = billingCycleId },
                    new UtilityType { Name = "Internet", Description = "High-speed fiber optic internet connectivity.", Status = "Enabled", BillingCycleId = billingCycleId }
                };

                _context.UtilityTypes.AddRange(utilities);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedBillingCyclesAsync()
        {
            if (!await _context.BillingCycles.AnyAsync())
            {
                var cycles = new[]
                {
                    new BillingCycle { Name = "Standard Monthly Cycle", GenerationDay = 1, DueDateOffset = 15, GracePeriod = 5, IsActive = true },
                    new BillingCycle { Name = "Mid-Month Cycle", GenerationDay = 15, DueDateOffset = 10, GracePeriod = 3, IsActive = false }
                };

                _context.BillingCycles.AddRange(cycles);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedTariffsAsync()
        {
            if (!await _context.Tariffs.AnyAsync())
            {
                var electricity = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Electricity");
                var water = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Water");
                var internet = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Internet");

                if (electricity != null && water != null && internet != null)
                {
                    var tariffs = new[]
                    {
                        new Tariff { Name = "Residential Standard", UtilityTypeId = electricity.Id, BaseRate = 6, FixedCharge = 50, TaxPercentage = 5, CreatedAt = new DateTime(2023, 1, 1), IsActive = true },
                        new Tariff { Name = "Commercial Standard", UtilityTypeId = electricity.Id, BaseRate = 8.5m, FixedCharge = 150, TaxPercentage = 7.5m, CreatedAt = new DateTime(2023, 1, 1), IsActive = false },
                        new Tariff { Name = "Residential Water Plan", UtilityTypeId = water.Id, BaseRate = 25, FixedCharge = 100, TaxPercentage = 2, CreatedAt = new DateTime(2023, 6, 1), IsActive = true },
                        new Tariff { Name = "FiberNet 100Mbps", UtilityTypeId = internet.Id, BaseRate = 0, FixedCharge = 799, TaxPercentage = 18, CreatedAt = new DateTime(2023, 4, 1), IsActive = true }
                    };

                    _context.Tariffs.AddRange(tariffs);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedConnectionsAsync()
        {
            if (!await _context.Connections.AnyAsync())
            {
                var consumerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "consumer@example.com");
                var electricity = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Electricity");
                var water = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Water");
                var internet = await _context.UtilityTypes.FirstOrDefaultAsync(u => u.Name == "Internet");

                var residentialElectricityTariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.Name == "Residential Standard");
                var waterTariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.Name == "Residential Water Plan");
                var internetTariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.Name == "FiberNet 100Mbps");

                if (consumerUser != null && electricity != null && water != null && internet != null &&
                    residentialElectricityTariff != null && waterTariff != null && internetTariff != null)
                {
                    var connections = new[]
                    {
                        new Connection
                        {
                            UserId = consumerUser.Id,
                            UtilityTypeId = electricity.Id,
                            TariffId = residentialElectricityTariff.Id,
                            MeterNumber = "MTR-ELE-001",
                            Status = "Active"
                        },
                        new Connection
                        {
                            UserId = consumerUser.Id,
                            UtilityTypeId = water.Id,
                            TariffId = waterTariff.Id,
                            MeterNumber = "MTR-WTR-001",
                            Status = "Active"
                        },
                        new Connection
                        {
                            UserId = consumerUser.Id,
                            UtilityTypeId = internet.Id,
                            TariffId = internetTariff.Id,
                            MeterNumber = "MTR-INT-001",
                            Status = "Active"
                        }
                    };

                    _context.Connections.AddRange(connections);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedMeterReadingsAndBillsAsync()
        {
            // Only seed if there are no existing meter readings
            if (await _context.MeterReadings.AnyAsync())
            {
                return; // Skip if data already exists
            }

            var consumerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "consumer@example.com");
            if (consumerUser == null)
                return;

            var connections = await _context.Connections
                .Include(c => c.UtilityType)
                    .ThenInclude(ut => ut!.BillingCycle)
                .Include(c => c.Tariff)
                .Where(c => c.UserId == consumerUser.Id && c.Status == "Active")
                .ToListAsync();

            if (!connections.Any())
                return;

            var billingOfficerEmail = "billing@example.com";
            var random = new Random();
            var now = DateTime.UtcNow;
            
            // Generate data for the last 5 months (excluding current month so billing officer can enter readings)
            for (int monthOffset = 5; monthOffset >= 1; monthOffset--)
            {
                var targetDate = now.AddMonths(-monthOffset);
                var targetMonth = targetDate.Month;
                var targetYear = targetDate.Year;

                foreach (var connection in connections)
                {
                    if (connection.UtilityType?.BillingCycle == null || connection.Tariff == null)
                        continue;

                    var billingCycle = connection.UtilityType.BillingCycle;
                    
                    // Calculate reading date within the billing period
                    var generationDay = Math.Min(billingCycle.GenerationDay, DateTime.DaysInMonth(targetYear, targetMonth));
                    var readingDate = new DateTime(targetYear, targetMonth, generationDay, 0, 0, 0, DateTimeKind.Utc);

                    // Check if reading already exists for this period
                    var existingReading = await _context.MeterReadings
                        .Where(mr => mr.ConnectionId == connection.Id &&
                                     mr.BillingCycleId == billingCycle.Id &&
                                     mr.ReadingDate.Year == targetYear &&
                                     mr.ReadingDate.Month == targetMonth)
                        .FirstOrDefaultAsync();

                    if (existingReading != null)
                        continue;

                    // Get previous reading (last billed reading's current reading)
                    var previousReading = await _context.MeterReadings
                        .Where(mr => mr.ConnectionId == connection.Id && mr.Status == "Billed")
                        .OrderByDescending(mr => mr.ReadingDate)
                        .Select(mr => mr.CurrentReading)
                        .FirstOrDefaultAsync();

                    decimal consumption;
                    decimal currentReading;
                    
                    if (connection.UtilityType.Name == "Electricity")
                    {
                        // Electricity: 200-500 units per month
                        consumption = random.Next(200, 501);
                        currentReading = previousReading + consumption;
                    }
                    else if (connection.UtilityType.Name == "Water")
                    {
                        // Water: 10-30 units per month
                        consumption = random.Next(10, 31);
                        currentReading = previousReading + consumption;
                    }
                    else if (connection.UtilityType.Name == "Internet")
                    {
                        // Internet: fixed charge only, no consumption units
                        consumption = 0;
                        currentReading = previousReading;
                    }
                    else
                    {
                        consumption = random.Next(50, 201);
                        currentReading = previousReading + consumption;
                    }

                    // Create meter reading
                    var meterReading = new MeterReading
                    {
                        ConnectionId = connection.Id,
                        PreviousReading = previousReading,
                        CurrentReading = currentReading,
                        Consumption = consumption,
                        ReadingDate = readingDate,
                        Status = "Billed",
                        RecordedBy = billingOfficerEmail,
                        BillingCycleId = billingCycle.Id,
                        TariffId = connection.TariffId, // Store tariff at time of reading
                        CreatedAt = readingDate
                    };

                    _context.MeterReadings.Add(meterReading);
                    await _context.SaveChangesAsync();

                    // Generate bill for this reading
                    var tariff = connection.Tariff;
                    var baseAmount = (consumption * tariff.BaseRate) + tariff.FixedCharge;
                    var taxAmount = baseAmount * (tariff.TaxPercentage / 100);
                    var totalAmount = baseAmount + taxAmount;
                    var dueDate = readingDate.AddDays(billingCycle.DueDateOffset);
                    var billingPeriod = BillingCycleHelper.GetBillingPeriodLabelForDate(readingDate, billingCycle);

                    // Check if bill already exists
                    var existingBill = await _context.Bills
                        .Where(b => b.ConnectionId == connection.Id && b.BillingPeriod == billingPeriod)
                        .FirstOrDefaultAsync();

                    if (existingBill == null)
                    {
                        // Most recent month (offset 1) will be "Overdue", older months will be "Paid"
                        var billStatus = monthOffset == 1 ? "Overdue" : "Paid";
                        
                        var bill = new Bill
                        {
                            ConnectionId = connection.Id,
                            BillingPeriod = billingPeriod,
                            GenerationDate = readingDate,
                            DueDate = dueDate,
                            PreviousReading = meterReading.PreviousReading,
                            CurrentReading = meterReading.CurrentReading,
                            Consumption = consumption,
                            BaseAmount = baseAmount,
                            TaxAmount = taxAmount,
                            PenaltyAmount = 0,
                            TotalAmount = totalAmount,
                            Status = billStatus
                        };

                        _context.Bills.Add(bill);
                        await _context.SaveChangesAsync();

                        // Add payment for bills older than the most recent month (offset > 1) to show payment history
                        if (monthOffset > 1)
                        {
                            var paymentMethod = random.Next(0, 2) == 0 ? "Cash" : "Online";
                            var payment = new Payment
                            {
                                BillId = bill.Id,
                                PaymentDate = dueDate.AddDays(random.Next(-5, 1)), // Payment made around due date
                                Amount = totalAmount,
                                PaymentMethod = paymentMethod,
                                ReceiptNumber = paymentMethod == "Cash" ? $"RCP-{random.Next(10000, 99999)}" : null,
                                UpiId = paymentMethod == "Online" ? $"user{random.Next(100, 999)}@upi" : null,
                                Status = "Completed"
                            };

                            _context.Payments.Add(payment);
                            bill.Status = "Paid"; // Update status after payment is added
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
