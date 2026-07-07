using YsMoment.Core.Enums;

namespace YsMoment.Core.DTOs;

public record CreateOrderRequest(
    string CustomerName,
    string Phone,
    MagnetSize MagnetSize,
    int Quantity,
    bool PrivacyAccepted
);

public record UpdateOrderRequest(
    string? CustomerName,
    string? Phone,
    MagnetSize? MagnetSize,
    int? Quantity
);

public record OrderResponse(
    Guid Id,
    Guid EventId,
    int OrderNumber,
    string CustomerName,
    string Phone,
    string? ImageUrl,
    MagnetSize MagnetSize,
    int Quantity,
    OrderStatus Status,
    DateTime CreatedAt,
    int? PositionInQueue,
    int? EstimatedWaitMinutes,
    int WaitMinutes,
    bool NotificationFailed
);

public record OrderStatusResponse(
    Guid Id,
    Guid EventId,
    int OrderNumber,
    string CustomerName,
    OrderStatus Status,
    int? PositionInQueue,
    int? EstimatedWaitMinutes
);

public record DashboardStatsResponse(
    int PrintedCount,
    int PendingCount,
    int CancelledCount,
    double AverageWaitMinutes,
    string CurrentLoad
);

public record QueueUpdatePayload(
    List<OrderResponse> Orders,
    DashboardStatsResponse Stats
);
