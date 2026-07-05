using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YsMoment.Core.DTOs;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Api.Controllers;

[ApiController]
[Route("api")]
public class PublicOrderController : ControllerBase
{
    private readonly OrderService _orders;

    public PublicOrderController(OrderService orders) => _orders = orders;

    [HttpGet("o/{token}")]
    public async Task<ActionResult<PublicOrderView>> GetByToken(string token)
    {
        var order = await _orders.GetByPublicTokenAsync(token);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPut("o/{token}")]
    public async Task<ActionResult<PublicOrderView>> Update(string token, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var order = await _orders.UpdateByTokenAsync(token, request);
            if (order == null) return NotFound();
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPost("o/{token}/cancel")]
    public async Task<ActionResult<PublicOrderView>> Cancel(string token)
    {
        var order = await _orders.CancelByTokenAsync(token);
        if (order == null) return BadRequest(new { message = "לא ניתן לבטל הזמנה זו." });
        return Ok(order);
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPost("events/{slug}/orders/validate-tokens")]
    public async Task<ActionResult<List<OrderTokenSummary>>> ValidateTokens(string slug, [FromBody] ValidateTokensRequest request)
        => Ok(await _orders.ValidateTokensAsync(slug, request.Tokens));

    [EnableRateLimiting("guest-orders")]
    [HttpPost("events/{slug}/orders/send-code")]
    public async Task<ActionResult> SendCode(string slug, [FromBody] SendRecoveryCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "יש להזין מספר טלפון." });
        await _orders.SendRecoveryCodeAsync(slug, request.Phone);
        return Ok(new { message = "קוד אימות נשלח." });
    }

    [EnableRateLimiting("guest-orders")]
    [HttpPost("events/{slug}/orders/recover")]
    public async Task<ActionResult<List<OrderTokenSummary>>> Recover(string slug, [FromBody] RecoverOrdersRequest request)
    {
        try
        {
            var orders = await _orders.RecoverOrdersAsync(slug, request.Phone, request.Code);
            return Ok(orders);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
