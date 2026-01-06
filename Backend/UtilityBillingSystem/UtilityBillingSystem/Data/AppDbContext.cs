using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Core.Notification;

namespace UtilityBillingSystem.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UtilityType> UtilityTypes { get; set; }
        public DbSet<BillingCycle> BillingCycles { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<UtilityRequest> UtilityRequests { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure User entity
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.Status).HasDefaultValue("Active");
            });

            // Configure UtilityType
            builder.Entity<UtilityType>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Status).HasDefaultValue("Enabled");

                entity.HasOne(u => u.BillingCycle)
                    .WithMany(bc => bc.UtilityTypes)
                    .HasForeignKey(u => u.BillingCycleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure BillingCycle
            builder.Entity<BillingCycle>(entity =>
            {
                entity.HasKey(bc => bc.Id);
                entity.HasCheckConstraint("CK_BillingCycle_GenerationDay", "[GenerationDay] BETWEEN 1 AND 28");
            });

            // Configure Tariff
            builder.Entity<Tariff>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.BaseRate).HasColumnType("decimal(18,2)");
                entity.Property(t => t.FixedCharge).HasColumnType("decimal(18,2)");
                entity.Property(t => t.TaxPercentage).HasColumnType("decimal(5,2)");

                entity.HasOne(t => t.UtilityType)
                    .WithMany(ut => ut.Tariffs)
                    .HasForeignKey(t => t.UtilityTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Connection
            builder.Entity<Connection>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Status).HasDefaultValue("Active");

                entity.HasIndex(c => c.MeterNumber).IsUnique();

                entity.HasOne(c => c.User)
                    .WithMany(u => u.Connections)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.UtilityType)
                    .WithMany(ut => ut.Connections)
                    .HasForeignKey(c => c.UtilityTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Tariff)
                    .WithMany(t => t.Connections)
                    .HasForeignKey(c => c.TariffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure UtilityRequest
            builder.Entity<UtilityRequest>(entity =>
            {
                entity.HasKey(ur => ur.Id);
                entity.Property(ur => ur.Status).HasDefaultValue("Pending");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UtilityRequests)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ur => ur.UtilityType)
                    .WithMany(ut => ut.UtilityRequests)
                    .HasForeignKey(ur => ur.UtilityTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Bill
            builder.Entity<Bill>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.PreviousReading).HasColumnType("decimal(18,2)");
                entity.Property(b => b.CurrentReading).HasColumnType("decimal(18,2)");
                entity.Property(b => b.Consumption).HasColumnType("decimal(18,2)");
                entity.Property(b => b.BaseAmount).HasColumnType("decimal(18,2)");
                entity.Property(b => b.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(b => b.PenaltyAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(b => b.Status).HasDefaultValue("Generated");

                entity.HasOne(b => b.Connection)
                    .WithMany(c => c.Bills)
                    .HasForeignKey(b => b.ConnectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure MeterReading
            builder.Entity<MeterReading>(entity =>
            {
                entity.HasKey(mr => mr.Id);
                entity.Property(mr => mr.PreviousReading).HasColumnType("decimal(18,2)");
                entity.Property(mr => mr.CurrentReading).HasColumnType("decimal(18,2)");
                entity.Property(mr => mr.Consumption).HasColumnType("decimal(18,2)");
                entity.Property(mr => mr.Status).HasDefaultValue("ReadyForBilling");

                // Unique constraint: one reading per connection per billing cycle
                entity.HasIndex(mr => new { mr.ConnectionId, mr.BillingCycleId })
                    .IsUnique()
                    .HasFilter("[Status] = 'ReadyForBilling'");

                entity.HasOne(mr => mr.Connection)
                    .WithMany()
                    .HasForeignKey(mr => mr.ConnectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(mr => mr.BillingCycle)
                    .WithMany()
                    .HasForeignKey(mr => mr.BillingCycleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasOne(mr => mr.Tariff)
                    .WithMany()
                    .HasForeignKey(mr => mr.TariffId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(mr => mr.ReadingDate);
                entity.HasIndex(mr => mr.Status);
            });

            // Configure AuditLog
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(al => al.Id);
                entity.HasIndex(al => al.Timestamp);
            });

            // Configure Payment
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Status).HasDefaultValue("Completed");

                entity.HasOne(p => p.Bill)
                    .WithMany(b => b.Payments)
                    .HasForeignKey(p => p.BillId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.BillId);
                entity.HasIndex(p => p.PaymentDate);
            });

            // Configure Notification
            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.IsRead).HasDefaultValue(false);

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Bill)
                    .WithMany()
                    .HasForeignKey(n => n.BillId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.CreatedAt);
                entity.HasIndex(n => new { n.BillId, n.Type, n.CreatedAt });
            });
        }
    }
}
