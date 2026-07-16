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
    public ProductPricingType PricingType { get; set; } = ProductPricingType.Fixed;
    public bool IsPriceVisible { get; set; } = true;
    public string? PriceLabel { get; set; }
    public string Currency { get; set; } = "ILS";
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsCustomizable { get; set; } = true;
    public bool HasVariants { get; set; }
    public bool InventoryTrackingEnabled { get; set; } = true;
    public int? Quantity { get; set; }
    public int LowStockThreshold { get; set; } = 3;
    public bool MadeToOrder { get; set; }
    public bool AllowBackorder { get; set; }
    public int MinimumQuantity { get; set; } = 1;
    public int? MaximumQuantity { get; set; }
    public int PreparationTimeInDays { get; set; }
    public int? MinPreparationDays { get; set; }
    public int? MaxPreparationDays { get; set; }
    public ProductPreparationUnit PreparationUnit { get; set; } = ProductPreparationUnit.Days;
    public string? PreparationNote { get; set; }
    public bool ShowOnHomepage { get; set; }
    public bool ShowInSuggestions { get; set; }
    public bool DirectAccessOnly { get; set; }
    public bool AllowRatings { get; set; } = true;
    public bool AllowOrdering { get; set; } = true;
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleTo { get; set; }
    public int DisplayOrder { get; set; }

    public ProductCategory Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductOptionGroup> OptionGroups { get; set; } = new List<ProductOptionGroup>();
    public ICollection<ProductCustomizationField> CustomizationFields { get; set; } = new List<ProductCustomizationField>();
}
