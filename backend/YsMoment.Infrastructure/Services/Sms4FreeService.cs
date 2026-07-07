using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

/// <summary>
/// Real SMS sending via sms4free.co.il's ApiSMS/SendSMS endpoint.
/// API contract (POST JSON to https://api.sms4free.co.il/ApiSMS/SendSMS):
///   request:  { key, user, pass, sender, recipient, msg }
///   response: a bare integer — positive is a message id (success), 0 or negative is an error code.
/// </summary>
public class Sms4FreeService : ISmsService
{
    private const string SendUrl = "https://api.sms4free.co.il/ApiSMS/SendSMS";

    private readonly HttpClient _http;
    private readonly ILogger<Sms4FreeService> _logger;
    private readonly string _key;
    private readonly string _user;
    private readonly string _pass;
    private readonly string _sender;

    public Sms4FreeService(HttpClient http, IConfiguration config, ILogger<Sms4FreeService> logger)
    {
        _http = http;
        _logger = logger;
        _key = config["Sms4Free:Key"] ?? throw new InvalidOperationException("Sms4Free:Key is required.");
        _user = config["Sms4Free:User"] ?? throw new InvalidOperationException("Sms4Free:User is required.");
        _pass = config["Sms4Free:Pass"] ?? throw new InvalidOperationException("Sms4Free:Pass is required.");
        _sender = config["Sms4Free:Sender"] ?? throw new InvalidOperationException("Sms4Free:Sender is required.");
    }

    // Client-approved final copy — exactly 134 characters, tuned to stay within a
    // single SMS segment. Do not append anything to it (signature, extra link, etc.)
    // without re-testing the length through the actual provider: emoji and non-GSM
    // characters can silently shrink the per-segment budget and cause a silent split.
    private const string EventEndedMessage =
        "תודה שהשתתפתם באירוע והשתמשתם בשירות הברקוד שלנו📸 רוצים אותנו באירוע שלכם? בואו נדבר https://api.whatsapp.com/send?phone=972524225365";

    public Task SendOrderConfirmationAsync(string phone, string customerName, int orderNumber, int queuePosition, int estimatedMinutes)
        => SendAsync(phone, $"שלום {customerName}, הזמנתך מספר {orderNumber} התקבלה בהצלחה ואנחנו כבר מתחילים לעבוד עליה! 😊");

    public Task SendOrderReadyAsync(string phone, string customerName)
        => SendAsync(phone, $"{customerName}, ההזמנה שלך מוכנה! 🎉 בעוד כמה דקות היא תופיע על לוח המגנטים.");

    public Task SendEventThankYouAsync(string phone)
        => SendAsync(phone, EventEndedMessage);

    private async Task SendAsync(string phone, string message)
    {
        var payload = new { key = _key, user = _user, pass = _pass, sender = _sender, recipient = phone, msg = message };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(SendUrl, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS4Free request failed (network/transport error). Phone={Phone}", phone);
            throw;
        }

        var body = (await response.Content.ReadAsStringAsync()).Trim();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SMS4Free HTTP error. Phone={Phone} HttpStatus={Status} Body={Body}", phone, response.StatusCode, body);
            throw new InvalidOperationException($"SMS4Free HTTP {(int)response.StatusCode}: {body}");
        }

        if (!int.TryParse(body, out var code) || code <= 0)
        {
            _logger.LogError(
                "SMS4Free send failed. Phone={Phone} Code={Code} Body={Body} Reason={Reason}",
                phone, code, body, GetErrorMessage(code));
            throw new InvalidOperationException($"SMS4Free error {code}: {GetErrorMessage(code)}");
        }

        _logger.LogInformation("SMS sent via SMS4Free. Phone={Phone} MessageId={MessageId}", phone, code);
    }

    private static string GetErrorMessage(int code) => code switch
    {
        0 => "שגיאה כללית",
        -1 => "מפתח, שם משתמש או סיסמה שגויים",
        -2 => "שם או מספר שולח ההודעה שגוי",
        -3 => "לא נמצאו נמענים",
        -4 => "יתרת הודעות פנויות נמוכה",
        -5 => "הודעה לא מתאימה",
        -6 => "צריך לאמת מספר שולח",
        _ => "שגיאה לא ידועה"
    };
}
