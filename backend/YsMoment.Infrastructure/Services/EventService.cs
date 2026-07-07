using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using YsMoment.Core.DTOs;
using YsMoment.Core.Entities;
using YsMoment.Core.Enums;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Data;

namespace YsMoment.Infrastructure.Services;

public class EventService
{
    private readonly AppDbContext _db;
    private readonly ISmsQueue _smsQueue;
    private readonly IImageStorageService _storage;
    private readonly string _guestBaseUrl;
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const int SlugLength = 10;

    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public EventService(AppDbContext db, ISmsQueue smsQueue, IImageStorageService storage, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _db = db;
        _smsQueue = smsQueue;
        _storage = storage;
        _guestBaseUrl = config["App:GuestBaseUrl"] ?? "http://localhost:4200/e";
    }

    public async Task<EventResponse> CreateAsync(CreateEventRequest request)
    {
        string slug;
        do { slug = GenerateSlug(); }
        while (await _db.Events.AnyAsync(e => e.Slug == slug));

        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = request.Name,
            HostNames = request.HostNames,
            EventType = request.EventType,
            Date = request.Date,
            SizeSmallAvailable = request.SizeSmallAvailable,
            SizeMediumAvailable = request.SizeMediumAvailable,
            SizeLargeAvailable = request.SizeLargeAvailable,
            MaxCopies = request.MaxCopies,
            AveragePrepTimeMinutes = request.AveragePrepTimeMinutes,
            IsActive = request.IsActive,
            OrdersOpen = request.OrdersOpen
        };

        _db.Events.Add(evt);
        await _db.SaveChangesAsync();

        return ToResponse(evt, includeQr: true);
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id)
        => await _db.Events.Where(e => e.Id == id).Select(e => e).FirstOrDefaultAsync() is { } evt
            ? ToResponse(evt, includeQr: true) : null;

    public async Task<EventResponse?> GetBySlugAdminAsync(string slug)
        => await _db.Events.FirstOrDefaultAsync(e => e.Slug == slug) is { } evt
            ? ToResponse(evt, includeQr: true) : null;

    public async Task<GuestEventResponse?> GetGuestEventAsync(string slug)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.Slug == slug);
        if (evt == null || !evt.IsActive) return null;

        return new GuestEventResponse(
            evt.Name, evt.HostNames, evt.EventType,
            evt.SizeSmallAvailable, evt.SizeMediumAvailable, evt.SizeLargeAvailable,
            evt.MaxCopies, evt.OrdersOpen && !evt.IsEnded, evt.OrdersPaused, evt.IsEnded);
    }

    public async Task<EventResponse?> UpdateSettingsAsync(Guid id, UpdateEventSettingsRequest request)
    {
        var evt = await _db.Events.FindAsync(id);
        if (evt == null) return null;

        if (request.IsActive.HasValue) evt.IsActive = request.IsActive.Value;
        if (request.OrdersOpen.HasValue) evt.OrdersOpen = request.OrdersOpen.Value;
        if (request.OrdersPaused.HasValue) evt.OrdersPaused = request.OrdersPaused.Value;
        if (request.AveragePrepTimeMinutes.HasValue) evt.AveragePrepTimeMinutes = request.AveragePrepTimeMinutes.Value;

        await _db.SaveChangesAsync();
        return ToResponse(evt, includeQr: false);
    }

    public async Task<EventSummaryResponse?> EndEventAsync(Guid id, string ratingUrl)
    {
        var evt = await _db.Events.Include(e => e.Orders).FirstOrDefaultAsync(e => e.Id == id);
        if (evt == null) return null;

        evt.IsEnded = true;
        evt.OrdersOpen = false;
        evt.EndedAt = DateTime.UtcNow;

        foreach (var order in evt.Orders.Where(o => !o.ImageDeleted && !string.IsNullOrEmpty(o.ImagePath)))
        {
            await _storage.DeleteAsync(order.ImagePath!);
            order.ImagePath = null;
            order.ImageDeleted = true;
        }

        var phones = evt.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => o.Phone)
            .Distinct();

        foreach (var phone in phones)
            _smsQueue.Enqueue(new EventThankYouSmsJob(null, phone, ratingUrl));

        await _db.SaveChangesAsync();
        return await GetSummaryAsync(id);
    }

    public async Task<EventSummaryResponse?> GetSummaryAsync(Guid id)
    {
        var evt = await _db.Events.Include(e => e.Orders).FirstOrDefaultAsync(e => e.Id == id);
        if (evt == null) return null;

        var orders = evt.Orders;
        var printed = orders.Count(o => o.Status == OrderStatus.Ready);
        var cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled);
        var pending = orders.Count(o => o.Status is OrderStatus.New or OrderStatus.InProgress);
        var totalMagnets = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Quantity);

        var peakHours = orders
            .GroupBy(o => o.CreatedAt.ToLocalTime().ToString("HH:00"))
            .Select(g => new PeakHourStat(g.Key, g.Count()))
            .OrderByDescending(p => p.OrderCount)
            .Take(5)
            .ToList();

        var avgWait = orders
            .Where(o => o.Status == OrderStatus.Ready)
            .Select(o => (o.UpdatedAt - o.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        return new EventSummaryResponse(
            orders.Count, totalMagnets, printed, cancelled, pending, avgWait, peakHours);
    }

    private EventResponse ToResponse(Event evt, bool includeQr)
    {
        var guestUrl = $"{_guestBaseUrl.TrimEnd('/')}/{evt.Slug}";
        return new EventResponse(
            evt.Id, evt.Slug, evt.Name, evt.HostNames, evt.EventType, evt.Date,
            evt.SizeSmallAvailable, evt.SizeMediumAvailable, evt.SizeLargeAvailable,
            evt.MaxCopies, evt.AveragePrepTimeMinutes,
            evt.IsActive, evt.OrdersOpen, evt.OrdersPaused, evt.IsEnded,
            guestUrl, includeQr ? null : null);
    }

    public static string GenerateSlug()
    {
        var bytes = new byte[SlugLength];
        Rng.GetBytes(bytes);

        var result = new char[SlugLength];

        for (int i = 0; i < SlugLength; i++)
        {
            result[i] = Chars[bytes[i] % Chars.Length];
        }

        return new string(result);
    }
}
