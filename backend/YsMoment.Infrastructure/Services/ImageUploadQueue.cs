using System.Threading.Channels;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// In-memory queue so the guest-facing order request doesn't block on the image upload
/// to storage. Mirrors <see cref="SmsQueue"/> — a single background worker
/// (<see cref="ImageUploadBackgroundService"/>) drains it with retries. Not durable across
/// process restarts — acceptable for this scale; if that ever matters, swap this for a
/// persisted queue without touching callers.
/// </summary>
public class ImageUploadQueue : IImageUploadQueue
{
    private readonly Channel<ImageUploadJob> _channel = Channel.CreateUnbounded<ImageUploadJob>();

    public ChannelReader<ImageUploadJob> Reader => _channel.Reader;

    public void Enqueue(ImageUploadJob job) => _channel.Writer.TryWrite(job);
}
