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
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Where(product => product.Status == ProductStatus.Active)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<ProductResponseDto>>.Ok(
            products.Select(ToProductResponse).ToList(),
            "تم تحميل المنتجات بنجاح");
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

    private static ProductResponseDto ToProductResponse(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            ShortDescription = product.ShortDescription ?? string.Empty,
            Description = product.Description ?? string.Empty,
            BasePrice = product.BasePrice,
            Status = product.Status.ToString(),
            IsFeatured = product.IsFeatured,
            IsCustomizable = product.IsCustomizable,
            MinimumQuantity = product.MinimumQuantity,
            MaximumQuantity = product.MaximumQuantity,
            PreparationTimeInDays = product.PreparationTimeInDays,
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
                .ToList()
        };
    }
}
