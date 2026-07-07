namespace YsMoment.Core.Interfaces;

public abstract record SmsJob(Guid? OrderId);

public sealed record OrderConfirmationSmsJob(
    Guid? OrderId, string Phone, string CustomerName, int OrderNumber, int QueuePosition, int EstimatedMinutes) : SmsJob(OrderId);

public sealed record OrderReadySmsJob(Guid? OrderId, string Phone, string CustomerName) : SmsJob(OrderId);

public sealed record EventThankYouSmsJob(Guid? OrderId, string Phone, string RatingUrl) : SmsJob(OrderId);

public interface ISmsQueue
{
    void Enqueue(SmsJob job);
}
