using System.ComponentModel.DataAnnotations;

namespace namera_API.DTOs.Authentication;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password { get; init; } = string.Empty;
}
