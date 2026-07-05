using YsMoment.Core.Enums;

namespace YsMoment.Core.DTOs;

public record PublicOrderView(
    string PublicToken,
    Guid EventId,
    string EventSlug,
    string EventName,
    string HostNames,
    EventType EventType,
    int OrderNumber,
    string CustomerName,
    MagnetSize MagnetSize,
    int Quantity,
    OrderStatus Status,
    DateTime CreatedAt,
    int? PositionInQueue,
    int? EstimatedWaitMinutes,
    bool CanEdit,
    bool CanCancel,
    int MinutesAgo,
    bool EventEnded,
    bool OrdersPaused
);

public record OrderTokenSummary(
    string PublicToken,
    int OrderNumber,
    OrderStatus Status,
    MagnetSize MagnetSize,
    int Quantity,
    DateTime CreatedAt,
    int MinutesAgo
);

public record ValidateTokensRequest(string[] Tokens);

public record SendRecoveryCodeRequest(string Phone);

public record RecoverOrdersRequest(string Phone, string Code);
