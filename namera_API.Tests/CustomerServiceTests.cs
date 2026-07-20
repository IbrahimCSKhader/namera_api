using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using namera_API.Constants.Identity;
using namera_API.Data;
using namera_API.DTOs.Customer;
using namera_API.Models.Identity;
using namera_API.Models.Orders;
using namera_API.Models.Products.Categories;
using namera_API.Models.Products.Enums;
using namera_API.Models.Products.Products;
using namera_API.Services.Customer;
using Xunit;

namespace namera_API.Tests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateAddressAsync_FirstAddress_BecomesDefault()
    {
        var fixture = await CreateFixtureAsync();

        var response = await fixture.Service.CreateAddressAsync(fixture.CustomerPrincipal, new CustomerAddressRequestDto
        {
            Label = "Home",
            RecipientName = "Mayar Sabta",
            PhoneNumber = "0590000001",
            AddressLine = "Main street",
            City = "Ramallah"
        });

        Assert.True(response.Success);
        Assert.True(response.Data!.IsDefault);
    }

    [Fact]
    public async Task CreateAddressAsync_NewDefault_ClearsPreviousDefault()
    {
        var fixture = await CreateFixtureAsync();
        var first = await fixture.Service.CreateAddressAsync(fixture.CustomerPrincipal, new CustomerAddressRequestDto
        {
            Label = "Home",
            RecipientName = "Mayar Sabta",
            PhoneNumber = "0590000001",
            AddressLine = "Main street",
            City = "Ramallah"
        });

        var second = await fixture.Service.CreateAddressAsync(fixture.CustomerPrincipal, new CustomerAddressRequestDto
        {
            Label = "Work",
            RecipientName = "Mayar Sabta",
            PhoneNumber = "0590000001",
            AddressLine = "Office street",
            City = "Nablus",
            IsDefault = true
        });

        var addresses = await fixture.Service.GetAddressesAsync(fixture.CustomerPrincipal);

        Assert.True(second.Success);
        Assert.False(addresses.Data!.Single(address => address.Id == first.Data!.Id).IsDefault);
        Assert.True(addresses.Data!.Single(address => address.Id == second.Data!.Id).IsDefault);
    }

    [Fact]
    public async Task SaveReviewAsync_SameProduct_UpdatesExistingReview()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext);

        var created = await fixture.Service.SaveReviewAsync(fixture.CustomerPrincipal, new CustomerReviewRequestDto
        {
            ProductId = product.Id,
            Rating = 4,
            Comment = "Lovely work"
        });

        var updated = await fixture.Service.SaveReviewAsync(fixture.CustomerPrincipal, new CustomerReviewRequestDto
        {
            ProductId = product.Id,
            Rating = 5,
            Comment = "Even better"
        });

        Assert.True(created.Success);
        Assert.True(updated.Success);
        Assert.Equal(created.Data!.Id, updated.Data!.Id);
        Assert.Equal(5, updated.Data.Rating);
        Assert.Equal(1, await fixture.DbContext.ProductReviews.CountAsync());
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCustomerCounters()
    {
        var fixture = await CreateFixtureAsync();
        var product = await SeedProductAsync(fixture.DbContext);

        fixture.DbContext.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "RB-TEST-1",
            CustomerId = fixture.CustomerId,
            CustomerName = "Mayar Sabta",
            CustomerPhoneNumber = "0590000001",
            ShippingAddress = "Ramallah",
            Status = OrderStatus.Completed,
            Subtotal = 50,
            Total = 50,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    CategoryName = product.Category.Name,
                    Quantity = 1,
                    UnitPrice = 50,
                    LineTotal = 50
                }
            ]
        });
        await fixture.DbContext.SaveChangesAsync();

        await fixture.Service.CreateAddressAsync(fixture.CustomerPrincipal, new CustomerAddressRequestDto
        {
            Label = "Home",
            RecipientName = "Mayar Sabta",
            PhoneNumber = "0590000001",
            AddressLine = "Main street",
            City = "Ramallah"
        });
        await fixture.Service.SaveReviewAsync(fixture.CustomerPrincipal, new CustomerReviewRequestDto
        {
            ProductId = product.Id,
            Rating = 5,
            Comment = "Perfect"
        });

        var dashboard = await fixture.Service.GetDashboardAsync(fixture.CustomerPrincipal);

        Assert.True(dashboard.Success);
        Assert.Equal(1, dashboard.Data!.TotalOrders);
        Assert.Equal(1, dashboard.Data.CompletedOrders);
        Assert.Equal(1, dashboard.Data.AddressesCount);
        Assert.Equal(1, dashboard.Data.ReviewsCount);
        Assert.Equal(50, dashboard.Data.TotalSpent);
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

        var service = new CustomerService(dbContext, userManager);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, savedCustomer!.Id.ToString())],
            "Test"));

        return new TestFixture(dbContext, service, principal, savedCustomer.Id);
    }

    private static async Task<Product> SeedProductAsync(AppDbContext dbContext)
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

        product.Images.Add(new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ImageUrl = "/uploads/products/primary.png",
            AltText = product.Name,
            IsPrimary = true,
            DisplayOrder = 1
        });

        dbContext.ProductCategories.Add(category);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return product;
    }

    private sealed record TestFixture(AppDbContext DbContext, CustomerService Service, ClaimsPrincipal CustomerPrincipal, Guid CustomerId);
}
