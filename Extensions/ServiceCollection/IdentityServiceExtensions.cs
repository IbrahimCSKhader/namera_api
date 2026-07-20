using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using namera_API.Data;
using namera_API.Models.Identity;
using namera_API.Services.Authentication;
using namera_API.Services.Customer;
using namera_API.Services.Orders;
using namera_API.Services.Products;
using namera_API.Services.Token;

namespace namera_API.Extensions.ServiceCollection;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductManagementService, ProductManagementService>();
        services.AddScoped<IProductMediaStorageService, ProductMediaStorageService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
