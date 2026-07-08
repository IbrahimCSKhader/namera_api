using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Constants.Identity;
using namera_API.DTOs.Authentication;
using namera_API.Models.Identity;
using namera_API.Services.Token;

namespace namera_API.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterCustomerAsync(RegisterRequestDto request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return ApiResponse<AuthResponseDto>.Fail("كلمة المرور غير متطابقة", ["كلمة المرور وتأكيدها يجب أن يكونا متطابقين"]);
        }

        if (await PhoneNumberExistsAsync(request.PhoneNumber))
        {
            return ApiResponse<AuthResponseDto>.Fail("رقم الهاتف مستخدم مسبقاً", ["يوجد حساب مسجل بهذا الرقم"]);
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Address = request.Address.Trim(),
            UserName = request.PhoneNumber.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            PhoneNumberConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return ApiResponse<AuthResponseDto>.Fail("تعذر إنشاء الحساب", createResult.Errors.Select(error => error.Description).ToList());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Customer);

        if (!roleResult.Succeeded)
        {
            return ApiResponse<AuthResponseDto>.Fail("تم إنشاء الحساب لكن تعذر تعيين الدور", roleResult.Errors.Select(error => error.Description).ToList());
        }

        var response = _tokenService.CreateToken(user, [AppRoles.Customer]);
        return ApiResponse<AuthResponseDto>.Ok(response, "تم إنشاء الحساب بنجاح");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var phoneNumber = request.PhoneNumber.Trim();
        var user = await _userManager.Users.FirstOrDefaultAsync(item => item.PhoneNumber == phoneNumber);

        if (user is null || !user.IsActive)
        {
            return ApiResponse<AuthResponseDto>.Fail("بيانات الدخول غير صحيحة", ["رقم الهاتف أو كلمة المرور غير صحيحة"]);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return ApiResponse<AuthResponseDto>.Fail("بيانات الدخول غير صحيحة", ["رقم الهاتف أو كلمة المرور غير صحيحة"]);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = _tokenService.CreateToken(user, roles.ToList());

        return ApiResponse<AuthResponseDto>.Ok(response, "تم تسجيل الدخول بنجاح");
    }

    public async Task<ApiResponse<CurrentUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return ApiResponse<CurrentUserDto>.Fail("المستخدم غير معروف", ["الرمز المرسل غير صالح"]);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive)
        {
            return ApiResponse<CurrentUserDto>.Fail("المستخدم غير موجود", ["تعذر العثور على الحساب"]);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ApiResponse<CurrentUserDto>.Ok(new CurrentUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    private Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        var normalizedPhoneNumber = phoneNumber.Trim();
        return _userManager.Users.AnyAsync(user => user.PhoneNumber == normalizedPhoneNumber);
    }
}
