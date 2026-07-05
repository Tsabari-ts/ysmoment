using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YsMoment.Api.Services;
using YsMoment.Core.DTOs;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _events;
    private readonly QrCodeService _qr;
    private readonly RealtimeNotifier _notifier;

    public EventsController(EventService events, QrCodeService qr, RealtimeNotifier notifier)
    {
        _events = events;
        _qr = qr;
        _notifier = notifier;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create([FromBody] CreateEventRequest request)
    {
        var evt = await _events.CreateAsync(request);
        var withQr = evt with { QrCodeBase64 = _qr.GenerateBase64(evt.GuestUrl) };
        return CreatedAtAction(nameof(GetById), new { id = evt.Id }, withQr);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventResponse>> GetById(Guid id)
    {
        var evt = await _events.GetByIdAsync(id);
        if (evt == null) return NotFound();
        return Ok(evt with { QrCodeBase64 = _qr.GenerateBase64(evt.GuestUrl) });
    }

    [HttpGet("guest/{slug}")]
    public async Task<ActionResult<GuestEventResponse>> GetGuestEvent(string slug)
    {
        var evt = await _events.GetGuestEventAsync(slug);
        if (evt == null) return NotFound();
        return Ok(evt);
    }

    [Authorize]
    [HttpPatch("{id:guid}/settings")]
    public async Task<ActionResult<EventResponse>> UpdateSettings(Guid id, [FromBody] UpdateEventSettingsRequest request)
    {
        var evt = await _events.UpdateSettingsAsync(id, request);
        if (evt == null) return NotFound();
        await _notifier.NotifyEventUpdateAsync(id);
        return Ok(evt);
    }

    [Authorize]
    [HttpPost("{id:guid}/end")]
    public async Task<ActionResult<EventSummaryResponse>> EndEvent(Guid id, [FromQuery] string ratingUrl = "https://g.page/r/yourstudio")
    {
        var summary = await _events.EndEventAsync(id, ratingUrl);
        if (summary == null) return NotFound();
        await _notifier.NotifyEventUpdateAsync(id);
        return Ok(summary);
    }

    [Authorize]
    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<EventSummaryResponse>> GetSummary(Guid id)
    {
        var summary = await _events.GetSummaryAsync(id);
        if (summary == null) return NotFound();
        return Ok(summary);
    }

    [Authorize]
    [HttpGet("{id:guid}/qr")]
    public async Task<ActionResult> GetQr(Guid id)
    {
        var evt = await _events.GetByIdAsync(id);
        if (evt == null) return NotFound();
        var base64 = _qr.GenerateBase64(evt.GuestUrl);
        return Ok(new { qrCodeBase64 = base64, guestUrl = evt.GuestUrl });
    }
}
