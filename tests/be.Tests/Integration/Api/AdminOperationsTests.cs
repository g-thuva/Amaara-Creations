using be.Application.Features.Reporting;
using be.Application.Features.Promotions;
using be.Api.Controllers;
using be.Application.Features.Inventory;
using be.Domain.Audit;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Inventory;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Infrastructure.Observability;
using be.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class AdminOperationsTests
{
    [Theory]
    [InlineData("CustomerSupport", "POST", "payments/1/refunds")]
    [InlineData("CustomerSupport", "POST", "payments/1/reconcile")]
    [InlineData("CustomerSupport", "POST", "inventory/1/adjustments")]
    [InlineData("CustomerSupport", "GET", "reports")]
    [InlineData("CustomerSupport", "GET", "dashboard/revenue")]
    [InlineData("CustomerSupport", "GET", "staff")]
    [InlineData("CustomerSupport", "GET", "settings")]
    [InlineData("CustomerSupport", "GET", "audit")]
    [InlineData("Marketing", "GET", "customers")]
    [InlineData("Marketing", "GET", "payments")]
    [InlineData("CatalogManager", "GET", "customers")]
    [InlineData("ProductionStaff", "GET", "payments")]
    [InlineData("Analyst", "POST", "inventory/1/adjustments")]
    [InlineData("Analyst", "POST", "promotions")]
    [InlineData("Analyst", "PUT", "orders/1/status")]
    [InlineData("Admin", "GET", "staff")]
    [InlineData("Admin", "GET", "settings")]
    public async Task SensitiveEndpointsEnforceDistinctStaffPermissions(string role, string method, string path)
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "staff");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/admin/" + path)
        {
            Content = JsonContent.Create(new { })
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(0, factory.Gateway.RefundCalls);
    }

    [Fact]
    public async Task SupportCanReadOrdersButCannotChangeOrderState()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Orders.Add(new Order { OrderNumber = "SUPPORT-FIXTURE", UserId = "fixture", Status = OrderStatus.Pending });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "support");
        client.DefaultRequestHeaders.Add("X-Test-Role", "CustomerSupport");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/v1/admin/orders/1/status", new { status = "Processing" })).StatusCode);
    }

    [Fact]
    public async Task InventoryAdjustmentRecordsDeltaAndRejectsStaleNegativeAndCrossProductRequests()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var p = new Product
        {
            Name = "Stock fixture",
            Slug = "stock-fixture",
            Stock = 5
        };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        async Task<HttpStatusCode> Adjust(int expected, int delta, int? variant = null, string reason = "Count correction") => (await client.PostAsJsonAsync($"/api/v1/admin/inventory/{p.Id}/adjustments", new InventoryAdjustment(variant, expected, delta, reason, 2))).StatusCode;
        Assert.Equal(HttpStatusCode.NoContent, await Adjust(5, -2));
        Assert.Equal(HttpStatusCode.Conflict, await Adjust(5, 1));
        Assert.Equal(HttpStatusCode.Conflict, await Adjust(3, -4));
        Assert.Equal(HttpStatusCode.NotFound, await Adjust(3, 1, 999));
        Assert.Equal(HttpStatusCode.BadRequest, await Adjust(3, 1, reason: " "));
        db.ChangeTracker.Clear();
        Assert.Equal(3, (await db.Products.SingleAsync()).Stock);
        var movement = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(-2, movement.QuantityDelta);
        Assert.Equal(5, movement.PreviousQuantity);
        Assert.Equal(3, movement.NewQuantity);
        Assert.Equal("admin", movement.PerformedByUserId);
        Assert.Single(await db.AuditLogs.Where(a => a.Action == "InventoryAdjusted").ToListAsync());
    }

    [Fact]
    public async Task ReservedGoodsAreIncludedInOnHandButUnavailableToManualSubtraction()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync();
        await fixture.StartAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var data = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/inventory");
        var stock = data.GetProperty("items")[0];
        Assert.Equal(10, stock.GetProperty("onHand").GetInt32());
        Assert.Equal(8, stock.GetProperty("available").GetInt32());
        Assert.Equal(2, stock.GetProperty("reserved").GetInt32());
        var movement = await fixture.Db.InventoryTransactions.SingleAsync();
        Assert.Equal("Reserved", movement.MovementType);
        Assert.Equal(-2, movement.QuantityDelta);
    }

    [Fact]
    public async Task AuditAndLedgerEvidenceCannotBeEditedOrDeleted()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var log = new AuditLog
        {
            Action = "Test",
            EntityType = "Fixture"
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
        log.Action = "Rewritten";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var loaded = await db.AuditLogs.SingleAsync();
        db.Remove(loaded);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var movement = new InventoryTransaction
        {
            Reason = "Fixture"
        };
        db.Add(movement);
        await db.SaveChangesAsync();
        db.Remove(movement);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void SafeAuditMetadataRecursivelyRedactsSecrets()
    {
        var json = AuditService.SafeJson(new { Name = "Safe", ApiKey = "must-not-appear", Nested = new[] { new { Password = "also-private", Quantity = 3 } } });
        Assert.DoesNotContain("must-not-appear", json);
        Assert.DoesNotContain("also-private", json);
        Assert.Contains("Safe", json);
        Assert.Contains("Quantity", json);
    }

    [Theory]
    [InlineData("=SUM(A1:A2)")]
    [InlineData("+cmd")]
    [InlineData("-1+2")]
    [InlineData("@SUM(A1)")]
    [InlineData(" \t=1")]
    public void CsvExportNeutralizesSpreadsheetFormulaInjection(string value) => Assert.StartsWith("\"'", ReportingQueries.CsvCell(value));
    [Fact]
    public void CsvExportQuotesCommasLineBreaksAndDoubleQuotes() => Assert.Equal("\"a,\"\"b\"\"\nline\"", ReportingQueries.CsvCell("a,\"b\"\nline"));
    [Fact]
    public async Task ReportsKeepCurrenciesAndTestMoneySeparateAndExportNoCustomerData()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var currency in new[]
        {
            "LKR",
            "CHF"
        }

        )
            db.Orders.Add(new Order { OrderNumber = currency + "-REPORT", Currency = currency, Total = 100, UserId = "private-customer", Payments = [new Payment { Amount = 100, SettledAmount = 100, Currency = currency, Provider = "Fixture", ProviderMode = currency == "CHF" ? "TEST" : "LIVE", Status = PaymentStatus.Paid }] });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "analyst");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Analyst");
        var report = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/reports");
        var money = report.GetProperty("money");
        Assert.Equal(2, money.GetArrayLength());
        Assert.All(money.EnumerateArray(), r => Assert.Equal(100, r.GetProperty("settled").GetDecimal()));
        var csv = await client.GetStringAsync("/api/v1/admin/reports/orders.csv");
        Assert.Contains("currency", csv);
        Assert.DoesNotContain("private-customer", csv);
        Assert.Contains("CHF-REPORT", csv);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/admin/reports?from=2026-10-16&to=2026-10-01")).StatusCode);
    }

    [Fact]
    public async Task PromotionsNormalizeCodesValidateAndRetainRedemptionHistory()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "marketing");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Marketing");
        var request = new PromotionRequest("test-code", "Test promotion", "Percentage", 10, "CHF", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddDays(1), 10, 20, 5, 1, true, true, true, null);
        var response = await client.PostAsJsonAsync("/api/v1/admin/promotions", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TEST-CODE", saved.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/admin/promotions", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/admin/promotions", request with { Code = "BAD", DiscountValue = 110 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/admin/promotions/{saved.GetProperty("id").GetInt32()}/redemptions")).StatusCode);
    }
}
