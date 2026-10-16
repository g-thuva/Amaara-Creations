using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class AccountStatisticsTests
{
    [Fact]
    public Task AccountTotalsSeparateCurrenciesAndExcludeCancelledAndOtherCustomersOrders() => Verify(null);

    [SqlCommerceFact]
    public Task SqlAccountTotalsSeparateCurrenciesAndKeepHistoryAmountsLabelled()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_AccountCurrency_" + Guid.NewGuid().ToString("N")[..8];
        return Verify(connection.ConnectionString);
    }

    private static async Task Verify(string? connection)
    {
        await using var factory = new CommerceApiFactory(connection);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = new User { Id = "a", UserName = "currency-owner@example.test", Email = "currency-owner@example.test", Name = "Currency owner" };
        var other = new User { Id = "b", UserName = "other-owner@example.test", Email = "other-owner@example.test", Name = "Other owner" };
        var lkr = new Product { Name = "Historical LKR item", Slug = "currency-lkr", Currency = "LKR", Price = 100m, BasePrice = 100m };
        var chf = new Product { Name = "CHF item", Slug = "currency-chf", Currency = "CHF", Price = 25m, BasePrice = 25m };
        Order Order(string number, User user, Product product, decimal total, OrderStatus status = OrderStatus.Completed) => new()
        {
            User = user, OrderNumber = number, Currency = product.Currency, Total = total, Subtotal = total, Status = status,
            OrderItems = [new OrderItem { Product = product, ProductNameSnapshot = product.Name, Price = total, UnitPrice = total, Subtotal = total, Quantity = 1 }]
        };
        db.AddRange(Order("OWNER-LKR", owner, lkr, 100m), Order("OWNER-CHF", owner, chf, 25m),
            Order("OWNER-CANCELLED", owner, chf, 900m, OrderStatus.Cancelled), Order("OTHER-LKR", other, lkr, 400m));
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", owner.Id);
        var stats = await client.GetFromJsonAsync<JsonElement>("/api/v1/users/profile/stats");
        Assert.Equal(100m, stats.GetProperty("totalSpent").GetDecimal());
        Assert.Equal("LKR", stats.GetProperty("currency").GetString());
        Assert.Equal(3, stats.GetProperty("totalOrders").GetInt32());
        var totals = stats.GetProperty("orderTotalsByCurrency");
        Assert.Equal(100m, totals.GetProperty("LKR").GetDecimal());
        Assert.Equal(25m, totals.GetProperty("CHF").GetDecimal());
        Assert.Equal(2, totals.EnumerateObject().Count());

        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{owner.Id}/orders");
        Assert.All(history.GetProperty("orders").EnumerateArray(), order =>
            Assert.Contains(order.GetProperty("currency").GetString(), new[] { "LKR", "CHF" }));
    }
}
