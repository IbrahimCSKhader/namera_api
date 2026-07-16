using namera_API.Models.Common;

namespace namera_API.Models.Products.Products;

public sealed class ProductOptionGroup : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = new List<ProductOptionValue>();
}
