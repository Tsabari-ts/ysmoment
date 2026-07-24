using Microsoft.Extensions.Configuration;
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
    private readonly string _eventEndedMessage;

    public ConsoleSmsService(ILogger<ConsoleSmsService> logger, IConfiguration config)
    {
        _logger = logger;
        _eventEndedMessage = BuildEventEndedMessage(config["App:LandingPageUrl"]);
    }

    // Client-approved copy, kept close to the original 134-character single-segment length.
    // Do not append anything beyond the link (signature, extra text, etc.) without re-testing
    // the length through the actual provider: emoji and non-GSM characters can silently shrink
    // the per-segment budget and cause a silent split.
    private static string BuildEventEndedMessage(string? landingPageUrl) =>
        $"תודה שהשתתפתם באירוע והשתמשתם בשירות הברקוד שלנו📸 רוצים אותנו באירוע שלכם? בקרו באתר שלנו: {landingPageUrl ?? "https://ysmoment.vercel.app/"}";

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
        var message = $"[SMS → {phone}] {_eventEndedMessage}";

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}
