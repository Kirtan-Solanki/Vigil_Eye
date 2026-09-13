using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VE.Shared.Data.Migrations;

/// <summary>
/// VE-008: Initial migration for Vigil Eye data model.
/// Creates tables for pairing, events, alerts, and audit logging.
/// </summary>
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                MfaEnabled = table.Column<bool>(type: "boolean", nullable: false),
                MfaSecret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                CompletedAttestationId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "GuardianAttestations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                AttestationText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AttestationTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GuardianAttestations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ChildProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                BirthYear = table.Column<int>(type: "integer", nullable: false),
                Tier = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChildProfiles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RetentionPolicies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RetentionPolicies", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PairingSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                QrToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LockedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PairingSessions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Devices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                OsVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                DeviceModel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PublicKeyPem = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                PairedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Devices", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RotatedFromId = table.Column<Guid>(type: "uuid", nullable: true),
                LastIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                LastDeviceFingerprint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                EncryptedPayload = table.Column<byte[]>(type: "bytea", nullable: false),
                EncryptionIv = table.Column<byte[]>(type: "bytea", nullable: false),
                EncryptionAuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AlertRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                ConfigJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlertRules", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Geofences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                CenterLat = table.Column<double>(type: "double precision", nullable: false),
                CenterLng = table.Column<double>(type: "double precision", nullable: false),
                RadiusMeters = table.Column<int>(type: "integer", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Geofences", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                GuardianAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                InternalUserId = table.Column<string>(type: "text", nullable: true),
                ChildDataAccessedId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(name: "IX_Accounts_Email", table: "Accounts", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Accounts_CreatedAt", table: "Accounts", column: "CreatedAt");

        migrationBuilder.CreateIndex(name: "IX_GuardianAttestations_AccountId", table: "GuardianAttestations", column: "AccountId");
        migrationBuilder.CreateIndex(name: "IX_GuardianAttestations_AttestationTimestamp", table: "GuardianAttestations", column: "AttestationTimestamp");

        migrationBuilder.CreateIndex(name: "IX_ChildProfiles_AccountId", table: "ChildProfiles", column: "AccountId");
        migrationBuilder.CreateIndex(name: "IX_ChildProfiles_CreatedAt", table: "ChildProfiles", column: "CreatedAt");

        migrationBuilder.CreateIndex(name: "IX_PairingSessions_ExpiresAt", table: "PairingSessions", column: "ExpiresAt");
        migrationBuilder.CreateIndex(name: "IX_PairingSessions_AccountId", table: "PairingSessions", column: "AccountId");
        migrationBuilder.CreateIndex(name: "IX_PairingSessions_Code", table: "PairingSessions", column: "Code");

        migrationBuilder.CreateIndex(name: "IX_Devices_ChildId", table: "Devices", column: "ChildId");
        migrationBuilder.CreateIndex(name: "IX_Devices_Status", table: "Devices", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Devices_LastSeenAt", table: "Devices", column: "LastSeenAt");

        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_DeviceId", table: "RefreshTokens", column: "DeviceId");
        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_ExpiresAt", table: "RefreshTokens", column: "ExpiresAt");
        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_RevokedAt", table: "RefreshTokens", column: "RevokedAt");
        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_LastUsedAt", table: "RefreshTokens", column: "LastUsedAt");

        migrationBuilder.CreateIndex(name: "IX_Events_DeviceId", table: "Events", column: "DeviceId");
        migrationBuilder.CreateIndex(name: "IX_Events_Type", table: "Events", column: "Type");
        migrationBuilder.CreateIndex(name: "IX_Events_OccurredAt", table: "Events", column: "OccurredAt");
        migrationBuilder.CreateIndex(name: "IX_Events_SyncedAt", table: "Events", column: "SyncedAt");

        migrationBuilder.CreateIndex(name: "IX_AlertRules_ChildId", table: "AlertRules", column: "ChildId");
        migrationBuilder.CreateIndex(name: "IX_AlertRules_Enabled", table: "AlertRules", column: "Enabled");

        migrationBuilder.CreateIndex(name: "IX_Geofences_ChildId", table: "Geofences", column: "ChildId");
        migrationBuilder.CreateIndex(name: "IX_Geofences_Active", table: "Geofences", column: "Active");

        migrationBuilder.CreateIndex(name: "IX_AuditLogs_GuardianAccountId", table: "AuditLogs", column: "GuardianAccountId");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_InternalUserId", table: "AuditLogs", column: "InternalUserId");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_ChildDataAccessedId", table: "AuditLogs", column: "ChildDataAccessedId");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_Type", table: "AuditLogs", column: "Type");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_AccessedAt", table: "AuditLogs", column: "AccessedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GuardianAttestations");
        migrationBuilder.DropTable(name: "Accounts");
        migrationBuilder.DropTable(name: "ChildProfiles");
        migrationBuilder.DropTable(name: "PairingSessions");
        migrationBuilder.DropTable(name: "Devices");
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "Events");
        migrationBuilder.DropTable(name: "AlertRules");
        migrationBuilder.DropTable(name: "Geofences");
        migrationBuilder.DropTable(name: "RetentionPolicies");
        migrationBuilder.DropTable(name: "AuditLogs");
    }
}
