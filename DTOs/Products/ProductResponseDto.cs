namespace namera_API.DTOs.Products;

public sealed class ProductResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? BasePrice { get; init; }
    public string PricingType { get; init; } = string.Empty;
    public bool IsPriceVisible { get; init; }
    public string PriceLabel { get; init; } = string.Empty;
    public string Currency { get; init; } = "ILS";
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public bool IsNew { get; init; }
    public bool IsCustomizable { get; init; }
    public bool HasVariants { get; init; }
    public bool MadeToOrder { get; init; }
    public bool AllowOrdering { get; init; }
    public int MinimumQuantity { get; init; }
    public int? MaximumQuantity { get; init; }
    public int PreparationTimeInDays { get; init; }
    public string PreparationNote { get; init; } = string.Empty;
    public ProductCategoryResponseDto Category { get; init; } = new();
    public IReadOnlyList<ProductImageResponseDto> Images { get; init; } = [];
    public IReadOnlyList<ProductOptionGroupDto> OptionGroups { get; init; } = [];
    public IReadOnlyList<ProductCustomizationFieldDto> CustomizationFields { get; init; } = [];
}
