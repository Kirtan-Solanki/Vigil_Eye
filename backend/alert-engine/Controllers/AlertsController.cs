using Microsoft.AspNetCore.Mvc;
using VE.AlertEngine; // placeholder namespace

namespace VE.AlertEngine.Controllers;

/// <summary>
/// VE-021: Alert-engine endpoint (metadata-only events).
/// Per VE-003 / invariants: no raw content exposed; alert rules only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { service = "alert-engine", status = "healthy", ve = "VE-021" });

    [HttpGet("rules")]
    public IActionResult ListRules()
    {
        // VE-021: Return metadata-only alert rules (no message content)
        // Per VE-003 / invariants.md: never expose raw content captured by events
        return Ok(new
        {
            rules = new[]
            {
                new { id = "r1", type = "GeofenceExit", enabled = true, child_id = "1" },
                new { id = "r2", type = "TamperDetected", enabled = true, child_id = "1" }
            },
            meta_only = true,
            note = "VE-003 enforced: rules operate on event metadata; no SMS/call content evaluated",
            ve = "VE-021"
        });
    }

    [HttpPost("rule")]
    public IActionResult CreateRule([FromBody] object rule)
    {
        // TODO: full rule engine in Phase 1; scaffold for VE-021
        return Created("/api/alerts/rule/0", new { msg = "Rule created (scaffold)", ve = "VE-021", note = "Metadata-only; no content capture" });
    }
}
