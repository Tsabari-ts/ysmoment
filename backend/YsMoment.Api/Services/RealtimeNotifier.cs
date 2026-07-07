using Microsoft.AspNetCore.SignalR;
using YsMoment.Api.Hubs;
using YsMoment.Core.DTOs;
using YsMoment.Infrastructure.Services;

namespace YsMoment.Api.Services;

public class RealtimeNotifier
{
    private readonly IHubContext<OrderHub> _hub;
    private readonly OrderService _orders;

    public RealtimeNotifier(IHubContext<OrderHub> hub, OrderService orders)
    {
        _hub = hub;
        _orders = orders;
    }

    public async Task NotifyEventUpdateAsync(Guid eventId)
    {
        var orders = await _orders.GetAllOrdersAsync(eventId);
        var stats = await _orders.GetStatsAsync(eventId);
        var payload = new QueueUpdatePayload(orders, stats);
        await _hub.Clients.Group(eventId.ToString()).SendAsync("QueueUpdated", payload);
    }
}
