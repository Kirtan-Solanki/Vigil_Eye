using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VE.Shared.Data;
using VE.Shared.Models;

namespace VE.AuthPairing.Controllers;

/// <summary>
/// VE-002: Guardian consent attestation API.
/// Implements server-enforced attestation requirement before any pairing session can be created.
/// Security Assessment §4.3: Immutable, timestamped, hard prerequisite.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AttestationsController : ControllerBase
{
    private readonly VigileEyeDbContext _db;
    private readonly ILogger<AttestationsController> _logger;

    public AttestationsController(VigileEyeDbContext db, ILogger<AttestationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Creates a guardian attestation record.
    /// This is a prerequisite for creating any pairing session.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAttestation([FromBody] CreateAttestationRequest request)
    {
        // TODO: In production, require authenticated guardian account
        // For Phase 0, we'll accept account_id in request body

        if (string.IsNullOrWhiteSpace(request.AccountId))
        {
            return BadRequest(new { error = "account_id is required" });
        }

        var accountId = Guid.Parse(request.AccountId);

        // Verify account exists
        var accountExists = await _db.Accounts.AnyAsync(a => a.Id == accountId);
        if (!accountExists)
        {
            return NotFound(new { error = "Account not found" });
        }

        // Create immutable attestation record
        var attestation = new GuardianAttestation
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            AttestationText = request.AttestationText,
            AttestationTimestamp = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _db.GuardianAttestations.Add(attestation);

        // Update account to reference this attestation
        var account = await _db.Accounts.FindAsync(accountId);
        if (account != null)
        {
            account.CompletedAttestationId = attestation.Id;
            account.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "VE-002: Guardian attestation created for account {AccountId} at {Timestamp}",
            accountId, attestation.AttestationTimestamp);

        return Created($"/api/v1/attestations/{attestation.Id}", new
        {
            id = attestation.Id,
            account_id = attestation.AccountId,
            attestation_timestamp = attestation.AttestationTimestamp
        });
    }

    /// <summary>
    /// Retrieves attestation history for an account (read-only, audit purposes).
    /// </summary>
    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetAccountAttestations(string accountId)
    {
        var accountGuid = Guid.Parse(accountId);

        var attestations = await _db.GuardianAttestations
            .Where(a => a.AccountId == accountGuid)
            .OrderByDescending(a => a.AttestationTimestamp)
            .Select(a => new
            {
                id = a.Id,
                attestation_text = a.AttestationText,
                attestation_timestamp = a.AttestationTimestamp,
                ip_address = a.IpAddress,
                created_at = a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { attestations });
    }
}

public record CreateAttestationRequest(
    string AccountId,
    string AttestationText
);
