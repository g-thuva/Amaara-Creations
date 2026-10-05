using System.Reflection;
using be.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace be.Tests;

public class ApiRouteContractTests
{
    [Fact]
    public void Controllers_UseExplicitCanonicalVersionedRoutes()
    {
        var routes = GetControllerRoutes();

        Assert.DoesNotContain(routes, r => r.Template.Contains("[controller]", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, r => r.Template.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
            && !r.Template.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, r => r.Template.Contains("/refresh-token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, r => r.Template.Contains("orders/admin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, r => r.Template.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, r => r.Template.Equals("api/v1/upload/product-image", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CanonicalRoutes_AreRegisteredOnceForKeyWorkflows()
    {
        var routes = GetControllerRoutes();

        AssertRoute(routes, "POST", "api/v1/auth/login");
        AssertRoute(routes, "POST", "api/v1/auth/refresh");
        AssertRoute(routes, "GET", "api/v1/auth/me");

        AssertRoute(routes, "GET", "api/v1/addresses");
        AssertRoute(routes, "PUT", "api/v1/addresses/{id:int}/default");
        AssertNoRoute(routes, "POST", "api/v1/addresses/{id:int}/default");

        AssertRoute(routes, "GET", "api/v1/products");
        AssertNoRoute(routes, "POST", "api/v1/products");
        AssertNoRoute(routes, "PUT", "api/v1/products/{id}");
        AssertNoRoute(routes, "DELETE", "api/v1/products/{id}");

        AssertRoute(routes, "GET", "api/v1/cart");
        AssertRoute(routes, "GET", "api/v1/wishlist");
        AssertRoute(routes, "GET", "api/v1/orders");

        AssertRoute(routes, "GET", "api/v1/admin/dashboard");
        AssertRoute(routes, "GET", "api/v1/admin/customers");
        AssertRoute(routes, "GET", "api/v1/admin/orders");
        AssertRoute(routes, "GET", "api/v1/admin/orders/{id:int}");
        AssertRoute(routes, "PUT", "api/v1/admin/orders/{id:int}/status");

        AssertRoute(routes, "GET", "api/v1/products/{productId:int}/reviews");
        AssertRoute(routes, "POST", "api/v1/products/{productId:int}/reviews");
        AssertRoute(routes, "PUT", "api/v1/reviews/{id:int}");
        AssertRoute(routes, "DELETE", "api/v1/reviews/{id:int}");
        AssertRoute(routes, "GET", "api/v1/admin/reviews");
        AssertRoute(routes, "GET", "api/v1/admin/reviews/stats");
    }

    private static List<RouteEntry> GetControllerRoutes()
    {
        return typeof(AuthController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(GetRoutesForController)
            .ToList();
    }

    private static IEnumerable<RouteEntry> GetRoutesForController(Type controllerType)
    {
        var controllerRoutes = controllerType
            .GetCustomAttributes<RouteAttribute>()
            .Select(a => Normalize(a.Template ?? string.Empty))
            .ToArray();

        foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            foreach (var httpAttribute in method.GetCustomAttributes().OfType<HttpMethodAttribute>())
            {
                var rawActionTemplate = httpAttribute.Template ?? string.Empty;
                var actionTemplate = Normalize(rawActionTemplate);
                var fullTemplate = rawActionTemplate.TrimStart().StartsWith("/", StringComparison.Ordinal)
                    ? actionTemplate
                    : Combine(controllerRoutes, actionTemplate);

                foreach (var httpMethod in httpAttribute.HttpMethods)
                {
                    yield return new RouteEntry(httpMethod.ToUpperInvariant(), fullTemplate);
                }
            }
        }
    }

    private static string Combine(string[] controllerRoutes, string actionTemplate)
    {
        var controllerRoute = controllerRoutes.SingleOrDefault()
            ?? throw new InvalidOperationException("Controller must have exactly one canonical route.");

        return Normalize(string.IsNullOrWhiteSpace(actionTemplate)
            ? controllerRoute
            : $"{controllerRoute}/{actionTemplate}");
    }

    private static string Normalize(string value)
    {
        return value.Trim().Trim('/').Replace("\\", "/", StringComparison.Ordinal);
    }

    private static void AssertRoute(List<RouteEntry> routes, string method, string template)
    {
        Assert.Contains(routes, r => r.Method == method && r.Template == template);
        Assert.Single(routes, r => r.Method == method && r.Template == template);
    }

    private static void AssertNoRoute(List<RouteEntry> routes, string method, string template)
    {
        Assert.DoesNotContain(routes, r => r.Method == method && r.Template == template);
    }

    private sealed record RouteEntry(string Method, string Template);
}
