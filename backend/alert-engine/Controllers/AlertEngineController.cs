using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using VE.Shared.Data;
using VE.Shared.Models;

namespace VE.AlertEngine.Controllers;

/// <summary>
/// VE-021: Full rules engine — AlertRule DB queries + Redis event_queue consumer.
/// Per VE-003 (invariants.md): operates only on event metadata (type, device, occurred_at).
/// No message body/content evaluated; keyword match feature-gated until VE-001 sign-off.
/// Per invariant #1: no hidden disable path — indicator stays visible; tamper triggers alert.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertEngineController : ControllerBase
{
    private readonly VigileEyeDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public AlertEngineController(VigileEyeDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    /// <summary>
    /// Query active alert rules for a child (DB layer — full query, not scaffold).
    /// </summary>
    [HttpGet("rules/{childId}")]
    public async Task<IActionResult> GetActiveRules(Guid childId)
    {
        var rules = await _db.AlertRules
            .Where(r => r.ChildId == childId && r.Enabled == true)
            .Select(r => new
            {
                r.Id,
                r.Type,
                r.ConfigJson,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            child_id = childId,
            count = rules.Count,
            rules = rules,
            note = "VE-003 / invariants.md: rules evaluate metadata only; no SMS/call content",
            ve = "VE-021"
        });
    }

    /// <summary>
    /// Consume one event from Redis event_queue (ingestion published) and evaluate rules.
    /// Per VE-003: only event_type, device_id, occurred_at are used — never decrypted content.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateFromQueue()
    {
        var redisDb = _redis.GetDatabase();
        var raw = await redisDb.ListRightPopAsync("event_queue");
        if (raw.IsNullOrEmpty)
        {
            return Ok(new { result = "queue_empty", note = "No events to evaluate" });
        }

        var evt = JsonSerializer.Deserialize<JsonElement>(raw!);
        var eventId = evt.TryGetProperty("event_id", out var idProp) ? Guid.Parse(idProp.GetString()!) : Guid.Empty;
        var deviceIdStr = evt.TryGetProperty("device_id", out var devProp) ? devProp.GetString() : null;
        var eventTypeStr = evt.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

        // Load device to find child
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == Guid.Parse(deviceIdStr ?? Guid.Empty.ToString()));
        if (device == null)
        {
            return BadRequest(new { error = "unknown_device" });
        }

        // Full DB query for active rules on this child
        var rules = await _db.AlertRules
            .Where(r => r.ChildId == device.ChildId && r.Enabled == true)
            .ToListAsync();

        // Metadata-only evaluation: only match event type against rule type
        // No content payload accessed (VE-003 invariant preserved)
        var matched = rules
            .Where(r => r.Type.ToString().Equals(eventTypeStr, StringComparison.OrdinalIgnoreCase))
            .Select(r => new { r.Id, r.Type })
            .ToList();

        return Ok(new
        {
            event_id = eventId,
            device_id = deviceIdStr,
            event_type = eventTypeStr,
            matched_rules = matched,
            rule_count = rules.Count,
            metadata_only_evaluation = true,
            ve = "VE-021",
            guard = "VE-003: no content field accessed; alert rules on metadata only"
        });
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { service = "alert-engine", status = "healthy", ve = "VE-021", mode = "full-rules-engine" });
}
