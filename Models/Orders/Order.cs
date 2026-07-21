using namera_API.Models.Common;
using namera_API.Models.Identity;

namespace namera_API.Models.Orders;

public sealed class Order : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "ILS";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhoneNumber { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? StockDeductedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? OwnerNote { get; set; }

    public ApplicationUser? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
