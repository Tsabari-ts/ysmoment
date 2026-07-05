using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YsMoment.Api.Models;
using YsMoment.Api.Services;
using YsMoment.Core.DTOs;
using YsMoment.Core.Enums;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Api.Controllers;

[ApiController]
[Route("api")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orders;
    private readonly IImageValidator _validator;
    private readonly RealtimeNotifier _notifier;

    public OrdersController(OrderService orders, IImageValidator validator, RealtimeNotifier notifier)
    {
        _orders = orders;
        _validator = validator;
        _notifier = notifier;
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPost("events/{slug}/orders")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<OrderResponse>> Create(string slug, [FromForm] CreateOrderForm form, IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest(new { message = "יש להעלות תמונה." });

        await using var stream = image.OpenReadStream();
        var (valid, error) = _validator.Validate(stream, image.FileName, image.Length);
        if (!valid) return BadRequest(new { message = error });

        var request = new CreateOrderRequest(
            form.CustomerName, form.Phone, form.MagnetSize, form.Quantity, form.PrivacyAccepted);

        try
        {
            stream.Position = 0;
            var order = await _orders.CreateAsync(slug, request, stream, image.FileName);
            if (order == null) return NotFound();
            await _notifier.NotifyEventUpdateAsync(order.EventId);
            return Created($"/api/o/{order.PublicToken}", order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [EnableRateLimiting("guest-read")]
    [HttpGet("orders/{orderId:guid}/status")]
    public async Task<ActionResult<OrderStatusResponse>> GetStatus(Guid orderId)
    {
        var status = await _orders.GetStatusAsync(orderId);
        if (status == null) return NotFound();
        return Ok(status);
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPut("orders/{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> Update(Guid orderId, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var order = await _orders.UpdateAsync(orderId, request);
            if (order == null) return NotFound();
            await _notifier.NotifyEventUpdateAsync(order.EventId);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPost("orders/{orderId:guid}/cancel")]
    public async Task<ActionResult> Cancel(Guid orderId)
    {
        var order = await _orders.GetStatusAsync(orderId);
        var ok = await _orders.CancelAsync(orderId);
        if (!ok) return BadRequest(new { message = "לא ניתן לבטל הזמנה זו." });
        if (order != null)
            await _notifier.NotifyEventUpdateAsync(order.EventId);
        return NoContent();
    }

    [Authorize]
    [HttpGet("events/{eventId:guid}/orders")]
    public async Task<ActionResult<List<OrderResponse>>> GetQueue(Guid eventId, [FromQuery] bool all = false)
        => Ok(all ? await _orders.GetAllOrdersAsync(eventId) : await _orders.GetQueueAsync(eventId));

    [Authorize]
    [HttpGet("events/{eventId:guid}/orders/search")]
    public async Task<ActionResult<List<OrderResponse>>> Search(Guid eventId, [FromQuery] string q)
        => Ok(await _orders.SearchAsync(eventId, q));

    [Authorize]
    [HttpGet("events/{eventId:guid}/stats")]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats(Guid eventId)
        => Ok(await _orders.GetStatsAsync(eventId));

    [Authorize]
    [HttpGet("orders/{orderId:guid}/image")]
    public async Task<IActionResult> GetImage(Guid orderId)
    {
        var file = await _orders.GetImageFileAsync(orderId);
        if (file != null)
            return PhysicalFile(file.Value.FullPath, file.Value.ContentType, file.Value.FileName);

        // Cloud storage: redirect to CDN URL
        var redirectUrl = await _orders.GetImageRedirectUrlAsync(orderId);
        if (redirectUrl != null) return Redirect(redirectUrl);
        return NotFound();
    }

    [Authorize]
    [HttpPatch("orders/{orderId:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(Guid orderId, [FromBody] UpdateStatusRequest body)
    {
        var order = await _orders.UpdateStatusAsync(orderId, body.Status);
        if (order == null) return NotFound();
        await _notifier.NotifyEventUpdateAsync(order.EventId);
        return Ok(order);
    }
}

public record UpdateStatusRequest(OrderStatus Status);
