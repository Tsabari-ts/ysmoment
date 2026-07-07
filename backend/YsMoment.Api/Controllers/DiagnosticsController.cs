using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace YsMoment.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(ILogger<DiagnosticsController> logger)
    {
        _logger = logger;
    }

    // Lets the guest client report "page didn't load" so the team can see it happened,
    // instead of it failing silently on a device we never hear about.
    [EnableRateLimiting("guest-read")]
    [HttpPost("client-error")]
    public IActionResult ClientError([FromBody] ClientErrorReport report)
    {
        _logger.LogWarning(
            "Guest client reported a load issue. Slug={Slug} State={State} Online={Online} Url={Url} UserAgent={UserAgent} ClientTimestamp={ClientTimestamp}",
            report.Slug, report.State, report.Online, report.Url, report.UserAgent, report.Timestamp);
        return Ok();
    }
}

public record ClientErrorReport(string? Slug, string? Url, string? UserAgent, bool? Online, string? Timestamp, string? State);
