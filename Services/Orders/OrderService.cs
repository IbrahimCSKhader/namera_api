using System.Security.Claims;
using System.Text.Json;
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
            .Select(item => new CreateOrderItemRequestDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                SelectedOptions = item.SelectedOptions,
                CustomFields = item.CustomFields,
                CustomRequest = item.CustomRequest
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
            .Include(product => product.OptionGroups)
                .ThenInclude(group => group.Values)
            .Include(product => product.CustomizationFields)
                .ThenInclude(field => field.Choices)
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        var errors = ValidateOrderProducts(normalizedItems, products);
        if (errors.Count > 0)
        {
            return ApiResponse<OrderResponseDto>.Fail("راجع المنتجات قبل إرسال الطلب", errors);
        }

        var customizationSnapshots = normalizedItems
            .Select(item => BuildCustomizationSnapshot(item, products.Single(product => product.Id == item.ProductId)))
            .ToList();
        var customizationErrors = customizationSnapshots.SelectMany(snapshot => snapshot.Errors).ToList();
        if (customizationErrors.Count > 0)
        {
            return ApiResponse<OrderResponseDto>.Fail("راجع تخصيصات المنتجات قبل إرسال الطلب", customizationErrors);
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

        for (var index = 0; index < normalizedItems.Count; index += 1)
        {
            var item = normalizedItems[index];
            var customization = customizationSnapshots[index];
            var product = products.Single(product => product.Id == item.ProductId);
            var primaryImage = product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .FirstOrDefault();
            var unitPrice = (product.PricingType == ProductPricingType.Quote ? 0 : product.BasePrice) + customization.ExtraPrice;

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
                CustomizationSummary = customization.Summary,
                CustomizationDetailsJson = customization.DetailsJson,
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

            if (item.Quantity < product.MinimumQuantity)
            {
                errors.Add($"أقل كمية من {product.Name} هي {product.MinimumQuantity}.");
            }

            if (product.MaximumQuantity.HasValue && item.Quantity > product.MaximumQuantity.Value)
            {
                errors.Add($"أعلى كمية من {product.Name} هي {product.MaximumQuantity.Value}.");
            }

            if (product.DirectAccessOnly)
            {
                errors.Add($"المنتج {product.Name} غير ظاهر للطلبات العامة.");
            }

            var totalRequestedQuantity = requestItems
                .Where(requestItem => requestItem.ProductId == product.Id)
                .Sum(requestItem => requestItem.Quantity);
            var isFirstRequestForProduct = requestItems.TakeWhile(requestItem => requestItem != item).All(requestItem => requestItem.ProductId != product.Id);

            if (isFirstRequestForProduct && product.InventoryTrackingEnabled && !product.MadeToOrder && product.Quantity.HasValue && product.Quantity.Value < totalRequestedQuantity)
            {
                errors.Add($"الكمية المتوفرة من {product.Name} لا تكفي الطلب.");
            }
        }

        return errors;
    }

    private static OrderItemCustomizationSnapshot BuildCustomizationSnapshot(CreateOrderItemRequestDto requestItem, Product product)
    {
        var errors = new List<string>();
        var summaryParts = new List<string>();
        var detailItems = new List<OrderItemCustomizationDetail>();
        var extraPrice = 0m;
        var selectedOptions = requestItem.SelectedOptions
            .Where(option => option.GroupId != Guid.Empty && option.ValueId != Guid.Empty)
            .ToList();
        var selectedOptionGroupIds = selectedOptions.Select(option => option.GroupId).ToHashSet();

        foreach (var duplicateGroup in selectedOptions.GroupBy(option => option.GroupId).Where(group => group.Count() > 1))
        {
            var groupName = product.OptionGroups.FirstOrDefault(group => group.Id == duplicateGroup.Key)?.Name ?? "أحد الخيارات";
            errors.Add($"لا يمكن اختيار أكثر من قيمة داخل {groupName} للمنتج {product.Name}.");
        }

        foreach (var option in selectedOptions)
        {
            var group = product.OptionGroups.FirstOrDefault(item => item.Id == option.GroupId && item.IsActive);
            if (group is null)
            {
                errors.Add($"خيار غير معروف للمنتج {product.Name}.");
                continue;
            }

            var value = group.Values.FirstOrDefault(item => item.Id == option.ValueId && item.IsActive);
            if (value is null)
            {
                errors.Add($"قيمة غير معروفة داخل خيار {group.Name} للمنتج {product.Name}.");
                continue;
            }

            extraPrice += value.ExtraPrice;
            summaryParts.Add($"{group.Name}: {value.Label}");
            detailItems.Add(new OrderItemCustomizationDetail("option", group.Id, group.Name, value.Id, value.Label, value.ExtraPrice));
        }

        foreach (var group in product.OptionGroups.Where(group => group.IsActive && group.IsRequired))
        {
            if (!selectedOptionGroupIds.Contains(group.Id))
            {
                errors.Add($"الخيار {group.Name} مطلوب للمنتج {product.Name}.");
            }
        }

        foreach (var requestField in requestItem.CustomFields.Where(field => field.FieldId != Guid.Empty))
        {
            var field = product.CustomizationFields.FirstOrDefault(item => item.Id == requestField.FieldId && item.IsActive);
            if (field is null)
            {
                errors.Add($"حقل تخصيص غير معروف للمنتج {product.Name}.");
                continue;
            }

            var normalizedValue = NormalizeNullableText(requestField.Value) ?? string.Empty;
            var selectedChoices = field.Choices
                .Where(choice => choice.IsActive && requestField.SelectedChoiceIds.Contains(choice.Id))
                .ToList();
            var hasValue = !string.IsNullOrWhiteSpace(normalizedValue) || selectedChoices.Count > 0;

            if (!hasValue)
            {
                continue;
            }

            if (field.MinLength.HasValue && normalizedValue.Length > 0 && normalizedValue.Length < field.MinLength.Value)
            {
                errors.Add($"قيمة {field.Label} أقصر من المطلوب.");
            }

            if (field.MaxLength.HasValue && normalizedValue.Length > field.MaxLength.Value)
            {
                errors.Add($"قيمة {field.Label} أطول من المسموح.");
            }

            if (field.MinValue.HasValue && decimal.TryParse(normalizedValue, out var minDecimalValue) && minDecimalValue < field.MinValue.Value)
            {
                errors.Add($"قيمة {field.Label} أقل من المسموح.");
            }

            if (field.MaxValue.HasValue && decimal.TryParse(normalizedValue, out var maxDecimalValue) && maxDecimalValue > field.MaxValue.Value)
            {
                errors.Add($"قيمة {field.Label} أكبر من المسموح.");
            }

            if (requestField.SelectedChoiceIds.Count > 0 && selectedChoices.Count != requestField.SelectedChoiceIds.Distinct().Count())
            {
                errors.Add($"أحد اختيارات {field.Label} غير معروف.");
            }

            var choicePrice = selectedChoices.Sum(choice => choice.AdditionalPrice);
            var fieldPrice = field.AdditionalPrice + choicePrice;
            var displayValue = selectedChoices.Count > 0
                ? string.Join("، ", selectedChoices.Select(choice => choice.Label))
                : field.FieldType == ProductCustomizationFieldType.Checkbox && normalizedValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                    ? "نعم"
                    : normalizedValue;

            extraPrice += fieldPrice;
            summaryParts.Add($"{field.Label}: {displayValue}");
            detailItems.Add(new OrderItemCustomizationDetail(
                "field",
                field.Id,
                field.Label,
                null,
                displayValue,
                fieldPrice));
        }

        foreach (var field in product.CustomizationFields.Where(field => field.IsActive && field.IsRequired))
        {
            var requestField = requestItem.CustomFields.FirstOrDefault(item => item.FieldId == field.Id);
            var hasValue = requestField is not null &&
                (!string.IsNullOrWhiteSpace(requestField.Value) || requestField.SelectedChoiceIds.Count > 0);

            if (!hasValue)
            {
                errors.Add($"حقل {field.Label} مطلوب للمنتج {product.Name}.");
            }
        }

        var customRequest = NormalizeNullableText(requestItem.CustomRequest);
        if (!string.IsNullOrWhiteSpace(customRequest))
        {
            summaryParts.Add($"طلب خاص: {customRequest}");
            detailItems.Add(new OrderItemCustomizationDetail("customRequest", Guid.Empty, "طلب خاص", null, customRequest, 0));
        }

        var summary = summaryParts.Count == 0 ? null : string.Join(" | ", summaryParts);
        var detailsJson = detailItems.Count == 0 ? null : JsonSerializer.Serialize(detailItems, JsonOptions);

        return new OrderItemCustomizationSnapshot(extraPrice, summary, detailsJson, errors);
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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record OrderItemCustomizationSnapshot(
        decimal ExtraPrice,
        string? Summary,
        string? DetailsJson,
        IReadOnlyList<string> Errors);

    private sealed record OrderItemCustomizationDetail(
        string Type,
        Guid SourceId,
        string Label,
        Guid? ValueId,
        string Value,
        decimal ExtraPrice);

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
                LineTotal = item.LineTotal,
                CustomizationSummary = item.CustomizationSummary ?? string.Empty,
                CustomizationDetailsJson = item.CustomizationDetailsJson ?? string.Empty
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
