using YsMoment.Core.Enums;

namespace YsMoment.Core.DTOs;

public record CreateEventRequest(
    string Name,
    string HostNames,
    EventType EventType,
    DateOnly Date,
    bool SizeSmallAvailable,
    bool SizeMediumAvailable,
    bool SizeLargeAvailable,
    int MaxCopies,
    int AveragePrepTimeMinutes,
    bool IsActive,
    bool OrdersOpen
);

public record UpdateEventSettingsRequest(
    bool? IsActive,
    bool? OrdersOpen,
    bool? OrdersPaused,
    int? AveragePrepTimeMinutes,
    bool? MuteCustomerNotifications
);

public record EventResponse(
    Guid Id,
    string Slug,
    string Name,
    string HostNames,
    EventType EventType,
    DateOnly Date,
    bool SizeSmallAvailable,
    bool SizeMediumAvailable,
    bool SizeLargeAvailable,
    int MaxCopies,
    int AveragePrepTimeMinutes,
    bool IsActive,
    bool OrdersOpen,
    bool OrdersPaused,
    bool IsEnded,
    bool MuteCustomerNotifications,
    string GuestUrl,
    string? QrCodeBase64
);

public record GuestEventResponse(
    string Name,
    string HostNames,
    EventType EventType,
    bool SizeSmallAvailable,
    bool SizeMediumAvailable,
    bool SizeLargeAvailable,
    int MaxCopies,
    bool OrdersOpen,
    bool OrdersPaused,
    bool IsEnded
);

public record EventSummaryResponse(
    int TotalOrders,
    int TotalMagnets,
    int PrintedCount,
    int CancelledCount,
    int PendingCount,
    double AverageWaitMinutes,
    List<PeakHourStat> PeakHours
);

public record PeakHourStat(string Hour, int OrderCount);

public record EventDashboardResponse(
    EventResponse Event,
    List<OrderResponse> Orders,
    DashboardStatsResponse Stats
);
