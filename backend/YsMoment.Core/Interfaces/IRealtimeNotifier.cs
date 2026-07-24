namespace YsMoment.Core.Interfaces;

/// <summary>
/// Pushes a live queue/stats update to dashboard clients for an event. Implemented in the API
/// layer (SignalR hub lives there) so background services in Infrastructure — which cannot
/// reference the API project — can still notify the dashboard after an async state change.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyEventUpdateAsync(Guid eventId);
}
