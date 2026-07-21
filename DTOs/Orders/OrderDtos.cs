namespace namera_API.DTOs.Orders;

public sealed class CreateOrderRequestDto
{
    public IReadOnlyList<CreateOrderItemRequestDto> Items { get; init; } = [];
    public string? ShippingAddress { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreateOrderItemRequestDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; } = 1;
    public IReadOnlyList<CreateOrderSelectedOptionDto> SelectedOptions { get; init; } = [];
    public IReadOnlyList<CreateOrderCustomFieldDto> CustomFields { get; init; } = [];
    public string? CustomRequest { get; init; }
}

public sealed class CreateOrderSelectedOptionDto
{
    public Guid GroupId { get; init; }
    public Guid ValueId { get; init; }
}

public sealed class CreateOrderCustomFieldDto
{
    public Guid FieldId { get; init; }
    public string? Value { get; init; }
    public IReadOnlyList<Guid> SelectedChoiceIds { get; init; } = [];
}

public sealed class UpdateOrderStatusRequestDto
{
    public string Status { get; init; } = "pending";
    public string? OwnerNote { get; init; }
}

public sealed class OrderListQueryDto
{
    public string? Status { get; init; }
    public string? Search { get; init; }
}

public sealed class OrderResponseDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = "ILS";
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhoneNumber { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string OwnerNote { get; init; } = string.Empty;
    public bool StockDeducted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? ReceivedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public IReadOnlyList<OrderItemResponseDto> Items { get; init; } = [];
}

public sealed class OrderItemResponseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string CustomizationSummary { get; init; } = string.Empty;
    public string CustomizationDetailsJson { get; init; } = string.Empty;
}

public sealed class OwnerCustomerResponseDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int OrdersCount { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class OwnerDashboardStatsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int ApprovedOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int CustomersCount { get; init; }
    public int ProductsCount { get; init; }
    public decimal Revenue { get; init; }
    public IReadOnlyList<StatusCountDto> OrdersByStatus { get; init; } = [];
    public IReadOnlyList<RevenuePointDto> RevenueByDay { get; init; } = [];
}

public sealed class StatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class RevenuePointDto
{
    public string Date { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
}
