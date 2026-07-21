using Microsoft.EntityFrameworkCore;
using namera_API.Common.Responses;
using namera_API.Data;
using namera_API.Data.Seed;
using namera_API.Extensions.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCorsServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddApplicationServices();

Directory.CreateDirectory(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"));

var app = builder.Build();

// Keep Swagger available while the hosted backend is being tested.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("ReactFrontend");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("UnhandledRequestException");

        logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
            "حدث خطأ في الخادم أثناء معالجة الطلب. راجع السجل أو تأكد من تحديث قاعدة البيانات."));
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseStartup");

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        await ProductSeeder.SeedAsync(scope.ServiceProvider);

        logger.LogInformation("Database migration and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration or seeding failed.");
    }
}

await app.RunAsync();
