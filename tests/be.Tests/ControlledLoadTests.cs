using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using be.Data;
using be.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace be.Tests;

public sealed class ControlledLoadTests(ITestOutputHelper output)
{
    [Fact]
    public Task EightWorkersExerciseCatalogQuoteCartAndAdminWithoutProviderNetwork() => Exercise(null);

    [SqlCommerceFact]
    public Task SqlBackedEightWorkerReadAndQuoteRehearsal()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_Release789_Load_" + Guid.NewGuid().ToString("N")[..8];
        return Exercise(connection.ConnectionString);
    }

    private async Task Exercise(string? connection)
    {
        await using var factory = new CommerceApiFactory(connection); using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "a"); client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User { Id = "a", Name = "Load fixture", UserName = "load@example.test" };
        var product = new Product { Name = "Load test product", Slug = "load-test-product", Stock = 100, Price = 100, BasePrice = 100 };
        var address = new Address { User = user, RecipientName = "TEST recipient", AddressLine1 = "TEST street", City = "TEST city", Country = "Fixtureland", Phone = "+000000000" };
        var zone = new ShippingZone { Name = "TEST load zone", Country = "Fixtureland" };
        var method = new ShippingMethod { ShippingZone = zone, Name = "TEST load delivery", Code = "TEST_LOAD", Currency = "LKR", BasePrice = 0 };
        db.AddRange(user, product, address, zone, method); await db.SaveChangesAsync();
        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 1 });
        var order = new Order { UserId = user.Id, OrderNumber = "TEST-LOAD-ORDER", Currency = "LKR", Total = 100, Subtotal = 100, OrderItems = [new OrderItem { ProductId = product.Id, ProductNameSnapshot = product.Name, Quantity = 1, Price = 100, UnitPrice = 100, Subtotal = 100 }] };
        db.Orders.Add(order); await db.SaveChangesAsync();
        var routes = new[] { "/api/v1/products", $"/api/v1/products/{product.Id}", "/api/v1/cart", "/api/v1/orders", "/api/v1/admin/orders", "/api/v1/admin/custom-designs", "/api/v1/admin/inventory", "/api/v1/admin/reports" };
        // Warm up translation and auth before recording the controlled workload.
        foreach (var route in routes) (await client.GetAsync(route)).EnsureSuccessStatusCode();
        var failures = new ConcurrentBag<string>(); var timings = new ConcurrentBag<double>(); var watch = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async worker =>
        {
            for (var iteration = 0; iteration < 10; iteration++)
            {
                foreach (var route in routes)
                {
                    var started = Stopwatch.GetTimestamp(); using var response = await client.GetAsync(route); timings.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                    if (!response.IsSuccessStatusCode) failures.Add($"GET {route}: {(int)response.StatusCode}");
                }
                var quoteStart = Stopwatch.GetTimestamp(); using var quote = await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId = address.Id, shippingMethodId = method.Id }); timings.Add(Stopwatch.GetElapsedTime(quoteStart).TotalMilliseconds);
                if (!quote.IsSuccessStatusCode) failures.Add($"quote: {(int)quote.StatusCode}");
            }
        }));
        watch.Stop(); Assert.Empty(failures); Assert.Equal(720, timings.Count); Assert.Equal(0, factory.Gateway.Initializations); Assert.Equal(0, factory.Gateway.RefundCalls);
        db.ChangeTracker.Clear(); Assert.Equal(100, (await db.Products.SingleAsync()).Stock); Assert.Equal(1, await db.Orders.CountAsync()); Assert.Empty(await db.Payments.ToListAsync());
        var sorted = timings.Order().ToArray(); var p95 = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1];
        Assert.True(p95 < 5000, $"Controlled test-host p95 exceeded the generous 5-second local regression budget: {p95:F1}ms");
        var evidence = new { status = "PASS", environment = connection == null ? "in-memory HTTP TestServer" : "SQL Server HTTP TestServer", workers = 8, requests = timings.Count, failures = failures.Count, elapsedSeconds = watch.Elapsed.TotalSeconds, p95Ms = p95, providerNetworkCalls = 0, productionCapacityGuarantee = false };
        var json = JsonSerializer.Serialize(evidence); output.WriteLine(json);
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, connection == null ? "release789-load-memory.json" : "release789-load-sql.json"), json);
    }
}
