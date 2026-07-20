using Microsoft.EntityFrameworkCore;
using namera_API.Data;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;
using namera_API.Services.Products;
using Xunit;

namespace namera_API.Tests;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ReturnsOnlyStorefrontVisibleProducts()
    {
        await using var dbContext = CreateDbContext();
        var activeCategory = CreateCategory("Active category", "active-category", true);
        var inactiveCategory = CreateCategory("Inactive category", "inactive-category", false);

        dbContext.ProductCategories.AddRange(activeCategory, inactiveCategory);
        dbContext.Products.AddRange(
            CreateProduct(activeCategory, "Visible product", "visible-product", ProductStatus.Published),
            CreateProduct(activeCategory, "Draft product", "draft-product", ProductStatus.Draft),
            CreateProduct(activeCategory, "Direct product", "direct-product", ProductStatus.Published, directAccessOnly: true),
            CreateProduct(activeCategory, "Future product", "future-product", ProductStatus.Published, visibleFrom: DateTime.UtcNow.AddDays(2)),
            CreateProduct(inactiveCategory, "Inactive category product", "inactive-category-product", ProductStatus.Published));
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);
        var response = await service.GetProductsAsync();

        Assert.True(response.Success);
        var product = Assert.Single(response.Data!);
        Assert.Equal("Visible product", product.Name);
        Assert.Equal(activeCategory.Id, product.Category.Id);
    }

    [Fact]
    public async Task GetProductAsync_RejectsHiddenOrOutOfWindowProducts()
    {
        await using var dbContext = CreateDbContext();
        var category = CreateCategory("Active category", "active-category", true);

        dbContext.ProductCategories.Add(category);
        dbContext.Products.Add(CreateProduct(category, "Expired product", "expired-product", ProductStatus.Published, visibleTo: DateTime.UtcNow.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);
        var response = await service.GetProductAsync("expired-product");

        Assert.False(response.Success);
        Assert.Null(response.Data);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ProductCategory CreateCategory(string name, string slug, bool isActive)
    {
        return new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            IsActive = isActive,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product CreateProduct(
        ProductCategory category,
        string name,
        string slug,
        ProductStatus status,
        bool directAccessOnly = false,
        DateTime? visibleFrom = null,
        DateTime? visibleTo = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Category = category,
            CategoryId = category.Id,
            Name = name,
            Slug = slug,
            Status = status,
            BasePrice = 50,
            IsPriceVisible = true,
            Currency = "ILS",
            DirectAccessOnly = directAccessOnly,
            VisibleFrom = visibleFrom,
            VisibleTo = visibleTo,
            MinimumQuantity = 1,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };
    }
}
