using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.Data;
using namera_API.DTOs.Orders;
using namera_API.Models.Identity;
using namera_API.Models.Orders;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;

namespace namera_API.Services.Orders;

public sealed class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderService(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(ClaimsPrincipal principal, CreateOrderRequestDto request)
    {
        var customer = await GetUserAsync(principal);
        if (customer is null)
        {
            return ApiResponse<OrderResponseDto>.Fail("تعذر العثور على حساب الزبون");
        }

        var normalizedItems = request.Items
            .Where(item => item.ProductId != Guid.Empty)
            .GroupBy(item => item.ProductId)
            .Select(group => new CreateOrderItemRequestDto
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (normalizedItems.Count == 0)
        {
            return ApiResponse<OrderResponseDto>.Fail("يجب إضافة منتج واحد على الأقل إلى الطلب");
        }

        if (normalizedItems.Any(item => item.Quantity < 1))
        {
            return ApiResponse<OrderResponseDto>.Fail("كمية كل منتج يجب أن تكون 1 على الأقل");
        }

        var productIds = normalizedItems.Select(item => item.ProductId).ToList();
        var products = await _dbContext.Products
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        var errors = ValidateOrderProducts(normalizedItems, products);
        if (errors.Count > 0)
        {
            return ApiResponse<OrderResponseDto>.Fail("راجع المنتجات قبل إرسال الطلب", errors);
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            CustomerPhoneNumber = customer.PhoneNumber ?? string.Empty,
            ShippingAddress = string.IsNullOrWhiteSpace(request.ShippingAddress) ? customer.Address : request.ShippingAddress.Trim(),
            Notes = NormalizeNullableText(request.Notes),
            Status = OrderStatus.Pending,
            Currency = "ILS",
            CreatedAt = DateTime.UtcNow
        };

        order.OrderNumber = await CreateOrderNumberAsync(order.CreatedAt);

        foreach (var item in normalizedItems)
        {
            var product = products.Single(product => product.Id == item.ProductId);
            var primaryImage = product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .FirstOrDefault();
            var unitPrice = product.PricingType == ProductPricingType.Quote ? 0 : product.BasePrice;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                CategoryName = product.Category.Name,
                ImageUrl = primaryImage?.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * item.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        }

        order.Subtotal = order.Items.Sum(item => item.LineTotal);
        order.Total = order.Subtotal;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var created = await GetOrderForDetailsAsync(order.Id);
        return ApiResponse<OrderResponseDto>.Ok(ToOrderDto(created!), "تم إرسال الطلب بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> GetCustomerOrdersAsync(ClaimsPrincipal principal)
    {
        var customer = await GetUserAsync(principal);
        if (customer is null)
        {
            return ApiResponse<IReadOnlyList<OrderResponseDto>>.Fail("تعذر العثور على حساب الزبون");
        }

        var orders = await BaseOrdersQuery()
            .Where(order => order.CustomerId == customer.Id)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<OrderResponseDto>>.Ok(orders.Select(ToOrderDto).ToList(), "تم تحميل طلباتك بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> GetOwnerOrdersAsync(OrderListQueryDto query)
    {
        var ordersQuery = BaseOrdersQuery();

        if (!string.IsNullOrWhiteSpace(query.Status) && TryParseStatus(query.Status, out var status))
        {
            ordersQuery = ordersQuery.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            ordersQuery = ordersQuery.Where(order =>
                order.OrderNumber.Contains(search) ||
                order.CustomerName.Contains(search) ||
                order.CustomerPhoneNumber.Contains(search));
        }

        var orders = await ordersQuery
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<OrderResponseDto>>.Ok(orders.Select(ToOrderDto).ToList(), "تم تحميل الطلبات بنجاح");
    }

    public async Task<ApiResponse<OrderResponseDto>> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequestDto request)
    {
        var order = await _dbContext.Orders
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (order is null)
        {
            return ApiResponse<OrderResponseDto>.Fail("الطلب غير موجود");
        }

        if (!TryParseStatus(request.Status, out var nextStatus))
        {
            return ApiResponse<OrderResponseDto>.Fail("حالة الطلب غير معروفة");
        }

        if (ShouldDeductStock(nextStatus) && order.StockDeductedAt is null)
        {
            var deductionErrors = await DeductStockAsync(order);
            if (deductionErrors.Count > 0)
            {
                return ApiResponse<OrderResponseDto>.Fail("لا يمكن اعتماد الطلب قبل توفر الكمية", deductionErrors);
            }
        }

        if (ShouldRestoreStock(nextStatus) && order.StockDeductedAt is not null)
        {
            await RestoreStockAsync(order);
        }

        order.Status = nextStatus;
        order.OwnerNote = NormalizeNullableText(request.OwnerNote);
        order.UpdatedAt = DateTime.UtcNow;

        if (nextStatus == OrderStatus.Approved && order.ApprovedAt is null)
        {
            order.ApprovedAt = DateTime.UtcNow;
        }

        if (nextStatus == OrderStatus.Received && order.ReceivedAt is null)
        {
            order.ReceivedAt = DateTime.UtcNow;
        }

        if (nextStatus is OrderStatus.Cancelled or OrderStatus.Rejected && order.CancelledAt is null)
        {
            order.CancelledAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        var updated = await GetOrderForDetailsAsync(order.Id);
        return ApiResponse<OrderResponseDto>.Ok(ToOrderDto(updated!), "تم تحديث حالة الطلب بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<OwnerCustomerResponseDto>>> GetCustomersAsync()
    {
        var customers = await _userManager.GetUsersInRoleAsync(AppRoles.Customer);
        var customerIds = customers.Select(customer => customer.Id).ToList();
        var orderStats = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => customerIds.Contains(order.CustomerId))
            .GroupBy(order => order.CustomerId)
            .Select(group => new
            {
                CustomerId = group.Key,
                OrdersCount = group.Count(),
                TotalSpent = group
                    .Where(order => order.Status == OrderStatus.Completed || order.Status == OrderStatus.Received)
                    .Sum(order => order.Total)
            })
            .ToDictionaryAsync(item => item.CustomerId);

        var result = customers
            .OrderBy(customer => customer.FirstName)
            .ThenBy(customer => customer.LastName)
            .Select(customer =>
            {
                orderStats.TryGetValue(customer.Id, out var stats);

                return new OwnerCustomerResponseDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    FullName = $"{customer.FirstName} {customer.LastName}".Trim(),
                    UserName = customer.UserName ?? string.Empty,
                    Email = customer.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    Address = customer.Address,
                    IsActive = customer.IsActive,
                    OrdersCount = stats?.OrdersCount ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0,
                    CreatedAt = customer.CreatedAt,
                    UpdatedAt = customer.UpdatedAt
                };
            })
            .ToList();

        return ApiResponse<IReadOnlyList<OwnerCustomerResponseDto>>.Ok(result, "تم تحميل العملاء بنجاح");
    }

    public async Task<ApiResponse<OwnerDashboardStatsDto>> GetDashboardStatsAsync()
    {
        var orders = await _dbContext.Orders.AsNoTracking().ToListAsync();
        var customersCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Customer)).Count;
        var productsCount = await _dbContext.Products.AsNoTracking().CountAsync();
        var statusCounts = orders
            .GroupBy(order => order.Status)
            .Select(group => new StatusCountDto
            {
                Status = ToStatusKey(group.Key),
                Label = ToStatusLabel(group.Key),
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToList();

        var revenueByDay = orders
            .Where(order => order.Status is OrderStatus.Received or OrderStatus.Completed)
            .GroupBy(order => order.CreatedAt.Date)
            .OrderBy(group => group.Key)
            .TakeLast(7)
            .Select(group => new RevenuePointDto
            {
                Date = group.Key.ToString("yyyy-MM-dd"),
                Revenue = group.Sum(order => order.Total)
            })
            .ToList();

        var stats = new OwnerDashboardStatsDto
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(order => order.Status == OrderStatus.Pending),
            ApprovedOrders = orders.Count(order => order.Status is OrderStatus.Approved or OrderStatus.Received or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Shipped),
            CompletedOrders = orders.Count(order => order.Status == OrderStatus.Completed),
            CustomersCount = customersCount,
            ProductsCount = productsCount,
            Revenue = orders.Where(order => order.Status is OrderStatus.Received or OrderStatus.Completed).Sum(order => order.Total),
            OrdersByStatus = statusCounts,
            RevenueByDay = revenueByDay
        };

        return ApiResponse<OwnerDashboardStatsDto>.Ok(stats, "تم تحميل الإحصائيات بنجاح");
    }

    private IQueryable<Order> BaseOrdersQuery()
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items);
    }

    private async Task<Order?> GetOrderForDetailsAsync(Guid id)
    {
        return await BaseOrdersQuery().FirstOrDefaultAsync(order => order.Id == id);
    }

    private async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
            ? await _userManager.FindByIdAsync(userId.ToString())
            : null;
    }

    private static List<string> ValidateOrderProducts(IReadOnlyList<CreateOrderItemRequestDto> requestItems, IReadOnlyList<Product> products)
    {
        var errors = new List<string>();

        foreach (var item in requestItems)
        {
            var product = products.FirstOrDefault(product => product.Id == item.ProductId);
            if (product is null)
            {
                errors.Add("أحد المنتجات غير موجود.");
                continue;
            }

            if (!product.Category.IsActive)
            {
                errors.Add($"تصنيف المنتج {product.Name} معطل حاليا.");
            }

            if (product.Status is not (ProductStatus.Active or ProductStatus.Published))
            {
                errors.Add($"المنتج {product.Name} غير متاح للطلب حاليا.");
            }

            if (!product.AllowOrdering)
            {
                errors.Add($"المنتج {product.Name} لا يقبل الطلبات حاليا.");
            }

            if (product.DirectAccessOnly)
            {
                errors.Add($"المنتج {product.Name} غير ظاهر للطلبات العامة.");
            }

            if (product.InventoryTrackingEnabled && !product.MadeToOrder && product.Quantity.HasValue && product.Quantity.Value < item.Quantity)
            {
                errors.Add($"الكمية المتوفرة من {product.Name} لا تكفي الطلب.");
            }
        }

        return errors;
    }

    private async Task<List<string>> DeductStockAsync(Order order)
    {
        var errors = new List<string>();
        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        foreach (var itemGroup in order.Items.GroupBy(item => item.ProductId))
        {
            var product = products.Single(product => product.Id == itemGroup.Key);
            if (!product.InventoryTrackingEnabled || product.MadeToOrder)
            {
                continue;
            }

            var requiredQuantity = itemGroup.Sum(item => item.Quantity);
            if ((product.Quantity ?? 0) < requiredQuantity)
            {
                errors.Add($"الكمية المتوفرة من {product.Name} لا تكفي لاعتماد الطلب.");
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (var itemGroup in order.Items.GroupBy(item => item.ProductId))
        {
            var product = products.Single(product => product.Id == itemGroup.Key);
            if (!product.InventoryTrackingEnabled || product.MadeToOrder)
            {
                continue;
            }

            product.Quantity = (product.Quantity ?? 0) - itemGroup.Sum(item => item.Quantity);
            product.UpdatedAt = DateTime.UtcNow;
        }

        order.StockDeductedAt = DateTime.UtcNow;
        return errors;
    }

    private async Task RestoreStockAsync(Order order)
    {
        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        foreach (var itemGroup in order.Items.GroupBy(item => item.ProductId))
        {
            var product = products.Single(product => product.Id == itemGroup.Key);
            if (!product.InventoryTrackingEnabled || product.MadeToOrder)
            {
                continue;
            }

            product.Quantity = (product.Quantity ?? 0) + itemGroup.Sum(item => item.Quantity);
            product.UpdatedAt = DateTime.UtcNow;
        }

        order.StockDeductedAt = null;
    }

    private async Task<string> CreateOrderNumberAsync(DateTime createdAt)
    {
        var datePart = createdAt.ToString("yyyyMMdd");
        var count = await _dbContext.Orders.CountAsync(order => order.CreatedAt.Date == createdAt.Date) + 1;
        var orderNumber = $"RB-{datePart}-{count:0000}";

        while (await _dbContext.Orders.AnyAsync(order => order.OrderNumber == orderNumber))
        {
            count += 1;
            orderNumber = $"RB-{datePart}-{count:0000}";
        }

        return orderNumber;
    }

    private static bool ShouldDeductStock(OrderStatus status)
    {
        return status is OrderStatus.Approved or OrderStatus.Received or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Shipped or OrderStatus.Completed;
    }

    private static bool ShouldRestoreStock(OrderStatus status)
    {
        return status is OrderStatus.Cancelled or OrderStatus.Rejected;
    }

    private static bool TryParseStatus(string value, out OrderStatus status)
    {
        status = value.Trim().ToLowerInvariant() switch
        {
            "pending" => OrderStatus.Pending,
            "approved" or "accepted" => OrderStatus.Approved,
            "received" => OrderStatus.Received,
            "preparing" => OrderStatus.Preparing,
            "ready" => OrderStatus.Ready,
            "shipped" => OrderStatus.Shipped,
            "completed" => OrderStatus.Completed,
            "cancelled" or "canceled" => OrderStatus.Cancelled,
            "rejected" => OrderStatus.Rejected,
            _ => OrderStatus.Pending
        };

        return value.Trim().Length > 0 && (Enum.TryParse<OrderStatus>(value, true, out _) || KnownStatusKeys.Contains(value.Trim().ToLowerInvariant()));
    }

    private static readonly HashSet<string> KnownStatusKeys =
    [
        "pending",
        "approved",
        "accepted",
        "received",
        "preparing",
        "ready",
        "shipped",
        "completed",
        "cancelled",
        "canceled",
        "rejected"
    ];

    private static OrderResponseDto ToOrderDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = ToStatusKey(order.Status),
            StatusLabel = ToStatusLabel(order.Status),
            Subtotal = order.Subtotal,
            Total = order.Total,
            Currency = order.Currency,
            CustomerName = order.CustomerName,
            CustomerPhoneNumber = order.CustomerPhoneNumber,
            ShippingAddress = order.ShippingAddress,
            Notes = order.Notes ?? string.Empty,
            OwnerNote = order.OwnerNote ?? string.Empty,
            StockDeducted = order.StockDeductedAt.HasValue,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ApprovedAt = order.ApprovedAt,
            ReceivedAt = order.ReceivedAt,
            CancelledAt = order.CancelledAt,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSlug = item.ProductSlug,
                CategoryName = item.CategoryName,
                ImageUrl = item.ImageUrl ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList()
        };
    }

    private static string ToStatusKey(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Approved => "approved",
            OrderStatus.Received => "received",
            OrderStatus.Preparing => "preparing",
            OrderStatus.Ready => "ready",
            OrderStatus.Shipped => "shipped",
            OrderStatus.Completed => "completed",
            OrderStatus.Cancelled => "cancelled",
            OrderStatus.Rejected => "rejected",
            _ => "pending"
        };
    }

    private static string ToStatusLabel(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Approved => "مقبول",
            OrderStatus.Received => "تم استلامه",
            OrderStatus.Preparing => "قيد التجهيز",
            OrderStatus.Ready => "جاهز",
            OrderStatus.Shipped => "تم الشحن",
            OrderStatus.Completed => "مكتمل",
            OrderStatus.Cancelled => "ملغي",
            OrderStatus.Rejected => "مرفوض",
            _ => "جديد"
        };
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
