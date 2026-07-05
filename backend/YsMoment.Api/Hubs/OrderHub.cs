using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace YsMoment.Api.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public async Task JoinEvent(string eventId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, eventId);

    public async Task LeaveEvent(string eventId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId);
}
