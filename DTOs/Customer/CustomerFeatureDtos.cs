using System.ComponentModel.DataAnnotations;

namespace namera_API.DTOs.Customer;

public sealed class CustomerDashboardResponseDto
{
    public int TotalOrders { get; init; }
    public int ActiveOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int AddressesCount { get; init; }
    public int ReviewsCount { get; init; }
    public decimal TotalSpent { get; init; }
    public string? LastOrderNumber { get; init; }
    public string? LastOrderStatus { get; init; }
    public string? LastOrderStatusLabel { get; init; }
}

public sealed class CustomerAddressRequestDto
{
    [Required(ErrorMessage = "اسم العنوان مطلوب")]
    [MaxLength(80, ErrorMessage = "اسم العنوان طويل")]
    public string Label { get; init; } = string.Empty;

    [Required(ErrorMessage = "اسم المستلم مطلوب")]
    [MaxLength(160, ErrorMessage = "اسم المستلم طويل")]
    public string RecipientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [MaxLength(20, ErrorMessage = "رقم الهاتف طويل")]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "العنوان مطلوب")]
    [MaxLength(320, ErrorMessage = "العنوان طويل")]
    public string AddressLine { get; init; } = string.Empty;

    [Required(ErrorMessage = "المدينة مطلوبة")]
    [MaxLength(120, ErrorMessage = "اسم المدينة طويل")]
    public string City { get; init; } = string.Empty;

    [MaxLength(500, ErrorMessage = "الملاحظات طويلة")]
    public string? Notes { get; init; }

    public bool IsDefault { get; init; }
}

public sealed class CustomerAddressResponseDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CustomerReviewRequestDto
{
    [Required(ErrorMessage = "المنتج مطلوب")]
    public Guid ProductId { get; init; }

    [Range(0, 6, ErrorMessage = "التقييم يجب أن يكون بين 0 و 6")]
    public int Rating { get; init; }

    [Required(ErrorMessage = "نص التقييم مطلوب")]
    [MaxLength(1000, ErrorMessage = "نص التقييم طويل")]
    public string Comment { get; init; } = string.Empty;

    [MaxLength(180, ErrorMessage = "اسم الزبون طويل")]
    public string? CustomerName { get; init; }

    [MaxLength(30, ErrorMessage = "رقم الهاتف طويل")]
    public string? CustomerPhoneNumber { get; init; }
}

public sealed class CustomerReviewResponseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string ProductImageUrl { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public bool IsVisible { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
