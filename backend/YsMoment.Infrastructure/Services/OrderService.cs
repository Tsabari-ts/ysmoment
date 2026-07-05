using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using YsMoment.Core.DTOs;
using YsMoment.Core.Entities;
using YsMoment.Core.Enums;
using YsMoment.Core.Interfaces;
using YsMoment.Infrastructure.Data;

namespace YsMoment.Infrastructure.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly IImageStorageService _storage;
    private readonly IWhatsAppService _whatsApp;
    private readonly VerificationCodeService _verification;

    public OrderService(AppDbContext db, IImageStorageService storage, IWhatsAppService whatsApp, VerificationCodeService verification)
    {
        _db = db;
        _storage = storage;
        _whatsApp = whatsApp;
        _verification = verification;
    }

    public async Task<List<OrderResponse>> GetQueueAsync(Guid eventId)
    {
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return [];

        var orders = await _db.Orders
            .Where(o => o.EventId == eventId && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select((o, i) => MapOrder(o, evt, GetQueuePosition(orders, o))).ToList();
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync(Guid eventId)
    {
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return [];

        var orders = await _db.Orders
            .Where(o => o.EventId == eventId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var queue = orders
            .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress)
            .ToList();

        return orders.Select(o => MapOrder(o, evt, GetQueuePosition(queue, o))).ToList();
    }

    public async Task<List<OrderResponse>> SearchAsync(Guid eventId, string query)
    {
        query = query.Trim().ToLower();
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return [];

        var orders = await _db.Orders
            .Where(o => o.EventId == eventId &&
                (o.CustomerName.ToLower().Contains(query) ||
                 o.Phone.Contains(query) ||
                 o.OrderNumber.ToString().Contains(query)))
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        var queue = await _db.Orders
            .Where(o => o.EventId == eventId && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => MapOrder(o, evt, GetQueuePosition(queue, o))).ToList();
    }

    public async Task<DashboardStatsResponse> GetStatsAsync(Guid eventId)
    {
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return new DashboardStatsResponse(0, 0, 0, 0, "ריק");

        var orders = await _db.Orders.Where(o => o.EventId == eventId).ToListAsync();
        var printed = orders.Count(o => o.Status == OrderStatus.Ready);
        var pending = orders.Count(o => o.Status is OrderStatus.New or OrderStatus.InProgress);
        var cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled);

        var avgWait = orders
            .Where(o => o.Status == OrderStatus.Ready)
            .Select(o => (o.UpdatedAt - o.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var load = pending switch
        {
            0 => "ריק",
            <= 3 => "קל",
            <= 8 => "בינוני",
            _ => "עמוס"
        };

        return new DashboardStatsResponse(printed, pending, cancelled, avgWait, load);
    }

    public async Task<PublicOrderView?> CreateAsync(string slug, CreateOrderRequest request, Stream imageStream, string fileName)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.Slug == slug);
        if (evt == null) return null;
        if (!evt.IsActive || evt.IsEnded || !evt.OrdersOpen)
            throw new InvalidOperationException("האירוע אינו מקבל הזמנות כרגע.");
        if (evt.OrdersPaused)
            throw new InvalidOperationException("יש עומס זמני במערכת. נסו שוב בעוד מספר דקות.");
        if (!request.PrivacyAccepted)
            throw new InvalidOperationException("יש לאשר את מדיניות הפרטיות.");
        if (request.Quantity < 1 || request.Quantity > evt.MaxCopies)
            throw new InvalidOperationException($"כמות חייבת להיות בין 1 ל-{evt.MaxCopies}.");
        if (!IsSizeAvailable(evt, request.MagnetSize))
            throw new InvalidOperationException("גודל מגנט זה אינו זמין באירוע.");

        var orderId = Guid.NewGuid();
        var imagePath = await _storage.SaveAsync(imageStream, fileName, evt.Id, orderId);

        Order order;
        // Retry loop guards against concurrent requests colliding on NextOrderNumber
        while (true)
        {
            await _db.Entry(evt).ReloadAsync();
            var orderNumber = evt.NextOrderNumber++;
            order = new Order
            {
                Id = orderId,
                EventId = evt.Id,
                PublicToken = GeneratePublicToken(),
                OrderNumber = orderNumber,
                CustomerName = request.CustomerName.Trim(),
                Phone = NormalizePhone(request.Phone),
                ImagePath = imagePath,
                MagnetSize = request.MagnetSize,
                Quantity = request.Quantity,
                Status = OrderStatus.New
            };
            _db.Orders.Add(order);
            try
            {
                await _db.SaveChangesAsync();
                break;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("IX_Orders_EventId_OrderNumber") == true
                   || ex.InnerException?.Message.Contains("unique") == true)
            {
                _db.Entry(order).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                _db.Entry(evt).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                evt = await _db.Events.FirstOrDefaultAsync(e => e.Slug == slug) ?? evt;
            }
        }

        var queue = await GetActiveQueue(evt.Id);
        var position = queue.Count;
        var estimated = position * evt.AveragePrepTimeMinutes;

        await _whatsApp.SendOrderConfirmationAsync(order.Phone, order.CustomerName, order.OrderNumber, position, estimated);

        return MapPublicOrder(order, evt, position);
    }

    public async Task<PublicOrderView?> GetByPublicTokenAsync(string token)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.PublicToken == token);
        if (order == null) return null;

        var queue = await GetActiveQueue(order.EventId);
        var position = order.Status is OrderStatus.New or OrderStatus.InProgress
            ? GetQueuePosition(queue, order) : 0;

        return MapPublicOrder(order, order.Event, position);
    }

    public async Task<List<OrderTokenSummary>> ValidateTokensAsync(string slug, IEnumerable<string> tokens)
    {
        var tokenList = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (tokenList.Count == 0) return [];

        var orders = await _db.Orders
            .Include(o => o.Event)
            .Where(o => tokenList.Contains(o.PublicToken) && o.Event.Slug == slug)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapTokenSummary).ToList();
    }

    public Task SendRecoveryCodeAsync(string slug, string phone)
    {
        _verification.GenerateAndStore(slug, NormalizePhone(phone));
        return Task.CompletedTask;
    }

    public async Task<List<OrderTokenSummary>> RecoverOrdersAsync(string slug, string phone, string code)
    {
        var normalized = NormalizePhone(phone);
        if (!_verification.Verify(slug, normalized, code))
            throw new InvalidOperationException("קוד אימות שגוי או שפג תוקפו.");

        var orders = await _db.Orders
            .Include(o => o.Event)
            .Where(o => o.Event.Slug == slug && o.Phone == normalized && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapTokenSummary).ToList();
    }

    public async Task<PublicOrderView?> UpdateByTokenAsync(string token, UpdateOrderRequest request)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.PublicToken == token);
        if (order == null) return null;
        return await UpdateOrderInternal(order, request);
    }

    public async Task<PublicOrderView?> CancelByTokenAsync(string token)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.PublicToken == token);
        if (order == null) return null;
        if (!await CancelInternal(order)) return null;

        return MapPublicOrder(order, order.Event, 0);
    }

    public async Task<OrderResponse?> UpdateAsync(Guid orderId, UpdateOrderRequest request)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return null;
        var result = await UpdateOrderInternal(order, request);
        if (result == null) return null;
        var queue = await GetActiveQueue(order.EventId);
        return MapOrder(order, order.Event, GetQueuePosition(queue, order));
    }

    private async Task<PublicOrderView?> UpdateOrderInternal(Order order, UpdateOrderRequest request)
    {
        if (order.Status != OrderStatus.New)
            throw new InvalidOperationException("ההזמנה כבר בטיפול ולכן לא ניתן לערוך.");

        if (request.CustomerName != null) order.CustomerName = request.CustomerName.Trim();
        if (request.Phone != null) order.Phone = NormalizePhone(request.Phone);
        if (request.MagnetSize.HasValue)
        {
            if (!IsSizeAvailable(order.Event, request.MagnetSize.Value))
                throw new InvalidOperationException("גודל מגנט זה אינו זמין.");
            order.MagnetSize = request.MagnetSize.Value;
        }
        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value < 1 || request.Quantity.Value > order.Event.MaxCopies)
                throw new InvalidOperationException($"כמות חייבת להיות בין 1 ל-{order.Event.MaxCopies}.");
            order.Quantity = request.Quantity.Value;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var queue = await GetActiveQueue(order.EventId);
        return MapPublicOrder(order, order.Event, GetQueuePosition(queue, order));
    }

    public async Task<OrderResponse?> UpdateStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Ready && !order.ImageDeleted && !string.IsNullOrEmpty(order.ImagePath))
        {
            await _storage.DeleteAsync(order.ImagePath);
            order.ImagePath = null;
            order.ImageDeleted = true;
            await _whatsApp.SendOrderReadyAsync(order.Phone, order.CustomerName);
        }

        await _db.SaveChangesAsync();

        var queue = await GetActiveQueue(order.EventId);
        return MapOrder(order, order.Event, GetQueuePosition(queue, order));
    }

    public async Task<bool> CancelAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return false;
        return await CancelInternal(order);
    }

    private async Task<bool> CancelInternal(Order order)
    {
        if (order.Status != OrderStatus.New) return false;

        if (!order.ImageDeleted && !string.IsNullOrEmpty(order.ImagePath))
        {
            await _storage.DeleteAsync(order.ImagePath);
            order.ImagePath = null;
            order.ImageDeleted = true;
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(string FullPath, string FileName, string ContentType)?> GetImageFileAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null || order.ImageDeleted || string.IsNullOrEmpty(order.ImagePath))
            return null;

        var fullPath = _storage.GetPhysicalPath(order.ImagePath);
        if (fullPath == null) return null;

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".heic" => "image/heic",
            _ => "image/jpeg"
        };

        return (fullPath, $"order-{order.OrderNumber}{ext}", contentType);
    }

    // Returns public URL for cloud-hosted images when GetImageFileAsync returns null
    public async Task<string?> GetImageRedirectUrlAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null || order.ImageDeleted || string.IsNullOrEmpty(order.ImagePath))
            return null;
        return _storage.GetPublicUrl(order.ImagePath);
    }

    public async Task<OrderStatusResponse?> GetStatusAsync(Guid orderId)
    {
        var order = await _db.Orders.Include(o => o.Event).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return null;

        var queue = await GetActiveQueue(order.EventId);
        var position = order.Status is OrderStatus.New or OrderStatus.InProgress
            ? GetQueuePosition(queue, order) : (int?)null;
        var estimated = position.HasValue ? position.Value * order.Event.AveragePrepTimeMinutes : (int?)null;

        return new OrderStatusResponse(order.Id, order.EventId, order.OrderNumber, order.CustomerName, order.Status, position, estimated);
    }

    private async Task<List<Order>> GetActiveQueue(Guid eventId)
        => await _db.Orders
            .Where(o => o.EventId == eventId && (o.Status == OrderStatus.New || o.Status == OrderStatus.InProgress))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

    private static int GetQueuePosition(List<Order> queue, Order order)
    {
        if (order.Status is OrderStatus.Ready or OrderStatus.Cancelled) return 0;
        var idx = queue.FindIndex(o => o.Id == order.Id);
        return idx >= 0 ? idx + 1 : 0;
    }

    private OrderResponse MapOrder(Order order, Event evt, int position)
    {
        var estimated = position > 0 ? position * evt.AveragePrepTimeMinutes : (int?)null;
        var wait = (int)(DateTime.UtcNow - order.CreatedAt).TotalMinutes;

        return new OrderResponse(
            order.Id, order.EventId, order.OrderNumber, order.CustomerName, order.Phone,
            _storage.GetPublicUrl(order.ImagePath),
            order.MagnetSize, order.Quantity, order.Status,
            order.CreatedAt, position > 0 ? position : null, estimated, wait);
    }

    private static bool IsSizeAvailable(Event evt, MagnetSize size) => size switch
    {
        MagnetSize.Small => evt.SizeSmallAvailable,
        MagnetSize.Medium => evt.SizeMediumAvailable,
        MagnetSize.Large => evt.SizeLargeAvailable,
        _ => false
    };

    private static string NormalizePhone(string phone)
        => new string(phone.Where(char.IsDigit).ToArray());

    private static string GeneratePublicToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private PublicOrderView MapPublicOrder(Order order, Event evt, int position)
    {
        var estimated = position > 0 ? position * evt.AveragePrepTimeMinutes : (int?)null;
        var minutesAgo = (int)(DateTime.UtcNow - order.CreatedAt).TotalMinutes;

        return new PublicOrderView(
            order.PublicToken,
            evt.Id,
            evt.Slug,
            evt.Name,
            evt.HostNames,
            evt.EventType,
            order.OrderNumber,
            order.CustomerName,
            order.MagnetSize,
            order.Quantity,
            order.Status,
            order.CreatedAt,
            position > 0 ? position : null,
            estimated,
            order.Status == OrderStatus.New,
            order.Status == OrderStatus.New,
            minutesAgo,
            evt.IsEnded,
            evt.OrdersPaused);
    }

    private static OrderTokenSummary MapTokenSummary(Order order)
    {
        var minutesAgo = (int)(DateTime.UtcNow - order.CreatedAt).TotalMinutes;
        return new OrderTokenSummary(
            order.PublicToken,
            order.OrderNumber,
            order.Status,
            order.MagnetSize,
            order.Quantity,
            order.CreatedAt,
            minutesAgo);
    }

    public static async Task BackfillPublicTokensAsync(AppDbContext db)
    {
        var orders = await db.Orders.Where(o => o.PublicToken == "" || o.PublicToken == null).ToListAsync();
        foreach (var order in orders)
            order.PublicToken = GeneratePublicToken();
        if (orders.Count > 0)
            await db.SaveChangesAsync();
    }
}
