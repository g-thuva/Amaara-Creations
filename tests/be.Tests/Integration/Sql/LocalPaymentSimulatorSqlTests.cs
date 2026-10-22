using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Application.Abstractions.Payments;
using be.Application.Features.Payments;
using be.Domain.Payments;
using be.Infrastructure.Persistence;
using be.Tests.Simulation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class LocalPaymentSimulatorSqlTests
{
    [SqlCommerceFact]
    public async Task BothCurrenciesUseRealSqlCheckoutReplayRetryReconciliationInventoryAndRefundWorkflows()
    {
        var server = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!).DataSource;
        foreach (var currency in new[] { "LKR", "CHF" })
        {
            var lab = await SimulatorLab.CreateAsync(server, currency, Path.Combine(Path.GetTempPath(), "AmaaraSimulatorVerification"));
            try { await Journey(lab, currency); }
            finally
            {
                await lab.DisposeAsync(); SimulatorSafety.CheckConnection(lab.Connection);
                await using var cleanup = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(lab.Connection).Options);
                await cleanup.Database.EnsureDeletedAsync();
            }
        }
    }

    private static async Task Journey(SimulatorLab lab, string currency)
    {
        using var signed = await lab.SignInAsync(false); signed.EnsureSuccessStatusCode();
        using var client = lab.Factory.CreateClient(new() { HandleCookies = false });
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await signed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        int productId, addressId, shippingId;
        using (var scope = lab.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            productId = await db.Products.MinAsync(p => p.Id); addressId = await db.Addresses.Where(a => a.UserId == lab.CustomerId).Select(a => a.Id).SingleAsync();
            shippingId = await db.ShippingMethods.Where(m => m.Code == "SIMULATED").Select(m => m.Id).SingleAsync();
        }
        (await client.PostAsJsonAsync("/api/v1/cart", new { productId, quantity = 2 })).EnsureSuccessStatusCode();
        var quoteResponse = await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId, shippingMethodId = shippingId }); quoteResponse.EnsureSuccessStatusCode();
        var quote = await quoteResponse.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(227m, quote.GetProperty("total").GetDecimal()); Assert.True(quote.GetProperty("canPay").GetBoolean());
        var request = new { addressId, shippingMethodId = shippingId, quoteVersion = quote.GetProperty("quoteVersion").GetString(), idempotencyKey = Guid.NewGuid(), amount = 1, total = 1 };
        var started = await client.PostAsJsonAsync("/api/v1/checkout/session", request); started.EnsureSuccessStatusCode(); var first = await started.Content.ReadFromJsonAsync<JsonElement>();
        var duplicate = await client.PostAsJsonAsync("/api/v1/checkout/session", request); duplicate.EnsureSuccessStatusCode(); Assert.Equal(first.GetProperty("paymentId").GetInt64(), (await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetInt64()); Assert.Equal(1, lab.Gateway.Initializations);
        var orderId = first.GetProperty("orderId").GetInt32(); var paymentId = first.GetProperty("paymentId").GetInt64();
        await lab.EventAsync(paymentId, "pending", Guid.NewGuid(), true);
        await AssertSql(lab, paymentId, PaymentStatus.Pending, 98, "Reserved");
        await lab.EventAsync(paymentId, "decline", Guid.NewGuid(), true); await AssertSql(lab, paymentId, PaymentStatus.Failed, 100, "Released");
        async Task<long> Retry()
        {
            var key = Guid.NewGuid(); var response = await client.PostAsJsonAsync($"/api/v1/checkout/orders/{orderId}/retry", new { idempotencyKey = key }); response.EnsureSuccessStatusCode();
            var value = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(orderId, value.GetProperty("orderId").GetInt32());
            var replay = await client.PostAsJsonAsync($"/api/v1/checkout/orders/{orderId}/retry", new { idempotencyKey = key }); replay.EnsureSuccessStatusCode(); Assert.Equal(value.GetProperty("paymentId").GetInt64(), (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetInt64());
            return value.GetProperty("paymentId").GetInt64();
        }
        paymentId = await Retry(); await lab.EventAsync(paymentId, "cancel", Guid.NewGuid(), true); await AssertSql(lab, paymentId, PaymentStatus.Cancelled, 100, "Released");
        paymentId = await Retry(); await lab.EventAsync(paymentId, "success", Guid.NewGuid(), false); await AssertSql(lab, paymentId, PaymentStatus.Pending, 98, "Reserved");
        using (var reconciliation = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/reconcile", new { }, default)) reconciliation.EnsureSuccessStatusCode();
        await AssertSql(lab, paymentId, PaymentStatus.Paid, 98, "Consumed");
        var eventId = Guid.NewGuid(); await lab.EventAsync(paymentId, "success", eventId, true); await lab.EventAsync(paymentId, "success", eventId, true);
        var conflict = await Assert.ThrowsAsync<be.Domain.Common.CommerceException>(() => lab.EventAsync(paymentId, "decline", eventId, true)); Assert.Equal("IDEMPOTENCY_CONFLICT", conflict.Code);
        await AssertSql(lab, paymentId, PaymentStatus.Paid, 98, "Consumed");
        var refundKey = Guid.NewGuid(); var body = new RefundRequest(100, "SIMULATED SQL verification", refundKey);
        using var refund = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/refunds", body, default); refund.EnsureSuccessStatusCode();
        using var refundReplay = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/refunds", body, default); refundReplay.EnsureSuccessStatusCode(); Assert.Equal(1, lab.Gateway.RefundCalls);
        using var excessive = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/refunds", new RefundRequest(1000, "SIMULATED excessive refund", Guid.NewGuid()), default); Assert.Equal(HttpStatusCode.Conflict, excessive.StatusCode);
        using var verification = lab.Factory.Services.CreateScope(); var database = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await database.Orders.CountAsync()); Assert.Equal(3, await database.Payments.CountAsync()); Assert.Equal(3, await database.CheckoutAttempts.CountAsync()); Assert.Equal(1, await database.Refunds.CountAsync()); Assert.Equal(4, await database.PaymentWebhookEvents.CountAsync());
        Assert.False(await database.CartItems.AnyAsync()); Assert.All(await database.Payments.ToListAsync(), payment => { Assert.Equal(currency, payment.Currency); Assert.Equal(227m, payment.Amount); Assert.Equal(SimulatorSafety.Provider, payment.Provider); Assert.Equal(SimulatorSafety.Mode, payment.ProviderMode); });
        var final = await database.Payments.SingleAsync(p => p.Id == paymentId); Assert.Equal(PaymentStatus.PartiallyRefunded, final.Status); Assert.Equal(100, final.RefundedAmount);
        var declinedKey = Guid.NewGuid(); lab.Gateway.RefundOutcomes[declinedKey] = GatewayState.Failed;
        using var declined = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/refunds", new RefundRequest(10, "SIMULATED declined credit", declinedKey), default);
        declined.EnsureSuccessStatusCode(); Assert.Equal("Failed", (await declined.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        using var remaining = await lab.AdminActionAsync($"/api/v1/admin/payments/{paymentId}/refunds", new RefundRequest(127, "SIMULATED remaining credit", Guid.NewGuid()), default);
        remaining.EnsureSuccessStatusCode(); database.ChangeTracker.Clear(); final = await database.Payments.SingleAsync(p => p.Id == paymentId);
        Assert.Equal(PaymentStatus.Refunded, final.Status); Assert.Equal(227, final.RefundedAmount);
        Assert.Equal(98, await database.Products.OrderBy(p => p.Id).Select(p => p.Stock).FirstAsync());
    }

    private static async Task AssertSql(SimulatorLab lab, long id, PaymentStatus expected, int stock, string reservation)
    {
        using var scope = lab.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var payment = await db.Payments.SingleAsync(p => p.Id == id);
        Assert.Equal(expected, payment.Status); Assert.Equal(stock, await db.Products.OrderBy(p => p.Id).Select(p => p.Stock).FirstAsync());
        Assert.Contains(await db.InventoryReservations.Where(r => r.OrderId == payment.OrderId).ToListAsync(), row => row.Status == reservation);
    }
}
