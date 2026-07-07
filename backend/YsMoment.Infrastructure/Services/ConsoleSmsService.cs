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

    // Client-approved final copy — exactly 134 characters, tuned to stay within a
    // single SMS segment. Do not append anything to it (signature, extra link, etc.)
    // without re-testing the length through the actual provider: emoji and non-GSM
    // characters can silently shrink the per-segment budget and cause a silent split.
    private const string EventEndedMessage =
        "תודה שהשתתפתם באירוע והשתמשתם בשירות הברקוד שלנו📸 רוצים אותנו באירוע שלכם? בואו נדבר https://api.whatsapp.com/send?phone=972524225365";

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

    public Task SendEventThankYouAsync(string phone)
    {
        var message = $"[SMS → {phone}] {EventEndedMessage}";

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}
