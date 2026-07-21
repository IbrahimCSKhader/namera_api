using System.Security.Claims;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using namera_API.Common.Responses;
using namera_API.Configurations.Email;
using namera_API.Constants.Identity;
using namera_API.DTOs.Authentication;
using namera_API.Models.Identity;
using namera_API.Services.Email;
using namera_API.Services.Token;

namespace namera_API.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly SmtpEmailSettings _emailSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailSender emailSender,
        IOptions<SmtpEmailSettings> emailSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _emailSettings = emailSettings.Value;
    }

    public async Task<ApiResponse<RegistrationResponseDto>> RegisterCustomerAsync(RegisterRequestDto request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return ApiResponse<RegistrationResponseDto>.Fail("كلمة المرور غير متطابقة", ["كلمة المرور وتأكيدها يجب أن يكونا متطابقين"]);
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var phoneNumber = request.PhoneNumber.Trim();

        if (await _userManager.FindByNameAsync(userName) is not null)
        {
            return ApiResponse<RegistrationResponseDto>.Fail("اسم المستخدم مستخدم مسبقاً", ["اختر اسم مستخدم آخر"]);
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return ApiResponse<RegistrationResponseDto>.Fail("البريد الإلكتروني مستخدم مسبقاً", ["يوجد حساب مسجل بهذا البريد"]);
        }

        if (await PhoneNumberExistsAsync(phoneNumber))
        {
            return ApiResponse<RegistrationResponseDto>.Fail("رقم الهاتف مستخدم مسبقاً", ["يوجد حساب مسجل بهذا الرقم"]);
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Address = request.Address.Trim(),
            UserName = userName,
            Email = email,
            EmailConfirmed = false,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return ApiResponse<RegistrationResponseDto>.Fail("تعذر إنشاء الحساب", createResult.Errors.Select(error => error.Description).ToList());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Customer);

        if (!roleResult.Succeeded)
        {
            return ApiResponse<RegistrationResponseDto>.Fail("تم إنشاء الحساب لكن تعذر تعيين الدور", roleResult.Errors.Select(error => error.Description).ToList());
        }

        await SendEmailConfirmationAsync(user);

        return ApiResponse<RegistrationResponseDto>.Ok(new RegistrationResponseDto
        {
            UserId = user.Id,
            Email = email,
            RequiresEmailConfirmation = true
        }, "تم إنشاء الحساب. تحقق من بريدك الإلكتروني لتفعيل الحساب");
    }

    public async Task<ApiResponse<bool>> ConfirmEmailAsync(ConfirmEmailRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return ApiResponse<bool>.Fail("رابط التأكيد غير صالح");
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<bool>.Ok(true, "تم تأكيد البريد الإلكتروني مسبقاً");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded
            ? ApiResponse<bool>.Ok(true, "تم تأكيد البريد الإلكتروني بنجاح")
            : ApiResponse<bool>.Fail("رابط التأكيد غير صالح أو منتهي", result.Errors.Select(error => error.Description).ToList());
    }

    public async Task<ApiResponse<bool>> ResendEmailConfirmationAsync(ResendEmailConfirmationRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return ApiResponse<bool>.Ok(true, "إذا كان البريد مسجلاً ستصله رسالة تأكيد");
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<bool>.Ok(true, "البريد الإلكتروني مؤكد مسبقاً");
        }

        await SendEmailConfirmationAsync(user);
        return ApiResponse<bool>.Ok(true, "تم إرسال رابط التأكيد مرة أخرى");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var identifier = request.Identifier.Trim();
        var user = await FindUserByIdentifierAsync(identifier);

        if (user is null || !user.IsActive)
        {
            return InvalidLoginResponse();
        }

        if (!user.EmailConfirmed)
        {
            return ApiResponse<AuthResponseDto>.Fail(
                "يرجى تأكيد البريد الإلكتروني قبل تسجيل الدخول",
                ["افتح بريدك واضغط رابط التفعيل، أو اطلب إرسال رابط جديد"]);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return InvalidLoginResponse();
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
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier)
    {
        return await _userManager.FindByNameAsync(identifier)
            ?? await _userManager.FindByEmailAsync(identifier)
            ?? await _userManager.Users.FirstOrDefaultAsync(user => user.PhoneNumber == identifier);
    }

    private Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        return _userManager.Users.AnyAsync(user => user.PhoneNumber == phoneNumber);
    }

    private async Task SendEmailConfirmationAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Cannot send confirmation email without an email address.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = BuildConfirmationUrl(user.Id, token);
        var subject = "تأكيد البريد الإلكتروني - namera";
        var htmlBody = $"""
            <div dir="rtl" style="font-family:Arial,sans-serif;line-height:1.8;color:#241821">
              <h2>أهلاً {WebUtility.HtmlEncode(user.FirstName)}</h2>
              <p>شكراً لتسجيلك في namera. اضغط الزر التالي لتأكيد بريدك الإلكتروني وتفعيل حسابك.</p>
              <p>
                <a href="{WebUtility.HtmlEncode(confirmationUrl)}" style="display:inline-block;padding:12px 18px;background:#aa2f72;color:#fff;text-decoration:none;border-radius:10px;font-weight:bold">
                  تأكيد البريد الإلكتروني
                </a>
              </p>
              <p>إذا لم تطلب إنشاء حساب، تجاهل هذه الرسالة.</p>
            </div>
            """;

        await _emailSender.SendEmailAsync(user.Email, subject, htmlBody);
    }

    private string BuildConfirmationUrl(Guid userId, string token)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_emailSettings.FrontendBaseUrl)
            ? "http://localhost:5173"
            : _emailSettings.FrontendBaseUrl.TrimEnd('/');

        return $"{baseUrl}/confirm-email?userId={Uri.EscapeDataString(userId.ToString())}&token={Uri.EscapeDataString(token)}";
    }

    private static ApiResponse<AuthResponseDto> InvalidLoginResponse()
    {
        return ApiResponse<AuthResponseDto>.Fail(
            "بيانات الدخول غير صحيحة",
            ["رقم الهاتف أو البريد الإلكتروني أو اسم المستخدم أو كلمة المرور غير صحيحة"]);
    }
}
