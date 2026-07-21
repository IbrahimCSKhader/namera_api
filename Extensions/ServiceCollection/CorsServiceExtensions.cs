namespace namera_API.Extensions.ServiceCollection;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? DefaultAllowedOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy("ReactFrontend", policy =>
            {
                if (allowedOrigins.Contains("*", StringComparer.Ordinal))
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins);
                }

                policy.AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static readonly string[] DefaultAllowedOrigins =
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "https://namera-front.onrender.com",
        "https://resinbonn.runasp.net",
        "http://resinbonn.runasp.net"
    ];
}
