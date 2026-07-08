using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.DTOs.Customer;
using namera_API.Models.Identity;

namespace namera_API.Services.Customer;

public sealed class CustomerService : ICustomerService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerService(UserManager<ApplicationUser> userManager)
    {
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
}
