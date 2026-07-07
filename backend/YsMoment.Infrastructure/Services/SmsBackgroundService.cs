using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Data;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// Drains <see cref="SmsQueue"/> and dispatches each job to <see cref="ISmsService"/>,
/// retrying transient failures with backoff. If every attempt fails, the associated
/// order (when there is one) is flagged so staff can see it in the dashboard and follow
/// up manually instead of the failure being silently dropped.
/// </summary>
public class SmsBackgroundService : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

    private readonly SmsQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmsBackgroundService> _logger;

    public SmsBackgroundService(SmsQueue queue, IServiceScopeFactory scopeFactory, ILogger<SmsBackgroundService> logger)
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

    private async Task ProcessWithRetryAsync(SmsJob job, CancellationToken ct)
    {
        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var scope = _scopeFactory.CreateScope();
            var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

            try
            {
                await DispatchAsync(sms, job);
                _logger.LogInformation(
                    "SMS job succeeded. Type={JobType} OrderId={OrderId} Attempt={Attempt}",
                    job.GetType().Name, job.OrderId, attempt);

                if (job.OrderId.HasValue)
                    await SetNotificationFailedAsync(scope, job.OrderId.Value, false, ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "SMS job failed. Type={JobType} OrderId={OrderId} Attempt={Attempt}/{Max}",
                    job.GetType().Name, job.OrderId, attempt, MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    _logger.LogError(
                        ex, "SMS job permanently failed after {Max} attempts. Type={JobType} OrderId={OrderId}",
                        MaxAttempts, job.GetType().Name, job.OrderId);

                    if (job.OrderId.HasValue)
                        await SetNotificationFailedAsync(scope, job.OrderId.Value, true, ct);
                    return;
                }

                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { return; }
                delay *= 3;
            }
        }
    }

    private static Task DispatchAsync(ISmsService sms, SmsJob job) => job switch
    {
        OrderConfirmationSmsJob j => sms.SendOrderConfirmationAsync(j.Phone, j.CustomerName, j.OrderNumber, j.QueuePosition, j.EstimatedMinutes),
        OrderReadySmsJob j => sms.SendOrderReadyAsync(j.Phone, j.CustomerName),
        EventThankYouSmsJob j => sms.SendEventThankYouAsync(j.Phone),
        _ => Task.CompletedTask
    };

    private static async Task SetNotificationFailedAsync(IServiceScope scope, Guid orderId, bool failed, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FindAsync([orderId], ct);
        if (order != null && order.NotificationFailed != failed)
        {
            order.NotificationFailed = failed;
            await db.SaveChangesAsync(ct);
        }
    }
}
