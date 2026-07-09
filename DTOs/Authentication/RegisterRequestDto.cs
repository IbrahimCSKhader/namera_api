using System.ComponentModel.DataAnnotations;

namespace namera_API.DTOs.Authentication;

public sealed class RegisterRequestDto
{
    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    [MaxLength(100, ErrorMessage = "الاسم الأول طويل جداً")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "الاسم الأخير مطلوب")]
    [MaxLength(100, ErrorMessage = "الاسم الأخير طويل جداً")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [RegularExpression("^[a-zA-Z0-9_.-]{3,50}$", ErrorMessage = "اسم المستخدم يجب أن يكون 3 أحرف على الأقل ويحتوي أحرفاً أو أرقاماً فقط")]
    public string UserName { get; init; } = string.Empty;

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [MaxLength(256, ErrorMessage = "البريد الإلكتروني طويل جداً")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
    [MaxLength(20, ErrorMessage = "رقم الهاتف طويل جداً")]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "العنوان مطلوب")]
    [MaxLength(250, ErrorMessage = "العنوان طويل جداً")]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
