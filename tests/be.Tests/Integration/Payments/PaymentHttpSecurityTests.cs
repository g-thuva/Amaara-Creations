using be.Application.Abstractions.Email;
using be.Application.Abstractions.Payments;
using be.Application.Features.Notifications;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace be.Tests;
public sealed class PaymentHttpSecurityTests
{
    [Fact]
    public async Task SerializedWebhookSchemaOverridesGlobalBearerRequirement()
    {
        await using var factory = new AcceptanceApiFactory();
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(document.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        var webhook = document.GetProperty("paths").GetProperty("/api/v1/payments/datatrans/webhook").GetProperty("post");
        var requirements = webhook.GetProperty("security").EnumerateArray().ToArray();
        Assert.Single(requirements);
        Assert.Empty(requirements[0].EnumerateObject());
        Assert.Contains(webhook.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "Datatrans-Signature" && p.GetProperty("required").GetBoolean());
        Assert.True(webhook.GetProperty("requestBody").GetProperty("content").TryGetProperty("application/json", out _));
        var zahls = document.GetProperty("paths").GetProperty("/api/v1/payments/zahls/webhook").GetProperty("post");
        var zahlsRequirements = zahls.GetProperty("security").EnumerateArray().ToArray();
        Assert.Single(zahlsRequirements);
        Assert.Empty(zahlsRequirements[0].EnumerateObject());
        Assert.Contains(zahls.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "X-Webhook-Signature" && p.GetProperty("required").GetBoolean());
        Assert.True(zahls.GetProperty("requestBody").GetProperty("content").TryGetProperty("application/json", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignedCurrencyMismatchAndUnknownTransactionCannotMutatePayment(bool unknown)
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync();
        var payment = await fixture.StartAsync();
        var body = JsonSerializer.Serialize(new { transactionId = unknown ? "260000000000000099" : payment.ProviderTransactionId, merchantId = factory.Gateway.MerchantId, type = "payment", status = "settled", currency = unknown ? "LKR" : "CHF", refno = unknown ? "AC-unknown-disposable-transaction" : payment.ProviderReference, detail = new { authorize = new { amount = 22500 }, settle = new { amount = 22500 } } });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signature = Convert.ToHexString(HMACSHA256.HashData(CommerceApiFactory.HmacBytes, Encoding.UTF8.GetBytes(timestamp + body)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/datatrans/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Datatrans-Signature", "t=" + timestamp + ",s0=" + signature);
        Assert.Equal(unknown ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(PaymentStatus.Pending, (await fixture.Db.Payments.SingleAsync()).Status);
        Assert.Empty(fixture.Db.CommerceNotifications);
        Assert.Equal("Reserved", (await fixture.Db.InventoryReservations.SingleAsync()).Status);
        Assert.Single(fixture.Db.CartItems);
    }

    [Fact]
    public async Task ApiBlocksAnonymousOtherOwnersAndUnprivilegedRefunds()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId = 1 })).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-User", "a");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId = 2 })).StatusCode);
        var payment = await fixture.StartAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/admin/payments/{payment.Id}/refunds", new { amount = 10, reason = "forbidden", idempotencyKey = Guid.NewGuid() })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "b");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/payments/{payment.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/orders/{payment.OrderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/checkout/orders/{payment.OrderId}/retry", new { idempotencyKey = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task BrowserTotalsAreIgnoredAndPaymentRemainsPendingUntilVerified()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "a");
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync();
        var quoteResponse = await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId = 1, shippingMethodId = 1, subtotal = 1, total = 1, shippingPrice = -100, discountAmount = 99999, tax = -100, currency = "CHF" });
        quoteResponse.EnsureSuccessStatusCode();
        var quote = await quoteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(225m, quote.GetProperty("total").GetDecimal());
        Assert.Equal("LKR", quote.GetProperty("currency").GetString());
        var session = await client.PostAsJsonAsync("/api/v1/checkout/session", new { addressId = 1, shippingMethodId = 1, quoteVersion = quote.GetProperty("quoteVersion").GetString(), idempotencyKey = Guid.NewGuid(), subtotal = 1, total = 1, shipping = -100, discount = 99999, tax = -100, currency = "CHF", paymentStatus = "Paid" });
        session.EnsureSuccessStatusCode();
        var json = await session.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("paymentId").GetInt64();
        var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/payments/{id}?success=true&status=Paid");
        Assert.Equal("Pending", status.GetProperty("status").GetString());
        Assert.Equal(225m, status.GetProperty("amount").GetDecimal());
        Assert.Equal("LKR", status.GetProperty("currency").GetString());
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/api/v1/orders", new { total = 1 })).StatusCode);
    }

    [Fact]
    public async Task ChfReportingExcludesTestPaymentsAndHistoricalOtherCurrency()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Orders.Add(new Order { OrderNumber = "CHF-BUSINESS-FIXTURE", UserId = "fixture", Currency = "CHF", Total = 100m, Status = OrderStatus.Confirmed });
        db.Orders.AddRange(new Order { OrderNumber = "LKR-BUSINESS-FIXTURE", UserId = "fixture", Currency = "LKR", Total = 100m, Status = OrderStatus.Confirmed }, new Order { OrderNumber = "CHF-TEST-FIXTURE", UserId = "fixture", Currency = "CHF", Total = 50m, Status = OrderStatus.Confirmed, Payments = [new Payment { Provider = "Zahls", ProviderMode = "TEST", Method = "HostedRedirect", Currency = "CHF", Amount = 50m, Status = PaymentStatus.Paid }] });
        await db.SaveChangesAsync();
        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/dashboard");
        var revenue = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/dashboard/revenue");
        Assert.Equal("CHF", dashboard.GetProperty("currency").GetString());
        Assert.Equal("CHF", revenue.GetProperty("currency").GetString());
        Assert.Equal(100m, dashboard.GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(100m, revenue.GetProperty("totalRevenue").GetDecimal());
    }

    [Fact]
    public async Task SignedWebhookProcessesOnceAndRejectsAlteredPayloadAndWrongAmount()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync();
        var payment = await fixture.StartAsync();
        string Body(long amount) => JsonSerializer.Serialize(new { transactionId = payment.ProviderTransactionId, merchantId = factory.Gateway.MerchantId, type = "payment", status = "settled", currency = "LKR", refno = payment.ProviderReference, detail = new { authorize = new { amount }, settle = new { amount } } });
        async Task<HttpResponseMessage> Post(string body, bool signed = true, bool tampered = false)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var signature = Convert.ToHexString(HMACSHA256.HashData(CommerceApiFactory.HmacBytes, Encoding.UTF8.GetBytes(timestamp + body)));
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/datatrans/webhook")
            {
                Content = new StringContent(tampered ? body + " " : body, Encoding.UTF8, "application/json")
            };
            if (signed)
                request.Headers.Add("Datatrans-Signature", "t=" + timestamp + ",s0=" + signature);
            return await client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(Body(22500), signed: false)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(Body(22500), tampered: true)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(Body(1))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(Body(22500))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(Body(22500))).StatusCode);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(PaymentStatus.Paid, (await fixture.Db.Payments.SingleAsync()).Status);
        Assert.Single(fixture.Db.CommerceNotifications);
        Assert.Equal(8, (await fixture.Db.Products.SingleAsync()).Stock);
    }
}
