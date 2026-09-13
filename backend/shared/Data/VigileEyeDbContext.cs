using Microsoft.EntityFrameworkCore;
using VE.Shared.Models;

namespace VE.Shared.Data;

/// <summary>
/// VE-008: PostgreSQL data model for Vigil Eye.
/// Includes schema for pairing, events, alerts, and audit logging.
/// </summary>
public class VigileEyeDbContext : DbContext
{
    public VigileEyeDbContext(DbContextOptions<VigileEyeDbContext> options) : base(options)
    {
    }

    public DbSet<GuardianAttestation> GuardianAttestations { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<ChildProfile> ChildProfiles { get; set; }
    public DbSet<PairingSession> PairingSessions { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<Geofence> Geofences { get; set; }
    public DbSet<RetentionPolicy> RetentionPolicies { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // VE-002: Guardian attestation is immutable and timestamped
        modelBuilder.Entity<GuardianAttestation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AttestationText).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.AttestationTimestamp);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.MfaSecret).HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<ChildProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // VE-004: Pairing session with rate-limit and lockout tracking
        modelBuilder.Entity<PairingSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(8);
            entity.Property(e => e.QrToken).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.LockedByIp).HasMaxLength(45);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.Code);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(20);
            entity.Property(e => e.OsVersion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AppVersion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceModel).HasMaxLength(255);
            entity.Property(e => e.PublicKeyPem).IsRequired();
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastSeenAt);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LastIpAddress).HasMaxLength(45);
            entity.Property(e => e.LastDeviceFingerprint).HasMaxLength(255);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.RevokedAt);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // VE-003: Events stored encrypted; no message content field in v1
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.EncryptedPayload).IsRequired();
            entity.Property(e => e.EncryptionIv).IsRequired();
            entity.Property(e => e.EncryptionAuthTag).IsRequired();
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.SyncedAt);
            // Time-series partition by device and date (in TimescaleDB via hypertable)
        });

        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.ConfigJson).IsRequired();
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Enabled);
        });

        modelBuilder.Entity<Geofence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.Active);
        });

        modelBuilder.Entity<RetentionPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AccountId).IsUnique();
            entity.Property(e => e.RetentionDays).HasDefaultValue(90);
        });

        // VE-006: Audit logging with separate tables for guardian vs. internal access
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => e.GuardianAccountId);
            entity.HasIndex(e => e.InternalUserId);
            entity.HasIndex(e => e.ChildDataAccessedId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.AccessedAt);
            // Retained separately from activity data per Security Assessment §5
        });
    }
}
