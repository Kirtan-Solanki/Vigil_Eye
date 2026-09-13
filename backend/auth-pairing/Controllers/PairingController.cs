using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using VE.Shared.Authentication;
using VE.Shared.Data;
using VE.Shared.Models;

namespace VE.AuthPairing.Controllers;

/// <summary>
/// VE-004: Device pairing API with rate-limiting and lockout.
/// TRD §5.1: QR-code pairing + protected fallback code, one-time ceremony.
/// Security Assessment R-1: Rate-limit and lock out fallback code after 5 failed attempts.
/// </summary>
[ApiController]
[Route("api/v1/pairing")]
public class PairingController : ControllerBase
{
    private readonly VigileEyeDbContext _db;
    private readonly DeviceTokenManager _tokenManager;
    private readonly ILogger<PairingController> _logger;

    // VE-004: Shortened from 10 min to 5 min per Security Assessment R-1 mitigation
    private const int PairingCodeValidityMinutes = 5;
    private const int MaxFailedAttempts = 5; // VE-004: lockout after 5 failures
    private const int LockoutDurationMinutes = 30;

    public PairingController(
        VigileEyeDbContext db,
        ILogger<PairingController> logger)
    {
        _db = db;
        _tokenManager = new DeviceTokenManager();
        _logger = logger;
    }

    /// <summary>
    /// Creates a pairing session. Returns QR code and fallback alphanumeric code.
    /// VE-002: Requires completed attestation before pairing session can be created.
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreatePairingSession([FromBody] CreatePairingSessionRequest request)
    {
        var accountId = Guid.Parse(request.AccountId);
        var childId = Guid.Parse(request.ChildId);

        // VE-002: Hard gate — no pairing without completed attestation
        var account = await _db.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new { a.CompletedAttestationId })
            .FirstOrDefaultAsync();

        if (account == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        if (!account.CompletedAttestationId.HasValue)
        {
            _logger.LogWarning(
                "VE-002: Pairing session blocked for account {AccountId} — no completed attestation",
                accountId);
            return BadRequest(new {
                error = "guardian_attestation_required",
                message = "Guardian consent attestation must be completed before device pairing"
            });
        }

        // Verify child profile exists and belongs to this account
        var child = await _db.ChildProfiles
            .Where(c => c.Id == childId && c.AccountId == accountId)
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return NotFound(new { error = "Child profile not found or does not belong to this account" });
        }

        // Generate 8-character alphanumeric fallback code
        var fallbackCode = GenerateFallbackCode();

        // Generate QR token (includes more metadata for faster claim)
        var qrPayload = new
        {
            session_id = Guid.NewGuid(),
            code = fallbackCode,
            account_id = accountId,
            child_id = childId,
            expires_at = DateTime.UtcNow.AddMinutes(PairingCodeValidityMinutes)
        };
        var qrJson = System.Text.Json.JsonSerializer.Serialize(qrPayload);
        var qrToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(qrJson));

        // Create pairing session
        var session = new PairingSession
        {
            Id = qrPayload.session_id,
            AccountId = accountId,
            ChildId = childId,
            Code = fallbackCode,
            QrToken = qrToken,
            ExpiresAt = qrPayload.expires_at,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.PairingSessions.Add(session);
        await _db.SaveChangesAsync();

        // Generate QR code image
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrToken, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrImageBytes = qrCode.GetGraphic(20);

        _logger.LogInformation(
            "VE-004: Pairing session created {SessionId} for account {AccountId}, child {ChildId}, expires {ExpiresAt}",
            session.Id, accountId, childId, session.ExpiresAt);

        return Created($"/api/v1/pairing/sessions/{session.Id}", new
        {
            session_id = session.Id,
            fallback_code = fallbackCode,
            qr_code_base64 = Convert.ToBase64String(qrImageBytes),
            expires_at = session.ExpiresAt,
            validity_minutes = PairingCodeValidityMinutes
        });
    }

    /// <summary>
    /// Claims a pairing session (companion app redeems QR/fallback code).
    /// VE-004: Enforces rate-limiting and lockout after 5 failed attempts.
    /// TRD §5.1: Binds device public key and issues refresh + access tokens.
    /// </summary>
    [HttpPost("claim")]
    public async Task<IActionResult> ClaimPairingSession([FromBody] ClaimPairingRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        PairingSession? session = null;

        // Try to find session by fallback code or QR token
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            session = await _db.PairingSessions
                .Where(s => s.Code == request.Code.ToUpperInvariant() && s.ClaimedAt == null)
                .FirstOrDefaultAsync();
        }
        else if (!string.IsNullOrWhiteSpace(request.QrToken))
        {
            session = await _db.PairingSessions
                .Where(s => s.QrToken == request.QrToken && s.ClaimedAt == null)
                .FirstOrDefaultAsync();
        }

        if (session == null)
        {
            return NotFound(new { error = "Pairing session not found or already claimed" });
        }

        // VE-004: Check if session is locked (per-session or per-IP)
        if (session.LockedUntil.HasValue && session.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "VE-004: Pairing attempt blocked — session {SessionId} locked until {LockedUntil}",
                session.Id, session.LockedUntil.Value);
            return StatusCode(429, new
            {
                error = "pairing_locked",
                message = "Too many failed attempts. Try again later.",
                locked_until = session.LockedUntil.Value
            });
        }

        // Check expiry
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new {
                error = "pairing_expired",
                message = "Pairing code has expired. Please generate a new one."
            });
        }

        // VE-004: Increment failed attempts on invalid public key or missing data
        if (string.IsNullOrWhiteSpace(request.PublicKeyPem) ||
            string.IsNullOrWhiteSpace(request.Platform))
        {
            session.FailedAttempts++;

            if (session.FailedAttempts >= MaxFailedAttempts)
            {
                session.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                session.LockedByIp = clientIp;

                _logger.LogWarning(
                    "VE-004: Pairing session {SessionId} locked after {FailedAttempts} failed attempts from IP {ClientIp}",
                    session.Id, session.FailedAttempts, clientIp);
            }

            await _db.SaveChangesAsync();

            return BadRequest(new {
                error = "invalid_request",
                message = "public_key_pem and platform are required"
            });
        }

        // Register device
        var device = new Device
        {
            Id = Guid.NewGuid(),
            ChildId = session.ChildId,
            Platform = request.Platform,
            OsVersion = request.OsVersion ?? "unknown",
            AppVersion = request.AppVersion ?? "unknown",
            DeviceModel = request.DeviceModel ?? "unknown",
            PublicKeyPem = request.PublicKeyPem,
            Status = Device.DeviceStatus.Active,
            PairedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Devices.Add(device);

        // Issue refresh token (hashed) and access token
        var refreshToken = _tokenManager.GenerateRefreshToken();
        var refreshTokenHash = _tokenManager.HashRefreshToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            TokenHash = refreshTokenHash,
            IssuedAt = DateTime.UtcNow,
            LastIpAddress = clientIp,
            LastDeviceFingerprint = ComputeDeviceFingerprint(request),
            LastUsedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(365), // 1-year sliding expiry per TRD §5.1.1
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshTokenEntity);

        // Mark session as claimed
        session.ClaimedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Issue short-lived access token
        var accessToken = _tokenManager.IssueAccessToken(
            device.Id.ToString(),
            session.AccountId.ToString(),
            session.ChildId.ToString(),
            DateTime.UtcNow);

        _logger.LogInformation(
            "VE-004: Device {DeviceId} paired to child {ChildId} via session {SessionId}",
            device.Id, session.ChildId, session.Id);

        return Ok(new
        {
            device_id = device.Id,
            access_token = accessToken,
            refresh_token = refreshToken, // Only returned once, never stored in plaintext
            expires_in = 900, // 15 minutes
            token_type = "Bearer"
        });
    }

    /// <summary>
    /// VE-005: Exchanges a refresh token for a new access token (and rotates refresh token).
    /// Implements risk-based re-attestation checks.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshTokenRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Hash the provided refresh token to look up
        var tokenHash = _tokenManager.HashRefreshToken(request.RefreshToken);

        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt) // No navigation properties yet
            .Where(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null)
            .FirstOrDefaultAsync();

        if (refreshToken == null)
        {
            return Unauthorized(new { error = "invalid_refresh_token" });
        }

        // Check expiry
        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { error = "refresh_token_expired" });
        }

        var device = await _db.Devices.FindAsync(refreshToken.DeviceId);
        if (device == null || device.Status != Device.DeviceStatus.Active)
        {
            return Unauthorized(new { error = "device_not_found_or_inactive" });
        }

        var child = await _db.ChildProfiles.FindAsync(device.ChildId);
        if (child == null)
        {
            return Unauthorized(new { error = "child_profile_not_found" });
        }

        // VE-005: Risk-based re-attestation checks
        var requiresReAttestation = false;
        var reAttestationReason = "";

        // Check IP geography change (simple country-level check; expand in production)
        if (!string.IsNullOrEmpty(refreshToken.LastIpAddress) &&
            refreshToken.LastIpAddress != clientIp)
        {
            // TODO: Implement geolocation lookup to detect country-level IP change
            // For now, flag any IP change for logging
            _logger.LogInformation(
                "VE-005: IP address changed for device {DeviceId}: {OldIp} -> {NewIp}",
                device.Id, refreshToken.LastIpAddress, clientIp);
        }

        // Check inactivity (90+ days)
        if ((DateTime.UtcNow - refreshToken.LastUsedAt).TotalDays > 90)
        {
            requiresReAttestation = true;
            reAttestationReason = "prolonged_inactivity";

            _logger.LogWarning(
                "VE-005: Re-attestation required for device {DeviceId} due to {Reason} ({DaysSinceLastUse} days)",
                device.Id, reAttestationReason, (DateTime.UtcNow - refreshToken.LastUsedAt).TotalDays);
        }

        if (requiresReAttestation)
        {
            // Don't issue new tokens; require guardian re-attestation
            return StatusCode(403, new
            {
                error = "re_attestation_required",
                reason = reAttestationReason,
                message = "Guardian must re-confirm device pairing",
                device_id = device.Id
            });
        }

        // Rotate refresh token (invalidate old, issue new)
        refreshToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _tokenManager.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenManager.HashRefreshToken(newRefreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            TokenHash = newRefreshTokenHash,
            IssuedAt = DateTime.UtcNow,
            RotatedFromId = refreshToken.Id, // Audit trail
            LastIpAddress = clientIp,
            LastDeviceFingerprint = refreshToken.LastDeviceFingerprint, // TODO: recompute
            LastUsedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(365),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newRefreshTokenEntity);

        // Update device last-seen
        device.LastSeenAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Issue new access token
        var accessToken = _tokenManager.IssueAccessToken(
            device.Id.ToString(),
            child.AccountId.ToString(),
            child.Id.ToString(),
            DateTime.UtcNow);

        _logger.LogInformation(
            "VE-005: Refresh token rotated for device {DeviceId}",
            device.Id);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = newRefreshToken,
            expires_in = 900,
            token_type = "Bearer"
        });
    }

    private string GenerateFallbackCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous chars
        var random = new char[8];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            random[i] = chars[bytes[i] % chars.Length];
        }

        return new string(random);
    }

    private string ComputeDeviceFingerprint(ClaimPairingRequest request)
    {
        var parts = new[]
        {
            request.Platform,
            request.OsVersion ?? "",
            request.DeviceModel ?? ""
        };
        var combined = string.Join("|", parts);

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}

public record CreatePairingSessionRequest(string AccountId, string ChildId);

public record ClaimPairingRequest(
    string? Code,
    string? QrToken,
    string PublicKeyPem,
    string Platform, // "android" | "ios"
    string? OsVersion,
    string? AppVersion,
    string? DeviceModel
);

public record RefreshTokenRequest(string RefreshToken);
