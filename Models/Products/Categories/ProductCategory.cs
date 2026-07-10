using namera_API.Models.Common;
using namera_API.Models.Products.Products;

namespace namera_API.Models.Products.Categories;

public sealed class ProductCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ProductCategory? ParentCategory { get; set; }
    public ICollection<ProductCategory> Subcategories { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
