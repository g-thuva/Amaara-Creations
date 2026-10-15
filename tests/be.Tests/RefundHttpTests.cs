using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Data;
using be.DTOs.Checkout;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class RefundHttpTests
{
    [Fact]
    public void RefundRequestMetadataCanBeValidatedByMvc()
    {
        using var factory = new CommerceApiFactory();
        using var scope = factory.Services.CreateScope();
        var context = new ActionContext(new DefaultHttpContext { RequestServices = scope.ServiceProvider }, new RouteData(), new ActionDescriptor());
        scope.ServiceProvider.GetRequiredService<IObjectModelValidator>().Validate(context, null, "", new RefundRequest(50, "Test-only reason", Guid.NewGuid()));
        Assert.True(context.ModelState.IsValid);
    }

    [Theory]
    [InlineData("Completed", 50, "PartiallyRefunded", 50, 175)]
    [InlineData("Completed", 225, "Refunded", 225, 0)]
    [InlineData("Pending", 50, "Paid", 0, 175)]
    [InlineData("Failed", 50, "Paid", 0, 225)]
    public async Task RefundResultAndFreshDetailReadsAgree(string outcome, int amount, string paymentStatus, int refunded, int available)
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await using var fixture = new CommerceFixture(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), factory.Gateway);
        await fixture.SeedAsync(); var payment = await fixture.StartAsync();
        await fixture.Payments.ApplyAsync(fixture.State(payment), null, default);
        factory.Gateway.RejectRefund = outcome == "Failed"; factory.Gateway.AmbiguousRefund = outcome == "Pending";
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var response = await client.PostAsJsonAsync($"/api/v1/admin/payments/{payment.Id}/refunds", new RefundRequest(amount, "Test-only reason", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refund = await response.Content.ReadFromJsonAsync<RefundStatusDto>();
        Assert.Equal(outcome, refund!.Status);
        for (var read = 0; read < 2; read++)
        {
            var detail = await client.GetFromJsonAsync<PaymentStatusDto>($"/api/v1/admin/payments/{payment.Id}");
            Assert.Equal(paymentStatus, detail!.Status); Assert.Equal(refunded, detail.RefundedAmount); Assert.Equal(available, detail.RefundableAmount);
            Assert.Equal(outcome, Assert.Single(detail.Refunds).Status);
            var order = await client.GetFromJsonAsync<JsonElement>($"/api/v1/admin/orders/{payment.OrderId}");
            Assert.Equal(paymentStatus, order.GetProperty("paymentStatus").GetString()); Assert.Equal("Confirmed", order.GetProperty("status").GetString());
        }
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(refunded, (await fixture.Db.Payments.SingleAsync()).RefundedAmount);
        Assert.Equal(8, (await fixture.Db.Products.SingleAsync()).Stock);
    }

    [Theory]
    [InlineData(0, "reason")]
    [InlineData(50, "")]
    [InlineData(50, null)]
    public async Task InvalidRefundInputReturnsValidationErrorWithoutIssuingCredit(int amount, string? reason)
    {
        await using var factory = new CommerceApiFactory(); using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var response = await client.PostAsJsonAsync("/api/v1/admin/payments/1/refunds", new { amount, reason, idempotencyKey = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, factory.Gateway.RefundCalls);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(problem.TryGetProperty("errors", out _));
    }
}
