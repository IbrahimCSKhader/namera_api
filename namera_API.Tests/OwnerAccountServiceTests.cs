using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using namera_API.Constants.Identity;
using namera_API.Data;
using namera_API.DTOs.Owner;
using namera_API.Models.Customers;
using namera_API.Models.Identity;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;
using namera_API.Services.Owner;
using Xunit;

namespace namera_API.Tests;

public sealed class OwnerAccountServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_CreatesDefaultSettings()
    {
        var fixture = await CreateFixtureAsync();

        var response = await fixture.Service.GetSettingsAsync();

        Assert.True(response.Success);
        Assert.Equal("Resin Bon", response.Data!.StoreName);
        Assert.Equal(1, await fixture.DbContext.StoreSettings.CountAsync());
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesExistingSettings()
    {
        var fixture = await CreateFixtureAsync();
        await fixture.Service.GetSettingsAsync();

        var response = await fixture.Service.UpdateSettingsAsync(new UpdateStoreSettingsRequestDto
        {
            StoreName = "Resin Bon Updated",
            ContactPhone = "0599999999",
            ContactEmail = "owner@example.test",
            InstagramUrl = "https://instagram.com/resinbon",
            DefaultCurrency = "ILS",
            AboutText = "Updated settings",
            OrdersEnabled = false
        });

        Assert.True(response.Success);
        Assert.Equal("Resin Bon Updated", response.Data!.StoreName);
        Assert.False(response.Data.OrdersEnabled);
        Assert.Equal(1, await fixture.DbContext.StoreSettings.CountAsync());
    }

    [Fact]
    public async Task SetReviewVisibilityAsync_HidesAndShowsReview()
    {
        var fixture = await CreateFixtureAsync();
        var review = await SeedReviewAsync(fixture.DbContext, fixture.Customer);

        var hidden = await fixture.Service.SetReviewVisibilityAsync(review.Id, false);
        var shown = await fixture.Service.SetReviewVisibilityAsync(review.Id, true);

        Assert.True(hidden.Success);
        Assert.False(hidden.Data!.IsVisible);
        Assert.True(shown.Success);
        Assert.True(shown.Data!.IsVisible);
    }

    [Fact]
    public async Task DeleteReviewAsync_RemovesReview()
    {
        var fixture = await CreateFixtureAsync();
        var review = await SeedReviewAsync(fixture.DbContext, fixture.Customer);

        var response = await fixture.Service.DeleteReviewAsync(review.Id);

        Assert.True(response.Success);
        Assert.Equal(0, await fixture.DbContext.ProductReviews.CountAsync());
    }

    [Fact]
    public async Task ChangePasswordAsync_RejectsMismatchedConfirmation()
    {
        var fixture = await CreateFixtureAsync();

        var response = await fixture.Service.ChangePasswordAsync(fixture.OwnerPrincipal, new ChangeOwnerPasswordRequestDto
        {
            CurrentPassword = "Owner12345",
            NewPassword = "NewOwner12345",
            ConfirmPassword = "Different12345"
        });

        Assert.False(response.Success);
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

        await roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Owner));
        await roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Customer));

        var owner = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Namira",
            LastName = "Owner",
            UserName = "owner",
            Email = "owner@example.test",
            PhoneNumber = "0590000000",
            Address = "Store address",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
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

        Assert.True((await userManager.CreateAsync(owner, "Owner12345")).Succeeded);
        Assert.True((await userManager.CreateAsync(customer, "Customer12345")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(owner, AppRoles.Owner)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(customer, AppRoles.Customer)).Succeeded);

        var savedOwner = await userManager.FindByNameAsync(owner.UserName!);
        var savedCustomer = await userManager.FindByNameAsync(customer.UserName!);
        Assert.NotNull(savedOwner);
        Assert.NotNull(savedCustomer);
        dbContext.ChangeTracker.Clear();

        var service = new OwnerAccountService(dbContext, userManager);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, savedOwner!.Id.ToString())],
            "Test"));

        return new TestFixture(dbContext, service, principal, savedCustomer!);
    }

    private static async Task<ProductReview> SeedReviewAsync(AppDbContext dbContext, ApplicationUser customer)
    {
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = "Resin gifts",
            Slug = $"resin-gifts-{Guid.NewGuid():N}",
            IsActive = true,
            DisplayOrder = 1
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Name = "Resin memory piece",
            Slug = $"resin-memory-piece-{Guid.NewGuid():N}",
            Status = ProductStatus.Published,
            BasePrice = 50,
            Quantity = 5,
            InventoryTrackingEnabled = true,
            AllowRatings = true,
            AllowOrdering = true,
            MinimumQuantity = 1,
            Currency = "ILS"
        };
        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ProductId = product.Id,
            Product = product,
            Rating = 5,
            Comment = "Beautiful",
            IsVisible = true
        };

        dbContext.ProductCategories.Add(category);
        dbContext.Products.Add(product);
        dbContext.ProductReviews.Add(review);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        return review;
    }

    private sealed record TestFixture(AppDbContext DbContext, OwnerAccountService Service, ClaimsPrincipal OwnerPrincipal, ApplicationUser Customer);
}
