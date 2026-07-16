using namera_API.Models.Common;
using namera_API.Models.Products.Enums;

namespace namera_API.Models.Products.Products;

public sealed class ProductCustomizationField : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Label { get; set; } = string.Empty;
    public ProductCustomizationFieldType FieldType { get; set; } = ProductCustomizationFieldType.ShortText;
    public string? Description { get; set; }
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public decimal AdditionalPrice { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? AllowedFilesCsv { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public ICollection<ProductCustomizationChoice> Choices { get; set; } = new List<ProductCustomizationChoice>();
}
