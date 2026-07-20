using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using namera_API.Constants.Identity;
using namera_API.Data;
using namera_API.DTOs.Orders;
using namera_API.Models.Identity;
using namera_API.Models.Orders;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;
using namera_API.Services.Orders;
using Xunit;

namespace namera_API.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_DoesNotDeductStock()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext, quantity: 5);

        var response = await fixture.Service.CreateOrderAsync(fixture.CustomerPrincipal, new CreateOrderRequestDto
        {
            Items = [new CreateOrderItemRequestDto { ProductId = product.Id, Quantity = 2 }]
        });

        Assert.True(response.Success);
        Assert.Equal("pending", response.Data!.Status);
        Assert.False(response.Data.StockDeducted);
        Assert.Equal(5, (await fixture.DbContext.Products.FindAsync(product.Id))!.Quantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_Approved_DeductsStockOnce()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext, quantity: 5);
        var created = await fixture.Service.CreateOrderAsync(fixture.CustomerPrincipal, new CreateOrderRequestDto
        {
            Items = [new CreateOrderItemRequestDto { ProductId = product.Id, Quantity = 2 }]
        });

        var approved = await fixture.Service.UpdateOrderStatusAsync(created.Data!.Id, new UpdateOrderStatusRequestDto { Status = "approved" });
        var approvedAgain = await fixture.Service.UpdateOrderStatusAsync(created.Data.Id, new UpdateOrderStatusRequestDto { Status = "received" });

        Assert.True(approved.Success);
        Assert.True(approvedAgain.Success);
        Assert.True(approvedAgain.Data!.StockDeducted);
        Assert.Equal(3, (await fixture.DbContext.Products.FindAsync(product.Id))!.Quantity);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_CancelledAfterApproval_RestoresStock()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext, quantity: 5);
        var created = await fixture.Service.CreateOrderAsync(fixture.CustomerPrincipal, new CreateOrderRequestDto
        {
            Items = [new CreateOrderItemRequestDto { ProductId = product.Id, Quantity = 2 }]
        });

        await fixture.Service.UpdateOrderStatusAsync(created.Data!.Id, new UpdateOrderStatusRequestDto { Status = "approved" });
        var cancelled = await fixture.Service.UpdateOrderStatusAsync(created.Data.Id, new UpdateOrderStatusRequestDto { Status = "cancelled" });

        Assert.True(cancelled.Success);
        Assert.False(cancelled.Data!.StockDeducted);
        Assert.Equal(5, (await fixture.DbContext.Products.FindAsync(product.Id))!.Quantity);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsProductsUnderDisabledCategory()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext, quantity: 5, categoryIsActive: false);

        var response = await fixture.Service.CreateOrderAsync(fixture.CustomerPrincipal, new CreateOrderRequestDto
        {
            Items = [new CreateOrderItemRequestDto { ProductId = product.Id, Quantity = 1 }]
        });

        Assert.False(response.Success);
        Assert.Contains(response.Errors, error => error.Contains("تصنيف", StringComparison.Ordinal));
        Assert.Equal(5, (await fixture.DbContext.Products.FindAsync(product.Id))!.Quantity);
    }

    private static async Task<TestFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Customer));
        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Mayar",
            LastName = "Sabta",
            UserName = "mayar",
            Email = "mayar@example.test",
            PhoneNumber = "0590000001",
            Address = "Customer address",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        var createResult = await userManager.CreateAsync(customer, "Customer12345");
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));
        var roleResult = await userManager.AddToRoleAsync(customer, AppRoles.Customer);
        Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        var savedCustomer = await userManager.FindByNameAsync(customer.UserName!);
        Assert.NotNull(savedCustomer);
        dbContext.ChangeTracker.Clear();

        var service = new OrderService(dbContext, userManager);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, savedCustomer!.Id.ToString())],
            "Test"));

        return new TestFixture(dbContext, service, principal);
    }

    private static async Task<Product> SeedProductAsync(AppDbContext dbContext, int quantity, bool categoryIsActive = true)
    {
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Resin gifts",
            Slug = $"resin-gifts-{Guid.NewGuid():N}",
            IsActive = categoryIsActive,
            DisplayOrder = 1
        };

        dbContext.ProductCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Name = "Resin memory piece",
            Slug = $"resin-memory-piece-{Guid.NewGuid():N}",
            Status = ProductStatus.Published,
            BasePrice = 50,
            Quantity = quantity,
            InventoryTrackingEnabled = true,
            MadeToOrder = false,
            AllowOrdering = true,
            MinimumQuantity = 1,
            Currency = "ILS"
        };

        dbContext.Products.Add(product);
        dbContext.ProductImages.Add(new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ImageUrl = "/uploads/products/primary.png",
            AltText = product.Name,
            IsPrimary = true,
            DisplayOrder = 1
        });

        await dbContext.SaveChangesAsync();

        return product;
    }

    private sealed record TestFixture(AppDbContext DbContext, OrderService Service, ClaimsPrincipal CustomerPrincipal);
}
