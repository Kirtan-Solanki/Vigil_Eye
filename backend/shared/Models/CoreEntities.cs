namespace VE.Shared.Models;

/// <summary>
/// Core data models for Vigil Eye backend.
/// TRD §6: Core entities for the pairing, event, and alert system.
/// </summary>

// VE-002: Guardian consent attestation — immutable, timestamped, server-enforced
public class GuardianAttestation
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AttestationText { get; set; } = string.Empty;
    public DateTime AttestationTimestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Account
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; } // TOTP secret, encrypted
    public Guid? CompletedAttestationId { get; set; } // Latest completed attestation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ChildProfile
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; } // Owner (parent)
    public string Name { get; set; } = string.Empty;
    public int BirthYear { get; set; }

    /// <summary>
    /// Age tier determines which features/alerts are enabled.
    /// Supports age-appropriate supervision (PRD §7).
    /// </summary>
    public enum AgeTier
    {
        Child = 1,      // 6-12: basic monitoring
        Preteen = 2,    // 13-14: standard monitoring
        Teen = 3,       // 15-18: reduced monitoring
    }

    public AgeTier Tier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// VE-004: Pairing session with rate-limit enforcement
public class PairingSession
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid ChildId { get; set; }

    /// <summary>
    /// Short-lived (5 min per VE-004 rate-limit), single-use token.
    /// </summary>
    public string Code { get; set; } = string.Empty; // 8-char alphanumeric fallback
    public string QrToken { get; set; } = string.Empty; // Base64 encoded

    public DateTime ExpiresAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public int FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; } // VE-004: lockout tracking
    public string? LockedByIp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Device
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Platform { get; set; } = string.Empty; // "android" | "ios"
    public string OsVersion { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;

    /// <summary>
    /// Ed25519 public key, generated in Android Keystore / iOS Secure Enclave.
    /// Used to sign refresh-token requests (TRD §5.1.1).
    /// </summary>
    public string PublicKeyPem { get; set; } = string.Empty;

    public enum DeviceStatus
    {
        Active = 1,
        TamperFlagged = 2,
        Revoked = 3,
        PendingPairing = 4
    }

    public DeviceStatus Status { get; set; } = DeviceStatus.PendingPairing;
    public DateTime PairedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// VE-005: Refresh token with rotation and revocation tracking
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Store only hash; never store plaintext.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Reference to previous token in rotation chain (for audit trail).
    /// </summary>
    public Guid? RotatedFromId { get; set; }

    /// <summary>
    /// VE-005: Risk-based re-attestation triggers.
    /// </summary>
    public string? LastIpAddress { get; set; }
    public string? LastDeviceFingerprint { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // 1-year sliding expiry per TRD

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// VE-003: Event model with metadata-only fields (no message content in v1)
public class Event
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    public enum EventType
    {
        Call = 1,
        SmsMeta = 2,           // Metadata only: sender, recipient, timestamp
        Location = 3,
        AppUsage = 4,
        WebHistory = 5,
        DeviceStatus = 6,
        TamperDetected = 7
    }

    public EventType Type { get; set; }

    /// <summary>
    /// Encrypted payload (JSON). Decryption happens at application layer.
    /// </summary>
    public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();
    public byte[] EncryptionIv { get; set; } = Array.Empty<byte>();
    public byte[] EncryptionAuthTag { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// When the event occurred on the device.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// When it was synced to backend.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AlertRule
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }

    public enum AlertType
    {
        GeofenceExit = 1,
        GeofenceEntry = 2,
        UnknownContact = 3,
        NewAppInstall = 4,
        LowBattery = 5,
        DeviceOffline = 6,
        TamperDetected = 7,
        KeywordMatch = 8  // Metadata-only in v1; content matching feature-gated
    }

    public AlertType Type { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Rule config (JSON): geofence radius, contact patterns, etc.
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Geofence
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public int RadiusMeters { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RetentionPolicy
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }

    /// <summary>
    /// Default 90 days per PRD §10. Configurable within policy limits.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// VE-006: Audit logging for internal and guardian access
public class AuditLog
{
    public Guid Id { get; set; }

    public enum AccessType
    {
        GuardianAccess = 1,
        InternalAccess = 2
    }

    public AccessType Type { get; set; }
    public Guid? GuardianAccountId { get; set; } // Null if internal access
    public string? InternalUserId { get; set; } // Employee ID if internal access
    public Guid? ChildDataAccessedId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
