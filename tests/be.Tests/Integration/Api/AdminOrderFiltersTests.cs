using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class AdminOrderFiltersTests
{
    [Fact]
    public async Task DateSearchAndSortStayServerFilteredAndRejectReversedRanges()
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User { Id = "filter-customer", Name = "Filter customer", UserName = "filters@example.test", Email = "filters@example.test" };
        db.Orders.AddRange(
            new Order { User = user, OrderNumber = "FILTER-B", CreatedAt = new DateTime(2026, 10, 1, 23, 59, 59, DateTimeKind.Utc), Total = 20, Currency = "CHF" },
            new Order { User = user, OrderNumber = "FILTER-A", CreatedAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), Total = 30, Currency = "CHF" },
            new Order { User = user, OrderNumber = "FILTER-C", CreatedAt = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc), Total = 40, Currency = "CHF" });
        await db.SaveChangesAsync(); client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var day = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/orders?from=2026-10-01&to=2026-10-01");
        Assert.Equal(1, day.GetProperty("totalCount").GetInt32()); Assert.Equal("FILTER-B", day.GetProperty("orders")[0].GetProperty("orderNumber").GetString());
        var sorted = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/orders?search=Filter%20customer&sort=number&pageSize=2");
        Assert.Equal(3, sorted.GetProperty("totalCount").GetInt32()); Assert.Equal(2, sorted.GetProperty("orders").GetArrayLength()); Assert.Equal("FILTER-A", sorted.GetProperty("orders")[0].GetProperty("orderNumber").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/admin/orders?from=2026-10-03&to=2026-10-01")).StatusCode);
    }
}
