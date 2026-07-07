using System.Threading.Channels;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// In-memory queue so a slow/failed SMS provider call never blocks the guest-facing
/// order flow. A single background worker (<see cref="SmsBackgroundService"/>) drains
/// it with retries. Not durable across process restarts — acceptable for this scale;
/// if that ever matters, swap this for a persisted queue without touching callers.
/// </summary>
public class SmsQueue : ISmsQueue
{
    private readonly Channel<SmsJob> _channel = Channel.CreateUnbounded<SmsJob>();

    public ChannelReader<SmsJob> Reader => _channel.Reader;

    public void Enqueue(SmsJob job) => _channel.Writer.TryWrite(job);
}
