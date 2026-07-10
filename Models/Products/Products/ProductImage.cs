using namera_API.Models.Common;

namespace namera_API.Models.Products.Products;

public sealed class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
}
