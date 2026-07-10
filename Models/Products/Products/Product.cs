using namera_API.Models.Common;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;

namespace namera_API.Models.Products.Products;

public sealed class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public bool IsFeatured { get; set; }
    public bool IsCustomizable { get; set; } = true;
    public int MinimumQuantity { get; set; } = 1;
    public int? MaximumQuantity { get; set; }
    public int PreparationTimeInDays { get; set; }
    public int DisplayOrder { get; set; }

    public ProductCategory Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
