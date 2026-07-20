using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Data;
using namera_API.DTOs.Owner;
using namera_API.Models.Customers;
using namera_API.Models.Identity;
using namera_API.Models.Store;

namespace namera_API.Services.Owner;

public sealed class OwnerAccountService : IOwnerAccountService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public OwnerAccountService(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<ApiResponse<OwnerProfileResponseDto>> GetProfileAsync(ClaimsPrincipal principal)
    {
        var owner = await GetUserAsync(principal);

        return owner is null
            ? ApiResponse<OwnerProfileResponseDto>.Fail("صاحب المتجر غير موجود", ["تعذر العثور على حساب صاحب المتجر"])
            : ApiResponse<OwnerProfileResponseDto>.Ok(ToProfileResponse(owner));
    }

    public async Task<ApiResponse<OwnerProfileResponseDto>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateOwnerProfileRequestDto request)
    {
        var owner = await GetUserAsync(principal);

        if (owner is null)
        {
            return ApiResponse<OwnerProfileResponseDto>.Fail("صاحب المتجر غير موجود", ["تعذر العثور على حساب صاحب المتجر"]);
        }

        var phoneNumber = request.PhoneNumber.Trim();
        var phoneOwner = await _userManager.Users.FirstOrDefaultAsync(user => user.PhoneNumber == phoneNumber);

        if (phoneOwner is not null && phoneOwner.Id != owner.Id)
        {
            return ApiResponse<OwnerProfileResponseDto>.Fail("رقم الهاتف مستخدم مسبقاً", ["اختر رقم هاتف آخر"]);
        }

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (email is not null)
        {
            var emailOwner = await _userManager.FindByEmailAsync(email);
            if (emailOwner is not null && emailOwner.Id != owner.Id)
            {
                return ApiResponse<OwnerProfileResponseDto>.Fail("البريد الإلكتروني مستخدم مسبقاً", ["اختر بريداً إلكترونياً آخر"]);
            }
        }

        owner.FirstName = request.FirstName.Trim();
        owner.LastName = request.LastName.Trim();
        owner.PhoneNumber = phoneNumber;
        owner.Email = email;
        owner.EmailConfirmed = email is not null;
        owner.Address = request.Address.Trim();
        owner.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(owner);
        if (!result.Succeeded)
        {
            return ApiResponse<OwnerProfileResponseDto>.Fail("تعذر تحديث الملف الشخصي", result.Errors.Select(error => error.Description).ToList());
        }

        return ApiResponse<OwnerProfileResponseDto>.Ok(ToProfileResponse(owner), "تم تحديث الملف الشخصي بنجاح");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(ClaimsPrincipal principal, ChangeOwnerPasswordRequestDto request)
    {
        var owner = await GetUserAsync(principal);

        if (owner is null)
        {
            return ApiResponse<bool>.Fail("صاحب المتجر غير موجود", ["تعذر العثور على حساب صاحب المتجر"]);
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return ApiResponse<bool>.Fail("كلمة المرور غير متطابقة", ["كلمة المرور الجديدة وتأكيدها يجب أن يكونا متطابقين"]);
        }

        var result = await _userManager.ChangePasswordAsync(owner, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Fail("تعذر تغيير كلمة المرور", result.Errors.Select(error => error.Description).ToList());
        }

        owner.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(owner);

        return ApiResponse<bool>.Ok(true, "تم تغيير كلمة المرور بنجاح");
    }

    public async Task<ApiResponse<StoreSettingsDto>> GetSettingsAsync()
    {
        var settings = await GetOrCreateSettingsAsync();
        return ApiResponse<StoreSettingsDto>.Ok(ToSettingsResponse(settings));
    }

    public async Task<ApiResponse<StoreSettingsDto>> UpdateSettingsAsync(UpdateStoreSettingsRequestDto request)
    {
        var settings = await GetOrCreateSettingsAsync();

        settings.StoreName = request.StoreName.Trim();
        settings.ContactPhone = request.ContactPhone.Trim();
        settings.ContactEmail = request.ContactEmail.Trim();
        settings.InstagramUrl = request.InstagramUrl.Trim();
        settings.DefaultCurrency = request.DefaultCurrency.Trim();
        settings.AboutText = request.AboutText.Trim();
        settings.OrdersEnabled = request.OrdersEnabled;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return ApiResponse<StoreSettingsDto>.Ok(ToSettingsResponse(settings), "تم تحديث إعدادات المتجر بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<OwnerReviewResponseDto>>> GetReviewsAsync(bool? visible)
    {
        var query = _dbContext.ProductReviews
            .AsNoTracking()
            .Include(review => review.Customer)
            .Include(review => review.Product)
            .AsQueryable();

        if (visible.HasValue)
        {
            query = query.Where(review => review.IsVisible == visible.Value);
        }

        var reviews = await query
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => ToReviewResponse(review))
            .ToListAsync();

        return ApiResponse<IReadOnlyList<OwnerReviewResponseDto>>.Ok(reviews);
    }

    public async Task<ApiResponse<OwnerReviewResponseDto>> SetReviewVisibilityAsync(Guid reviewId, bool isVisible)
    {
        var review = await _dbContext.ProductReviews
            .Include(item => item.Customer)
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == reviewId);

        if (review is null)
        {
            return ApiResponse<OwnerReviewResponseDto>.Fail("التقييم غير موجود", ["تعذر العثور على التقييم المطلوب"]);
        }

        review.IsVisible = isVisible;
        review.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ApiResponse<OwnerReviewResponseDto>.Ok(ToReviewResponse(review), "تم تحديث حالة التقييم");
    }

    public async Task<ApiResponse<bool>> DeleteReviewAsync(Guid reviewId)
    {
        var review = await _dbContext.ProductReviews.FirstOrDefaultAsync(item => item.Id == reviewId);

        if (review is null)
        {
            return ApiResponse<bool>.Fail("التقييم غير موجود", ["تعذر العثور على التقييم المطلوب"]);
        }

        _dbContext.ProductReviews.Remove(review);
        await _dbContext.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "تم حذف التقييم بنجاح");
    }

    private async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
            ? await _userManager.FindByIdAsync(userId.ToString())
            : null;
    }

    private async Task<StoreSettings> GetOrCreateSettingsAsync()
    {
        var settings = await _dbContext.StoreSettings.OrderBy(item => item.CreatedAt).FirstOrDefaultAsync();

        if (settings is not null)
        {
            return settings;
        }

        settings = new StoreSettings
        {
            Id = Guid.NewGuid(),
            StoreName = "Resin Bon",
            ContactEmail = "namera@gmail.com",
            ContactPhone = "0590000000",
            DefaultCurrency = "ILS",
            AboutText = "نصنع قطع ريزن يدوية مخصصة لتخليد الذكريات والهدايا.",
            OrdersEnabled = true
        };

        _dbContext.StoreSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
        return settings;
    }

    private static OwnerProfileResponseDto ToProfileResponse(ApplicationUser owner)
    {
        return new OwnerProfileResponseDto
        {
            Id = owner.Id,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            UserName = owner.UserName ?? string.Empty,
            Email = owner.Email ?? string.Empty,
            PhoneNumber = owner.PhoneNumber ?? string.Empty,
            Address = owner.Address
        };
    }

    private static StoreSettingsDto ToSettingsResponse(StoreSettings settings)
    {
        return new StoreSettingsDto
        {
            Id = settings.Id,
            StoreName = settings.StoreName,
            ContactPhone = settings.ContactPhone,
            ContactEmail = settings.ContactEmail,
            InstagramUrl = settings.InstagramUrl,
            DefaultCurrency = settings.DefaultCurrency,
            AboutText = settings.AboutText,
            OrdersEnabled = settings.OrdersEnabled,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private static OwnerReviewResponseDto ToReviewResponse(ProductReview review)
    {
        return new OwnerReviewResponseDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.Product.Name,
            ProductSlug = review.Product.Slug,
            CustomerName = $"{review.Customer.FirstName} {review.Customer.LastName}".Trim(),
            CustomerPhoneNumber = review.Customer.PhoneNumber ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVisible = review.IsVisible,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
