namespace namera_API.Extensions.ServiceCollection;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ReactFrontend", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
