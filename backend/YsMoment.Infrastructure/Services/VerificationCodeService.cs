using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace YsMoment.Infrastructure.Services;

public class VerificationCodeService
{
    private readonly ConcurrentDictionary<string, (string Code, DateTime Expires)> _codes = new();
    private readonly ILogger<VerificationCodeService> _logger;

    public VerificationCodeService(ILogger<VerificationCodeService> logger) => _logger = logger;

    public string GenerateAndStore(string eventSlug, string phone)
    {
        var key = Key(eventSlug, phone);
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        _codes[key] = (code, DateTime.UtcNow.AddMinutes(5));

        var message = $"""
            [SMS/WhatsApp → {phone}]
            קוד אימות YsMoment: {code}
            (תקף 5 דקות)
            """;
        _logger.LogInformation("{Message}", message);
        Console.WriteLine(message);

        return code;
    }

    public bool Verify(string eventSlug, string phone, string code)
    {
        var key = Key(eventSlug, phone);
        if (!_codes.TryGetValue(key, out var entry)) return false;
        if (DateTime.UtcNow > entry.Expires)
        {
            _codes.TryRemove(key, out _);
            return false;
        }
        if (entry.Code != code.Trim()) return false;
        _codes.TryRemove(key, out _);
        return true;
    }

    private static string Key(string slug, string phone)
        => $"{slug}:{new string(phone.Where(char.IsDigit).ToArray())}";
}
