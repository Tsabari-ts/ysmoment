using Microsoft.Extensions.Logging;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// Development stub — logs SMS messages to console/log instead of sending them.
/// Real sending is done by <see cref="Sms4FreeService"/> in non-Development environments.
/// </summary>
public class ConsoleSmsService : ISmsService
{
    private readonly ILogger<ConsoleSmsService> _logger;

    public ConsoleSmsService(ILogger<ConsoleSmsService> logger)
    {
        _logger = logger;
    }

    private const string ContactUrl = "https://api.whatsapp.com/send?phone=972524225365";

    public Task SendOrderConfirmationAsync(string phone, string customerName, int orderNumber, int queuePosition, int estimatedMinutes)
    {
        var message = $"[SMS → {phone}] שלום {customerName}, הזמנתך מספר {orderNumber} התקבלה בהצלחה ואנחנו כבר מתחילים לעבוד עליה! 😊";

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task SendOrderReadyAsync(string phone, string customerName)
    {
        var message = $"[SMS → {phone}] {customerName}, ההזמנה שלך מוכנה! 🎉 בעוד כמה דקות היא תופיע על לוח המגנטים.";

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task SendEventThankYouAsync(string phone, string ratingUrl)
    {
        var message = $"[SMS → {phone}] תודה שהשתתפתם באירוע ושהשתמשתם בשירות הברקוד שלנו! 📸 אם תרצו שנגיע גם לאירוע שלכם, נשמח לשמוע ממכם — {ContactUrl}";

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}
