using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YsMoment.Api.Services;
using YsMoment.Core.DTOs;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _events;
    private readonly OrderService _orders;
    private readonly QrCodeService _qr;
    private readonly RealtimeNotifier _notifier;

    public EventsController(EventService events, OrderService orders, QrCodeService qr, RealtimeNotifier notifier)
    {
        _events = events;
        _orders = orders;
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

    // Merges the getEvent/getOrders(all=true)/getStats calls the dashboard used to make
    // separately into one round trip, loading the Event once and reusing it for all three.
    [Authorize]
    [HttpGet("{id:guid}/dashboard")]
    public async Task<ActionResult<EventDashboardResponse>> GetDashboard(Guid id)
    {
        var evt = await _events.GetEntityAsync(id);
        if (evt == null) return NotFound();

        var eventResponse = _events.ToEventResponse(evt);
        eventResponse = eventResponse with { QrCodeBase64 = _qr.GenerateBase64(eventResponse.GuestUrl) };
        var orders = await _orders.GetAllOrdersAsync(evt);
        var stats = await _orders.GetStatsAsync(evt);

        return Ok(new EventDashboardResponse(eventResponse, orders, stats));
    }

    [EnableRateLimiting("guest-read")]
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
    public async Task<ActionResult<EventSummaryResponse>> EndEvent(Guid id)
    {
        var summary = await _events.EndEventAsync(id);
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
