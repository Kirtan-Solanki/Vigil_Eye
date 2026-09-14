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

    [HttpPost("rule")]
    public IActionResult CreateRule([FromBody] object rule)
    {
        // TODO: full rule engine in Phase 1; scaffold for VE-021
        return Created("/api/alerts/rule/0", new { msg = "Rule created (scaffold)", ve = "VE-021", note = "Metadata-only; no content capture" });
    }
}
