using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Data;
using be.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class ReleaseSecurityAndSeoTests
{
    [Fact]
    public async Task PrivateResponsesHaveNoStoreAndSafeHeadersAndErrorsShareCorrelationId()
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "admin"); client.DefaultRequestHeaders.Add("X-Correlation-ID", "release-test-correlation");
        var response = await client.GetAsync("/api/v1/admin/reports?from=2026-10-16&to=2026-10-01"); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore); Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("release-test-correlation", Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("release-test-correlation", error.GetProperty("correlationId").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task UnconfiguredCanonicalHostDisablesIndexingWithoutInventingDomain()
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient();
        Assert.Equal("User-agent: *\nDisallow: /\n", await client.GetStringAsync("/api/v1/seo/robots.txt"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/v1/seo/sitemap.xml")).StatusCode);
    }

    [Fact]
    public async Task SitemapIncludesOnlyActivePublicProductsAndNoPrivateRoutes()
    {
        await using var factory = new AcceptanceApiFactory(); await factory.SeedCatalogueAsync(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var first = await db.Products.OrderBy(p => p.Id).FirstAsync(); first.IsActive = false; await db.SaveChangesAsync(); using var client = factory.CreateClient();
        var xml = await client.GetStringAsync("/api/v1/seo/sitemap.xml"); Assert.DoesNotContain($"/products/{first.Id}</loc>", xml); Assert.Contains("/products/13</loc>", xml);
        foreach (var path in new[] { "/admin", "/account", "/checkout", "/custom-designs", "/payments" }) Assert.DoesNotContain(path, xml);
        var robots = await client.GetStringAsync("/api/v1/seo/robots.txt"); Assert.Contains("Disallow: /api/", robots); Assert.Contains("Disallow: /Amaara-Creations/admin", robots);
    }

    [Fact]
    public async Task CatalogCreatesExplicitChfPricesWithoutRelabelingExistingCurrencyOrReplacingStock()
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var response = await client.PostAsJsonAsync("/api/v1/admin/products", new { name = "TEST explicit CHF product", description = "Fixture only", basePrice = 25.25, currency = "CHF", stock = 2 }); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = saved.GetProperty("id").GetInt32(); Assert.Equal("CHF", saved.GetProperty("currency").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/admin/products/{id}", new { name = "TEST explicit CHF product", description = "Fixture only", basePrice = 25.25, currency = "LKR", stock = 2, rowVersion = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/admin/products/{id}", new { name = "TEST explicit CHF product", description = "Fixture only", basePrice = 25.25, currency = "CHF", stock = 100, rowVersion = "" })).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var product = await db.Products.SingleAsync(); Assert.Equal("CHF", product.Currency); Assert.Equal(25.25m, product.Price); Assert.Equal(2, product.Stock);
        Assert.Equal("Opening", (await db.InventoryTransactions.SingleAsync()).MovementType);
    }
}
