using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using VE.Shared.Data;
using VE.Shared.Encryption;
using VE.Shared.Models;

namespace VE.Ingestion.Controllers;

/// <summary>
/// VE-007: Event ingestion API.
/// TRD §10.1: Companion app batch-uploads queued events.
/// VE-003: Events stored with metadata-only fields (no message content in v1).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly VigileEyeDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        VigileEyeDbContext db,
        IConnectionMultiplexer redis,
        ILogger<EventsController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Batch upload events from companion app.
    /// TRD §10.1: Backend acknowledges receipt only after durable write.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> IngestEvents([FromBody] IngestEventsRequest request)
    {
        // TODO: Extract device_id from authenticated access token
        // For Phase 0, accept device_id in request body

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest(new { error = "device_id is required" });
        }

        var deviceId = Guid.Parse(request.DeviceId);

        // Verify device exists and is active
        var device = await _db.Devices
            .Where(d => d.Id == deviceId && d.Status == Device.DeviceStatus.Active)
            .FirstOrDefaultAsync();

        if (device == null)
        {
            return Unauthorized(new { error = "Device not found or inactive" });
        }

        var events = new List<Event>();
        var dataEncryptionKey = EnvelopeEncryption.GenerateDataKey(); // TODO: Retrieve from KMS

        foreach (var eventData in request.Events)
        {
            // VE-003: Validate that SMS events contain only metadata (no content field)
            if (eventData.Type == "sms_meta")
            {
                var payloadJson = eventData.Payload is JsonElement je ? je.GetRawText() : eventData.Payload?.ToString();
                if (!string.IsNullOrEmpty(payloadJson))
                {
                    using var doc = JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("content", out _))
                    {
                        _logger.LogWarning(
                            "VE-003: Rejected SMS event from device {DeviceId} — content field present in v1 (metadata-only)",
                            deviceId);
                        return BadRequest(new {
                            error = "invalid_event",
                            message = "SMS events must be metadata-only in v1 (sender, recipient, timestamp). Content capture is feature-gated."
                        });
                    }
                }
            }

            // Encrypt payload
            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(eventData.Payload);
            var (ciphertext, iv, authTag) = EnvelopeEncryption.Encrypt(payloadBytes, dataEncryptionKey);

            var eventEntity = new Event
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                Type = eventData.Type switch
                {
                    "call" => Event.EventType.Call,
                    "sms_meta" => Event.EventType.SmsMeta,
                    "location" => Event.EventType.Location,
                    "app_usage" => Event.EventType.AppUsage,
                    "web_history" => Event.EventType.WebHistory,
                    "device_status" => Event.EventType.DeviceStatus,
                    "tamper_detected" => Event.EventType.TamperDetected,
                    _ => throw new ArgumentException($"Unknown event type: {eventData.Type}")
                },
                EncryptedPayload = ciphertext,
                EncryptionIv = iv,
                EncryptionAuthTag = authTag,
                OccurredAt = eventData.OccurredAt,
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            events.Add(eventEntity);
        }

        // Durable write to database
        _db.Events.AddRange(events);
        await _db.SaveChangesAsync();

        // Publish to Redis queue for alert engine (VE-007: decoupled pipeline)
        var redisDb = _redis.GetDatabase();
        foreach (var evt in events)
        {
            var message = JsonSerializer.Serialize(new
            {
                event_id = evt.Id,
                device_id = evt.DeviceId,
                type = evt.Type.ToString(),
                occurred_at = evt.OccurredAt
            });
            await redisDb.ListLeftPushAsync("event_queue", message);
        }

        // Update device last-seen
        device.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "VE-007: Ingested {EventCount} events from device {DeviceId}",
            events.Count, deviceId);

        return Ok(new
        {
            ingested_count = events.Count,
            synced_at = DateTime.UtcNow
        });
    }
}

public record IngestEventsRequest(
    string DeviceId,
    List<EventData> Events
);

public record EventData(
    string Type,
    object Payload,
    DateTime OccurredAt
);
