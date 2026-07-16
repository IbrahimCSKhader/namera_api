using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Data;
using namera_API.DTOs.Products;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;

namespace namera_API.Services.Products;

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;

    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<IReadOnlyList<ProductResponseDto>>> GetProductsAsync()
    {
        var products = await VisibleProducts()
            .Where(product => !product.DirectAccessOnly)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ProductResponseDto>>.Ok(
            products.Select(ToProductResponse).ToList(),
            "تم تحميل المنتجات بنجاح");
    }

    public async Task<ApiResponse<ProductResponseDto>> GetProductAsync(string slug)
    {
        var product = await VisibleProducts()
            .FirstOrDefaultAsync(item => item.Slug == slug);

        if (product is null)
        {
            return ApiResponse<ProductResponseDto>.Fail("المنتج غير موجود");
        }

        return ApiResponse<ProductResponseDto>.Ok(ToProductResponse(product), "تم تحميل المنتج بنجاح");
    }

    public async Task<ApiResponse<IReadOnlyList<ProductCategoryResponseDto>>> GetCategoriesAsync()
    {
        var categories = await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new ProductCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description ?? string.Empty,
                ImageUrl = category.ImageUrl ?? string.Empty
            })
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ProductCategoryResponseDto>>.Ok(categories, "تم تحميل التصنيفات بنجاح");
    }

    private IQueryable<Product> VisibleProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Include(product => product.OptionGroups)
                .ThenInclude(group => group.Values)
            .Include(product => product.CustomizationFields)
                .ThenInclude(field => field.Choices)
            .Where(product =>
                product.Status == ProductStatus.Active ||
                product.Status == ProductStatus.Published ||
                product.Status == ProductStatus.OutOfStock ||
                product.Status == ProductStatus.Unavailable);
    }

    private static ProductResponseDto ToProductResponse(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            ShortDescription = product.ShortDescription ?? string.Empty,
            Description = product.Description ?? string.Empty,
            BasePrice = product.PricingType == ProductPricingType.Quote ? null : product.BasePrice,
            PricingType = product.PricingType switch
            {
                ProductPricingType.StartingFrom => "startingFrom",
                ProductPricingType.OptionsBased => "optionsBased",
                ProductPricingType.Quote => "quote",
                _ => "fixed"
            },
            IsPriceVisible = product.IsPriceVisible,
            PriceLabel = BuildPriceLabel(product),
            Currency = product.Currency,
            Status = product.Status.ToString(),
            IsFeatured = product.IsFeatured,
            IsNew = product.IsNew,
            IsCustomizable = product.IsCustomizable,
            HasVariants = product.HasVariants,
            MadeToOrder = product.MadeToOrder,
            AllowOrdering = product.AllowOrdering,
            MinimumQuantity = product.MinimumQuantity,
            MaximumQuantity = product.MaximumQuantity,
            PreparationTimeInDays = product.PreparationTimeInDays,
            PreparationNote = product.PreparationNote ?? string.Empty,
            Category = new ProductCategoryResponseDto
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                Slug = product.Category.Slug,
                Description = product.Category.Description ?? string.Empty,
                ImageUrl = product.Category.ImageUrl ?? string.Empty
            },
            Images = product.Images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => new ProductImageResponseDto
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    AltText = image.AltText ?? product.Name,
                    IsPrimary = image.IsPrimary,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList(),
            OptionGroups = product.OptionGroups
                .Where(group => group.IsActive)
                .OrderBy(group => group.DisplayOrder)
                .Select(group => new ProductOptionGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description ?? string.Empty,
                    IsRequired = group.IsRequired,
                    IsActive = group.IsActive,
                    DisplayOrder = group.DisplayOrder,
                    Values = group.Values
                        .Where(value => value.IsActive)
                        .OrderBy(value => value.DisplayOrder)
                        .Select(value => new ProductOptionValueDto
                        {
                            Id = value.Id,
                            Label = value.Label,
                            ExtraPrice = value.ExtraPrice,
                            DisplayOrder = value.DisplayOrder,
                            IsActive = value.IsActive,
                            IsDefault = value.IsDefault,
                            StockQuantity = value.StockQuantity,
                            Sku = value.Sku ?? string.Empty,
                            ImageUrl = value.ImageUrl ?? string.Empty
                        })
                        .ToList()
                })
                .ToList(),
            CustomizationFields = product.CustomizationFields
                .Where(field => field.IsActive)
                .OrderBy(field => field.DisplayOrder)
                .Select(field => new ProductCustomizationFieldDto
                {
                    Id = field.Id,
                    Label = field.Label,
                    Type = field.FieldType switch
                    {
                        ProductCustomizationFieldType.LongText => "longText",
                        ProductCustomizationFieldType.ImageUpload => "imageUpload",
                        ProductCustomizationFieldType.SingleSelect => "singleSelect",
                        ProductCustomizationFieldType.MultiSelect => "multiSelect",
                        ProductCustomizationFieldType.Checkbox => "checkbox",
                        ProductCustomizationFieldType.Date => "date",
                        ProductCustomizationFieldType.Number => "number",
                        _ => "shortText"
                    },
                    Description = field.Description ?? string.Empty,
                    Placeholder = field.Placeholder ?? string.Empty,
                    IsRequired = field.IsRequired,
                    DisplayOrder = field.DisplayOrder,
                    AdditionalPrice = field.AdditionalPrice,
                    MinLength = field.MinLength,
                    MaxLength = field.MaxLength,
                    MinValue = field.MinValue,
                    MaxValue = field.MaxValue,
                    AllowedFiles = string.IsNullOrWhiteSpace(field.AllowedFilesCsv)
                        ? []
                        : field.AllowedFilesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    IsActive = field.IsActive,
                    Choices = field.Choices
                        .Where(choice => choice.IsActive)
                        .OrderBy(choice => choice.DisplayOrder)
                        .Select(choice => new ProductCustomizationChoiceDto
                        {
                            Id = choice.Id,
                            Label = choice.Label,
                            AdditionalPrice = choice.AdditionalPrice,
                            DisplayOrder = choice.DisplayOrder,
                            IsActive = choice.IsActive
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static string BuildPriceLabel(Product product)
    {
        if (!product.IsPriceVisible || product.PricingType == ProductPricingType.Quote)
        {
            return string.IsNullOrWhiteSpace(product.PriceLabel) ? "السعر عند الطلب" : product.PriceLabel;
        }

        return product.PricingType switch
        {
            ProductPricingType.StartingFrom => $"يبدأ من {product.BasePrice:0.##} شيكل",
            ProductPricingType.OptionsBased => $"حسب الخيارات من {product.BasePrice:0.##} شيكل",
            _ => $"{product.BasePrice:0.##} شيكل"
        };
    }
}
