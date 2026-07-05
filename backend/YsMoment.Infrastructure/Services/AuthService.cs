using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using YsMoment.Core.DTOs;
using YsMoment.Core.Entities;
using YsMoment.Infrastructure.Data;

namespace YsMoment.Infrastructure.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task EnsureDefaultAdminAsync()
    {
        if (await _db.Admins.AnyAsync()) return;

        var username = _config["Admin:Username"]
            ?? throw new InvalidOperationException("Admin:Username is required.");
        var password = _config["Admin:Password"]
            ?? throw new InvalidOperationException("Admin:Password is required.");

        _db.Admins.Add(new Admin
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = HashPassword(password)
        });
        await _db.SaveChangesAsync();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username == request.Username);
        if (admin == null || !VerifyPassword(request.Password, admin.PasswordHash))
            return null;

        // Upgrade legacy SHA-256 hash to PBKDF2 on successful login
        if (!admin.PasswordHash.StartsWith("v2:"))
        {
            admin.PasswordHash = HashPassword(request.Password);
            await _db.SaveChangesAsync();
        }

        var expires = DateTime.UtcNow.AddHours(12);
        var token = GenerateToken(admin, expires);
        return new LoginResponse(token, expires);
    }

    private string GenerateToken(Admin admin, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: [new Claim(ClaimTypes.Name, admin.Username), new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString())],
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"v2:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (storedHash.StartsWith("v2:"))
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 3) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        // Legacy SHA-256 (no salt) — upgrade on next login in LoginAsync
        var legacyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(legacyBytes) == storedHash;
    }
}
