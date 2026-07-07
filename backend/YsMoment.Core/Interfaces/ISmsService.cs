namespace YsMoment.Core.Interfaces;

public interface ISmsService
{
    Task SendOrderConfirmationAsync(string phone, string customerName, int orderNumber, int queuePosition, int estimatedMinutes);
    Task SendOrderReadyAsync(string phone, string customerName);
    Task SendEventThankYouAsync(string phone, string ratingUrl);
}
