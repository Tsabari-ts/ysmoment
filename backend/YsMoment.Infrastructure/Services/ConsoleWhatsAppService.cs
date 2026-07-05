using Microsoft.Extensions.Logging;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// Stub implementation — logs WhatsApp messages to console.
/// Replace with real provider (Twilio, Green API, etc.) when ready.
/// </summary>
public class ConsoleWhatsAppService : IWhatsAppService
{
    private readonly ILogger<ConsoleWhatsAppService> _logger;

    public ConsoleWhatsAppService(ILogger<ConsoleWhatsAppService> logger)
    {
        _logger = logger;
    }

    public Task SendOrderConfirmationAsync(string phone, string customerName, int orderNumber, int queuePosition, int estimatedMinutes)
    {
        var message = $"""
            [WhatsApp → {phone}]
            היי {customerName} ❤️
            הזמנתך מספר #{orderNumber} התקבלה בהצלחה

            לפניך {queuePosition} הזמנות
            זמן משוער: {estimatedMinutes} דקות
            """;

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task SendOrderReadyAsync(string phone, string customerName)
    {
        var message = $"""
            [WhatsApp → {phone}]
            היי {customerName} ❤️
            המגנט שלך מוכן ומחכה לך בלוח
            """;

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task SendEventThankYouAsync(string phone, string ratingUrl)
    {
        var message = $"""
            [WhatsApp → {phone}]
            תודה שהשתמשת בשירות שלנו ❤️
            נשמח אם תדרג אותנו

            [כפתור דירוג] {ratingUrl}

            להזמנת אירועים נוספים:
            yourstudio.co.il
            """;

        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}
