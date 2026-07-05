using YsMoment.Core.Enums;

namespace YsMoment.Core.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HostNames { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly Date { get; set; }

    public bool SizeSmallAvailable { get; set; } = true;
    public bool SizeMediumAvailable { get; set; } = true;
    public bool SizeLargeAvailable { get; set; } = true;

    public int MaxCopies { get; set; } = 6;
    public int AveragePrepTimeMinutes { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public bool OrdersOpen { get; set; } = true;
    public bool OrdersPaused { get; set; }
    public bool IsEnded { get; set; }

    public int NextOrderNumber { get; set; } = 1000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
