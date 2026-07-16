using namera_API.Models.Common;

namespace namera_API.Models.Products.Products;

public sealed class ProductCustomizationChoice : BaseEntity
{
    public Guid ProductCustomizationFieldId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ProductCustomizationField ProductCustomizationField { get; set; } = null!;
}
