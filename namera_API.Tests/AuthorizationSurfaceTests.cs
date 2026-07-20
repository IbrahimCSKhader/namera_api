using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using namera_API.Constants.Identity;
using Xunit;

namespace namera_API.Tests;

public sealed class AuthorizationSurfaceTests
{
    [Fact]
    public void ControllerAuthorizationSurface_MatchesExpectedRoles()
    {
        var controllers = typeof(namera_API.Controllers.Authentication.AuthController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            var route = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            var authorize = controller.GetCustomAttribute<AuthorizeAttribute>();

            if (route.StartsWith("api/admin", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotNull(authorize);
                Assert.Equal(AppRoles.Owner, authorize!.Roles);
                continue;
            }

            if (route.StartsWith("api/customer", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotNull(authorize);
                Assert.Equal(AppRoles.Customer, authorize!.Roles);
                continue;
            }

            if (route.StartsWith("api/auth", StringComparison.OrdinalIgnoreCase) ||
                route.StartsWith("api/products", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Null(authorize);
            }
        }
    }

    [Fact]
    public void HttpRoutes_AreUniqueWithinControllerSurface()
    {
        var routes = typeof(namera_API.Controllers.Authentication.AuthController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(controller =>
            {
                var controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? controller.Name;
                return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>(), (method, attribute) =>
                    {
                        var actionRoute = attribute.Template ?? string.Empty;
                        var route = string.Join("/", new[] { controllerRoute, actionRoute }.Where(part => !string.IsNullOrWhiteSpace(part)));
                        return $"{string.Join(",", attribute.HttpMethods.OrderBy(item => item))} {route}";
                    });
            })
            .ToList();

        var duplicates = routes
            .GroupBy(route => route)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
        Assert.True(routes.Count >= 33);
    }
}
