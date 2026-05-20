using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Country>                  Countries                  => Set<Country>();
    public DbSet<Branch>                   Branches                   => Set<Branch>();
    public DbSet<User>                     Users                      => Set<User>();
    public DbSet<Customer>                 Customers                  => Set<Customer>();
    public DbSet<CustomerProfile>          CustomerProfiles           => Set<CustomerProfile>();
    public DbSet<PointsAccount>            PointsAccounts             => Set<PointsAccount>();
    public DbSet<Shipment>                 Shipments                  => Set<Shipment>();
    public DbSet<PointsTransaction>        PointsTransactions         => Set<PointsTransaction>();
    public DbSet<RedemptionRequest>        RedemptionRequests         => Set<RedemptionRequest>();
    public DbSet<RedemptionQrCode>         RedemptionQrCodes          => Set<RedemptionQrCode>();
    public DbSet<RefreshToken>             RefreshTokens              => Set<RefreshToken>();
    public DbSet<AuditLog>                 AuditLogs                  => Set<AuditLog>();
    public DbSet<LoginAttempt>             LoginAttempts              => Set<LoginAttempt>();
    public DbSet<FraudAlert>               FraudAlerts                => Set<FraudAlert>();
    public DbSet<SystemConfiguration>      SystemConfigurations       => Set<SystemConfiguration>();
    public DbSet<SystemConfigurationAudit> SystemConfigurationAudits  => Set<SystemConfigurationAudit>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Countries ──────────────────────────────────────────────────
        mb.Entity<Country>(e =>
        {
            e.ToTable("Countries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(2).IsFixedLength();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Currency).HasMaxLength(3).IsFixedLength();
            e.Property(x => x.TimeZone).HasMaxLength(50);
        });

        // ── Branches ───────────────────────────────────────────────────
        mb.Entity<Branch>(e =>
        {
            e.ToTable("Branches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Code).HasMaxLength(10);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId);
        });

        // ── Users ──────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Role).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.CreatedBy).HasMaxLength(100).HasDefaultValue("SYSTEM");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Customers ──────────────────────────────────────────────────
        mb.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.IdentityNumber).HasMaxLength(20);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.DeviceToken).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.IdentityNumber).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique();
        });

        // ── CustomerProfiles ──────────────────────────────────────────
        mb.Entity<CustomerProfile>(e =>
        {
            e.ToTable("CustomerProfiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.ProfileCompletionLevel).HasConversion<byte>();
            e.Property(x => x.BonusApplied).HasPrecision(18, 2);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.CustomerId).IsUnique();
            e.HasOne(x => x.Customer).WithOne(c => c.Profile)
             .HasForeignKey<CustomerProfile>(x => x.CustomerId);
            e.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PointsAccounts ────────────────────────────────────────────
        mb.Entity<PointsAccount>(e =>
        {
            e.ToTable("PointsAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.TotalAccumulated).HasPrecision(18, 2);
            e.Property(x => x.TotalRedeemed).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();      // CRITICAL: concurrency lock
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.CustomerId).IsUnique();
            e.HasOne(x => x.Customer).WithOne(c => c.PointsAccount)
             .HasForeignKey<PointsAccount>(x => x.CustomerId);
        });

        // ── Shipments ─────────────────────────────────────────────────
        mb.Entity<Shipment>(e =>
        {
            e.ToTable("Shipments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.ShipmentNumber).HasMaxLength(50);
            e.Property(x => x.OwnerIdentityNumber).HasMaxLength(20);
            e.Property(x => x.ShipmentAmount).HasPrecision(18, 2);
            e.Property(x => x.PointsAwarded).HasPrecision(18, 2);
            e.Property(x => x.SourceSystem).HasMaxLength(50).HasDefaultValue("API_CENTRAL_CE");
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.ShipmentNumber).IsUnique();
            e.HasIndex(x => x.OwnerIdentityNumber);
            e.HasOne(x => x.ScannedByCustomer).WithMany()
             .HasForeignKey(x => x.ScannedByCustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── PointsTransactions ────────────────────────────────────────
        mb.Entity<PointsTransaction>(e =>
        {
            e.ToTable("PointsTransactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.Property(x => x.TransactionType).HasConversion<byte>();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.PointsAccount).WithMany().HasForeignKey(x => x.PointsAccountId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Shipment).WithMany().HasForeignKey(x => x.ShipmentId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── RedemptionRequests ────────────────────────────────────────
        mb.Entity<RedemptionRequest>(e =>
        {
            e.ToTable("RedemptionRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.RequestedAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.CancellationReason).HasMaxLength(300);
            e.Property(x => x.RequestedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── RedemptionQrCodes ─────────────────────────────────────────
        mb.Entity<RedemptionQrCode>(e =>
        {
            e.ToTable("RedemptionQrCodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.QrCode).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();       // CRITICAL: double-use prevention
            e.Property(x => x.GeneratedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.QrCode).IsUnique();
            e.HasOne(x => x.RedemptionRequest).WithOne(r => r.QrCode)
             .HasForeignKey<RedemptionQrCode>(x => x.RedemptionRequestId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RefreshTokens ─────────────────────────────────────────────
        mb.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.TokenHash).HasMaxLength(256);
            e.Property(x => x.RevokedReason).HasMaxLength(20);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.DeviceInfo).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLogs ─────────────────────────────────────────────────
        mb.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.EntityId).HasMaxLength(50);
            e.Property(x => x.OperationType).HasMaxLength(50);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.Result).HasConversion<byte>();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ── LoginAttempts ─────────────────────────────────────────────
        mb.Entity<LoginAttempt>(e =>
        {
            e.ToTable("LoginAttempts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.IdentityNumber).HasMaxLength(20);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.DeviceInfo).HasMaxLength(200);
            e.Property(x => x.FailureReason).HasMaxLength(30);
            e.Property(x => x.UserType).HasConversion<byte>();
            e.Property(x => x.AttemptedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ── FraudAlerts ───────────────────────────────────────────────
        mb.Entity<FraudAlert>(e =>
        {
            e.ToTable("FraudAlerts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.AlertType).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.RelatedEntityType).HasMaxLength(50);
            e.Property(x => x.RelatedEntityId).HasMaxLength(50);
            e.Property(x => x.ResolutionNotes).HasMaxLength(500);
            e.Property(x => x.Severity).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.DetectedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── SystemConfigurations ──────────────────────────────────────
        mb.Entity<SystemConfiguration>(e =>
        {
            e.ToTable("SystemConfigurations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ConfigKey).HasMaxLength(100);
            e.Property(x => x.ConfigValue).HasMaxLength(500);
            e.Property(x => x.DataType).HasMaxLength(10);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Module).HasMaxLength(20);
            e.Property(x => x.MinValue).HasMaxLength(50);
            e.Property(x => x.MaxValue).HasMaxLength(50);
            e.Property(x => x.UpdatedBy).HasMaxLength(100).HasDefaultValue("SYSTEM");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.ConfigKey).IsUnique();
        });

        // ── SystemConfigurationAudit ──────────────────────────────────
        mb.Entity<SystemConfigurationAudit>(e =>
        {
            e.ToTable("SystemConfigurationAudit");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ConfigKey).HasMaxLength(100);
            e.Property(x => x.OldValue).HasMaxLength(500);
            e.Property(x => x.NewValue).HasMaxLength(500);
            e.Property(x => x.ChangedBy).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.ChangedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
