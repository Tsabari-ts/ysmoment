using YsMoment.Core.Enums;

namespace YsMoment.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string PublicToken { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool ImageDeleted { get; set; }
    public MagnetSize MagnetSize { get; set; }
    public int Quantity { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
}
