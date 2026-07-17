namespace namera_API.DTOs.Products;

public sealed class AdminProductListQueryDto
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Status { get; init; }
    public bool? Customized { get; init; }
    public bool LowStockOnly { get; init; }
}

public class CreateProductRequestDto
{
    public Guid? ClientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public Guid CategoryId { get; init; }
    public string Status { get; init; } = "draft";
    public string PricingType { get; init; } = "fixed";
    public decimal? BasePrice { get; init; }
    public bool IsPriceVisible { get; init; } = true;
    public string? PriceLabel { get; init; }
    public string Currency { get; init; } = "ILS";
    public bool HasVariants { get; init; }
    public IReadOnlyList<ProductImageRequestDto> Images { get; init; } = [];
    public IReadOnlyList<ProductOptionGroupRequestDto> OptionGroups { get; init; } = [];
    public IReadOnlyList<ProductCustomizationFieldRequestDto> CustomizationFields { get; init; } = [];
    public bool InventoryTrackingEnabled { get; init; } = true;
    public int? Quantity { get; init; }
    public int LowStockThreshold { get; init; } = 3;
    public bool MadeToOrder { get; init; }
    public bool AllowBackorder { get; init; }
    public int MinimumQuantity { get; init; } = 1;
    public int? MaximumQuantity { get; init; }
    public int? MinPreparationDays { get; init; }
    public int? MaxPreparationDays { get; init; }
    public string PreparationUnit { get; init; } = "days";
    public string? PreparationNote { get; init; }
    public bool ShowOnHomepage { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsNew { get; init; }
    public bool ShowInSuggestions { get; init; }
    public bool DirectAccessOnly { get; init; }
    public bool AllowRatings { get; init; } = true;
    public bool AllowOrdering { get; init; } = true;
    public int DisplayOrder { get; init; }
    public DateTime? VisibleFrom { get; init; }
    public DateTime? VisibleTo { get; init; }
}

public sealed class UpdateProductRequestDto : CreateProductRequestDto
{
}

public sealed class CreateProductCategoryRequestDto
{
    public Guid? ClientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class UploadProductImageRequestDto
{
    public Guid ProductId { get; init; }
    public IFormFile? File { get; init; }
}

public sealed class UploadCategoryImageRequestDto
{
    public Guid CategoryId { get; init; }
    public IFormFile? File { get; init; }
}

public sealed class UploadedMediaDto
{
    public string Url { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long Size { get; init; }
}

public sealed class ProductImageRequestDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class ProductOptionGroupRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; }
    public IReadOnlyList<ProductOptionValueRequestDto> Values { get; init; } = [];
}

public sealed class ProductOptionValueRequestDto
{
    public string Label { get; init; } = string.Empty;
    public decimal ExtraPrice { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsDefault { get; init; }
    public int? StockQuantity { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class ProductCustomizationFieldRequestDto
{
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = "shortText";
    public string? Description { get; init; }
    public string? Placeholder { get; init; }
    public bool IsRequired { get; init; }
    public int DisplayOrder { get; init; }
    public decimal AdditionalPrice { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public IReadOnlyList<string> AllowedFiles { get; init; } = [];
    public IReadOnlyList<ProductCustomizationChoiceRequestDto> Choices { get; init; } = [];
    public bool IsActive { get; init; } = true;
}

public sealed class ProductCustomizationChoiceRequestDto
{
    public string Label { get; init; } = string.Empty;
    public decimal AdditionalPrice { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class AdminProductListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PricingType { get; init; } = string.Empty;
    public decimal? BasePrice { get; init; }
    public string PriceLabel { get; init; } = string.Empty;
    public string InventoryLabel { get; init; } = string.Empty;
    public bool IsLowStock { get; init; }
    public bool HasCustomizations { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsVisible { get; init; }
    public int DisplayOrder { get; init; }
    public string PrimaryImageUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class ProductDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public ProductCategoryResponseDto Category { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public string PricingType { get; init; } = string.Empty;
    public decimal? BasePrice { get; init; }
    public bool IsPriceVisible { get; init; }
    public string PriceLabel { get; init; } = string.Empty;
    public string Currency { get; init; } = "ILS";
    public bool HasVariants { get; init; }
    public bool InventoryTrackingEnabled { get; init; }
    public int? Quantity { get; init; }
    public int LowStockThreshold { get; init; }
    public bool MadeToOrder { get; init; }
    public bool AllowBackorder { get; init; }
    public int MinimumQuantity { get; init; }
    public int? MaximumQuantity { get; init; }
    public int? MinPreparationDays { get; init; }
    public int? MaxPreparationDays { get; init; }
    public string PreparationUnit { get; init; } = "days";
    public string PreparationNote { get; init; } = string.Empty;
    public bool ShowOnHomepage { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsNew { get; init; }
    public bool ShowInSuggestions { get; init; }
    public bool DirectAccessOnly { get; init; }
    public bool AllowRatings { get; init; }
    public bool AllowOrdering { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime? VisibleFrom { get; init; }
    public DateTime? VisibleTo { get; init; }
    public IReadOnlyList<ProductImageResponseDto> Images { get; init; } = [];
    public IReadOnlyList<ProductOptionGroupDto> OptionGroups { get; init; } = [];
    public IReadOnlyList<ProductCustomizationFieldDto> CustomizationFields { get; init; } = [];
}

public sealed class ProductOptionGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyList<ProductOptionValueDto> Values { get; init; } = [];
}

public sealed class ProductOptionValueDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal ExtraPrice { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public int? StockQuantity { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
}

public sealed class ProductCustomizationFieldDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Placeholder { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public int DisplayOrder { get; init; }
    public decimal AdditionalPrice { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public IReadOnlyList<string> AllowedFiles { get; init; } = [];
    public bool IsActive { get; init; }
    public IReadOnlyList<ProductCustomizationChoiceDto> Choices { get; init; } = [];
}

public sealed class ProductCustomizationChoiceDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal AdditionalPrice { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
