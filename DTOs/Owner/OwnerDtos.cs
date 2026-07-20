using System.ComponentModel.DataAnnotations;

namespace namera_API.DTOs.Owner;

public sealed class OwnerProfileResponseDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}

public sealed class UpdateOwnerProfileRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; init; }

    [Required]
    [MaxLength(250)]
    public string Address { get; init; } = string.Empty;
}

public sealed class ChangeOwnerPasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class StoreSettingsDto
{
    public Guid Id { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string InstagramUrl { get; init; } = string.Empty;
    public string DefaultCurrency { get; init; } = "ILS";
    public string AboutText { get; init; } = string.Empty;
    public bool OrdersEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class UpdateStoreSettingsRequestDto
{
    [Required]
    [MaxLength(120)]
    public string StoreName { get; init; } = string.Empty;

    [MaxLength(20)]
    public string ContactPhone { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string ContactEmail { get; init; } = string.Empty;

    [MaxLength(300)]
    public string InstagramUrl { get; init; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string DefaultCurrency { get; init; } = "ILS";

    [MaxLength(1200)]
    public string AboutText { get; init; } = string.Empty;

    public bool OrdersEnabled { get; init; } = true;
}

public sealed class OwnerReviewResponseDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhoneNumber { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public bool IsVisible { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class UpdateReviewVisibilityRequestDto
{
    public bool IsVisible { get; init; }
}
