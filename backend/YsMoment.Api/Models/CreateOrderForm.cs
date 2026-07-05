using YsMoment.Core.Enums;

namespace YsMoment.Api.Models;

public class CreateOrderForm
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public MagnetSize MagnetSize { get; set; }
    public int Quantity { get; set; }
    public bool PrivacyAccepted { get; set; }
}
