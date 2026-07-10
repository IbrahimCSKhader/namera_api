namespace namera_API.DTOs.Products;

public sealed class ProductResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public bool IsCustomizable { get; init; }
    public int MinimumQuantity { get; init; }
    public int? MaximumQuantity { get; init; }
    public int PreparationTimeInDays { get; init; }
    public ProductCategoryResponseDto Category { get; init; } = new();
    public IReadOnlyList<ProductImageResponseDto> Images { get; init; } = [];
}
