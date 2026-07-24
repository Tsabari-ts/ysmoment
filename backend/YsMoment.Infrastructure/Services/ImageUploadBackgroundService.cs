using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YsMoment.Core.Enums;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Data;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// Drains <see cref="ImageUploadQueue"/> and uploads each order's image to storage in the
/// background, retrying transient failures with backoff — the same shape as
/// <see cref="SmsBackgroundService"/>. The order is created with <see cref="OrderStatus.PendingUpload"/>
/// before the image reaches storage; this service flips it to <see cref="OrderStatus.New"/> once
/// the upload finishes (or after final failure, so the order isn't stuck invisible to staff).
/// </summary>
public class ImageUploadBackgroundService : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

    private readonly ImageUploadQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImageUploadBackgroundService> _logger;

    public ImageUploadBackgroundService(ImageUploadQueue queue, IServiceScopeFactory scopeFactory, ILogger<ImageUploadBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(job, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(ImageUploadJob job, CancellationToken ct)
    {
        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var scope = _scopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();

            try
            {
                using var stream = new MemoryStream(job.ImageBytes);
                var imagePath = await storage.SaveAsync(stream, job.FileName, job.EventId, job.OrderId);

                _logger.LogInformation(
                    "Image upload succeeded. OrderId={OrderId} Attempt={Attempt}", job.OrderId, attempt);

                await CompleteOrderAsync(scope, job.OrderId, imagePath, ct);
                await NotifyAsync(scope, job.EventId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Image upload failed. OrderId={OrderId} Attempt={Attempt}/{Max}",
                    job.OrderId, attempt, MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    _logger.LogError(
                        ex, "Image upload permanently failed after {Max} attempts. OrderId={OrderId}",
                        MaxAttempts, job.OrderId);

                    await MarkUploadFailedAsync(scope, job.OrderId, ct);
                    await NotifyAsync(scope, job.EventId);
                    return;
                }

                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { return; }
                delay *= 3;
            }
        }
    }

    private static async Task CompleteOrderAsync(IServiceScope scope, Guid orderId, string imagePath, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FindAsync([orderId], ct);
        if (order == null) return;

        order.ImagePath = imagePath;
        order.Status = OrderStatus.New;
        order.ImageUploadFailed = false;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task MarkUploadFailedAsync(IServiceScope scope, Guid orderId, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FindAsync([orderId], ct);
        if (order == null) return;

        // Still surface the order to staff even though there is no photo — they can follow up manually.
        order.Status = OrderStatus.New;
        order.ImageUploadFailed = true;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task NotifyAsync(IServiceScope scope, Guid eventId)
    {
        var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
        await notifier.NotifyEventUpdateAsync(eventId);
    }
}
