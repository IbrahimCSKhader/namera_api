using Microsoft.EntityFrameworkCore;
using namera_API.Data;
using namera_API.DTOs.Products;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;
using namera_API.Services.Products;
using Xunit;

namespace namera_API.Tests;

public sealed class ProductManagementServiceTests
{
    [Fact]
    public async Task GetCategoriesAsync_ReturnsInactiveCategoriesAndProductCounts()
    {
        await using var dbContext = CreateDbContext();
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Keychains",
            Slug = "keychains",
            IsActive = false,
            DisplayOrder = 2
        };

        dbContext.ProductCategories.Add(category);
        dbContext.Products.AddRange(
            CreateProduct(category, "Visible keychain", "visible-keychain", ProductStatus.Published),
            CreateProduct(category, "Draft keychain", "draft-keychain", ProductStatus.Draft),
            CreateProduct(category, "Direct keychain", "direct-keychain", ProductStatus.Published, directAccessOnly: true));
        await dbContext.SaveChangesAsync();

        var service = new ProductManagementService(dbContext);
        var response = await service.GetCategoriesAsync();

        Assert.True(response.Success);
        var result = Assert.Single(response.Data!);
        Assert.False(result.IsActive);
        Assert.Equal(3, result.ProductsCount);
        Assert.Equal(0, result.VisibleProductsCount);
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdatesEditableFieldsAndKeepsSlugUnique()
    {
        await using var dbContext = CreateDbContext();
        var existingCategory = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Slug = "memory-boxes",
            IsActive = true,
            DisplayOrder = 1
        };
        var editedCategory = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Old name",
            Slug = "old-name",
            IsActive = true,
            DisplayOrder = 4
        };

        dbContext.ProductCategories.AddRange(existingCategory, editedCategory);
        await dbContext.SaveChangesAsync();

        var service = new ProductManagementService(dbContext);
        var response = await service.UpdateCategoryAsync(editedCategory.Id, new UpdateProductCategoryRequestDto
        {
            Name = "Memory boxes",
            Slug = "memory-boxes",
            Description = "Custom resin memory boxes",
            ImageUrl = "/uploads/categories/memory.png",
            DisplayOrder = 2,
            IsActive = false
        });

        Assert.True(response.Success);
        Assert.Equal("Memory boxes", response.Data!.Name);
        Assert.Equal("memory-boxes-2", response.Data.Slug);
        Assert.Equal(2, response.Data.DisplayOrder);
        Assert.False(response.Data.IsActive);
    }

    [Fact]
    public async Task SetCategoryActiveAsync_TogglesTheCategoryState()
    {
        await using var dbContext = CreateDbContext();
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Trays",
            Slug = "trays",
            IsActive = true,
            DisplayOrder = 1
        };

        dbContext.ProductCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = new ProductManagementService(dbContext);
        var response = await service.SetCategoryActiveAsync(category.Id, false);

        Assert.True(response.Success);
        Assert.False(response.Data!.IsActive);
        Assert.False((await dbContext.ProductCategories.FindAsync(category.Id))!.IsActive);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Product CreateProduct(
        ProductCategory category,
        string name,
        string slug,
        ProductStatus status,
        bool directAccessOnly = false)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Category = category,
            CategoryId = category.Id,
            Name = name,
            Slug = slug,
            Status = status,
            BasePrice = 20,
            DirectAccessOnly = directAccessOnly,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };
    }
}
