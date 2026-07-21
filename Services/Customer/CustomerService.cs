using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Data;
using namera_API.DTOs.Customer;
using namera_API.Models.Customers;
using namera_API.Models.Identity;
using namera_API.Models.Orders;
using namera_API.Models.Products.Enums;

namespace namera_API.Services.Customer;

public sealed class CustomerService : ICustomerService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerService(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<ApiResponse<CustomerProfileResponseDto>> GetProfileAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<CustomerProfileResponseDto>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        return ApiResponse<CustomerProfileResponseDto>.Ok(ToProfileResponse(user));
    }

    public async Task<ApiResponse<CustomerProfileResponseDto>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateCustomerProfileRequestDto request)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<CustomerProfileResponseDto>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var phoneNumber = request.PhoneNumber.Trim();
        var phoneOwner = await _userManager.Users.FirstOrDefaultAsync(item => item.PhoneNumber == phoneNumber);

        if (phoneOwner is not null && phoneOwner.Id != user.Id)
        {
            return ApiResponse<CustomerProfileResponseDto>.Fail("رقم الهاتف مستخدم مسبقاً", ["اختر رقم هاتف آخر"]);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Address = request.Address.Trim();
        user.PhoneNumber = phoneNumber;
        user.UserName = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse<CustomerProfileResponseDto>.Fail("تعذر تحديث الملف الشخصي", result.Errors.Select(error => error.Description).ToList());
        }

        return ApiResponse<CustomerProfileResponseDto>.Ok(ToProfileResponse(user), "تم تحديث الملف الشخصي بنجاح");
    }

    public async Task<ApiResponse<CustomerDashboardResponseDto>> GetDashboardAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<CustomerDashboardResponseDto>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == user.Id)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        var lastOrder = orders.FirstOrDefault();
        var terminalStatuses = new[] { OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.Rejected };
        var addressesCount = await _dbContext.CustomerAddresses.CountAsync(address => address.CustomerId == user.Id);
        var reviewsCount = await _dbContext.ProductReviews.CountAsync(review => review.CustomerId == user.Id);

        return ApiResponse<CustomerDashboardResponseDto>.Ok(new CustomerDashboardResponseDto
        {
            TotalOrders = orders.Count,
            ActiveOrders = orders.Count(order => !terminalStatuses.Contains(order.Status)),
            CompletedOrders = orders.Count(order => order.Status == OrderStatus.Completed),
            AddressesCount = addressesCount,
            ReviewsCount = reviewsCount,
            TotalSpent = orders
                .Where(order => order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Rejected)
                .Sum(order => order.Total),
            LastOrderNumber = lastOrder?.OrderNumber,
            LastOrderStatus = lastOrder?.Status.ToString().ToLowerInvariant(),
            LastOrderStatusLabel = lastOrder is null ? null : GetStatusLabel(lastOrder.Status)
        });
    }

    public async Task<ApiResponse<IReadOnlyList<CustomerAddressResponseDto>>> GetAddressesAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<IReadOnlyList<CustomerAddressResponseDto>>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var addresses = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(address => address.CustomerId == user.Id)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.CreatedAt)
            .Select(address => ToAddressResponse(address))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<CustomerAddressResponseDto>>.Ok(addresses);
    }

    public async Task<ApiResponse<CustomerAddressResponseDto>> CreateAddressAsync(ClaimsPrincipal principal, CustomerAddressRequestDto request)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<CustomerAddressResponseDto>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var hasAddress = await _dbContext.CustomerAddresses.AnyAsync(address => address.CustomerId == user.Id);
        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = user.Id,
            Label = request.Label.Trim(),
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            Notes = request.Notes?.Trim(),
            IsDefault = request.IsDefault || !hasAddress
        };

        if (address.IsDefault)
        {
            await ClearDefaultAddressesAsync(user.Id);
        }

        _dbContext.CustomerAddresses.Add(address);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<CustomerAddressResponseDto>.Ok(ToAddressResponse(address), "تم حفظ العنوان بنجاح");
    }

    public async Task<ApiResponse<CustomerAddressResponseDto>> UpdateAddressAsync(ClaimsPrincipal principal, Guid addressId, CustomerAddressRequestDto request)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<CustomerAddressResponseDto>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var address = await _dbContext.CustomerAddresses.FirstOrDefaultAsync(item => item.Id == addressId && item.CustomerId == user.Id);

        if (address is null)
        {
            return ApiResponse<CustomerAddressResponseDto>.Fail("العنوان غير موجود", ["تعذر العثور على العنوان المطلوب"]);
        }

        address.Label = request.Label.Trim();
        address.RecipientName = request.RecipientName.Trim();
        address.PhoneNumber = request.PhoneNumber.Trim();
        address.AddressLine = request.AddressLine.Trim();
        address.City = request.City.Trim();
        address.Notes = request.Notes?.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = DateTime.UtcNow;

        if (address.IsDefault)
        {
            await ClearDefaultAddressesAsync(user.Id, address.Id);
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponse<CustomerAddressResponseDto>.Ok(ToAddressResponse(address), "تم تحديث العنوان بنجاح");
    }

    public async Task<ApiResponse<bool>> DeleteAddressAsync(ClaimsPrincipal principal, Guid addressId)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<bool>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var address = await _dbContext.CustomerAddresses.FirstOrDefaultAsync(item => item.Id == addressId && item.CustomerId == user.Id);

        if (address is null)
        {
            return ApiResponse<bool>.Fail("العنوان غير موجود", ["تعذر العثور على العنوان المطلوب"]);
        }

        var wasDefault = address.IsDefault;
        _dbContext.CustomerAddresses.Remove(address);
        await _dbContext.SaveChangesAsync();

        if (wasDefault)
        {
            var nextAddress = await _dbContext.CustomerAddresses
                .Where(item => item.CustomerId == user.Id)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextAddress is not null)
            {
                nextAddress.IsDefault = true;
                nextAddress.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        return ApiResponse<bool>.Ok(true, "تم حذف العنوان بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<CustomerReviewResponseDto>>> GetReviewsAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<IReadOnlyList<CustomerReviewResponseDto>>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var reviews = await _dbContext.ProductReviews
            .AsNoTracking()
            .Include(review => review.Product)
                .ThenInclude(product => product.Images)
            .Where(review => review.CustomerId == user.Id)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => ToReviewResponse(review))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<CustomerReviewResponseDto>>.Ok(reviews);
    }

    public async Task<ApiResponse<CustomerReviewResponseDto>> SaveReviewAsync(ClaimsPrincipal principal, CustomerReviewRequestDto request)
    {
        var user = await GetUserAsync(principal);

        var guestName = NormalizeNullableText(request.CustomerName);
        var guestPhoneNumber = NormalizeNullableText(request.CustomerPhoneNumber);
        if (user is null && (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(guestPhoneNumber)))
        {
            return ApiResponse<CustomerReviewResponseDto>.Fail("أكمل معلومات التقييم", ["اسم الزبون ورقم الهاتف مطلوبان لإرسال تقييم بدون حساب"]);
        }

        var product = await _dbContext.Products
            .Include(item => item.Category)
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == request.ProductId);

        if (product is null)
        {
            return ApiResponse<CustomerReviewResponseDto>.Fail("المنتج غير موجود", ["اختر منتجاً موجوداً"]);
        }

        if (!product.AllowRatings || !product.Category.IsActive || product.Status is ProductStatus.Hidden or ProductStatus.Archived)
        {
            return ApiResponse<CustomerReviewResponseDto>.Fail("لا يمكن تقييم هذا المنتج", ["المنتج غير متاح للتقييم حالياً"]);
        }

        var review = user is null
            ? null
            : await _dbContext.ProductReviews
                .Include(item => item.Product)
                    .ThenInclude(item => item.Images)
                .FirstOrDefaultAsync(item => item.CustomerId == user.Id && item.ProductId == request.ProductId);

        if (review is null)
        {
            review = new ProductReview
            {
                Id = Guid.NewGuid(),
                CustomerId = user?.Id,
                ProductId = product.Id,
                Product = product,
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                GuestName = user is null ? guestName : null,
                GuestPhoneNumber = user is null ? guestPhoneNumber : null
            };
            _dbContext.ProductReviews.Add(review);
        }
        else
        {
            review.Rating = request.Rating;
            review.Comment = request.Comment.Trim();
            review.IsVisible = true;
            review.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponse<CustomerReviewResponseDto>.Ok(ToReviewResponse(review), "تم حفظ التقييم بنجاح");
    }

    public async Task<ApiResponse<bool>> DeleteReviewAsync(ClaimsPrincipal principal, Guid reviewId)
    {
        var user = await GetUserAsync(principal);

        if (user is null)
        {
            return ApiResponse<bool>.Fail("المستخدم غير موجود", ["تعذر العثور على حساب العميل"]);
        }

        var review = await _dbContext.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId && item.CustomerId == user.Id);

        if (review is null)
        {
            return ApiResponse<bool>.Fail("التقييم غير موجود", ["تعذر العثور على التقييم المطلوب"]);
        }

        _dbContext.ProductReviews.Remove(review);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "تم حذف التقييم بنجاح");
    }

    private async Task ClearDefaultAddressesAsync(Guid customerId, Guid? exceptAddressId = null)
    {
        var addresses = await _dbContext.CustomerAddresses
            .Where(address => address.CustomerId == customerId && address.IsDefault && address.Id != exceptAddressId)
            .ToListAsync();

        foreach (var address in addresses)
        {
            address.IsDefault = false;
            address.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
            ? await _userManager.FindByIdAsync(userId.ToString())
            : null;
    }

    private static CustomerProfileResponseDto ToProfileResponse(ApplicationUser user)
    {
        return new CustomerProfileResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address
        };
    }

    private static CustomerAddressResponseDto ToAddressResponse(CustomerAddress address)
    {
        return new CustomerAddressResponseDto
        {
            Id = address.Id,
            Label = address.Label,
            RecipientName = address.RecipientName,
            PhoneNumber = address.PhoneNumber,
            AddressLine = address.AddressLine,
            City = address.City,
            Notes = address.Notes ?? string.Empty,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }

    private static CustomerReviewResponseDto ToReviewResponse(ProductReview review)
    {
        var primaryImage = review.Product.Images
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .FirstOrDefault();

        return new CustomerReviewResponseDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.Product.Name,
            ProductSlug = review.Product.Slug,
            ProductImageUrl = primaryImage?.ImageUrl ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVisible = review.IsVisible,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string GetStatusLabel(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "جديد",
            OrderStatus.Approved => "مقبول",
            OrderStatus.Received => "تم استلامه",
            OrderStatus.Preparing => "قيد التجهيز",
            OrderStatus.Ready => "جاهز",
            OrderStatus.Shipped => "تم الشحن",
            OrderStatus.Completed => "مكتمل",
            OrderStatus.Cancelled => "ملغي",
            OrderStatus.Rejected => "مرفوض",
            _ => status.ToString()
        };
    }
}
