using namera_API.Models.Common;

namespace namera_API.Models.Products.Products;

public sealed class ProductOptionValue : BaseEntity
{
    public Guid ProductOptionGroupId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int? StockQuantity { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }

    public ProductOptionGroup ProductOptionGroup { get; set; } = null!;
}
