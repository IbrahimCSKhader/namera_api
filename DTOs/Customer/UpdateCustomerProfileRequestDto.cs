using System.ComponentModel.DataAnnotations;

namespace namera_API.DTOs.Customer;

public sealed class UpdateCustomerProfileRequestDto
{
    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    [MaxLength(100, ErrorMessage = "الاسم الأول طويل جداً")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "الاسم الأخير مطلوب")]
    [MaxLength(100, ErrorMessage = "الاسم الأخير طويل جداً")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "العنوان مطلوب")]
    [MaxLength(250, ErrorMessage = "العنوان طويل جداً")]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
    [MaxLength(20, ErrorMessage = "رقم الهاتف طويل جداً")]
    public string PhoneNumber { get; init; } = string.Empty;
}
