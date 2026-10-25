using be.Application.Features.Checkout;
using be.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace be.Tests;

public sealed class OptionalCouponTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChfCheckoutCanCreatePaymentWithoutACoupon(string? coupon)
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync(currency: "CHF");
        var request = new CheckoutSessionRequest
        {
            AddressId = 1, ShippingMethodId = 1, CouponCode = coupon, IdempotencyKey = Guid.NewGuid(),
        };
        var quote = (await fixture.Pricing.CalculateAsync("a", request, default)).Quote;
        Assert.True(quote.CanPay);
        Assert.Null(quote.CouponResult);
        Assert.Equal(0, quote.Discount);
        Assert.Equal("CHF", quote.Currency);
        request.QuoteVersion = quote.QuoteVersion;
        await fixture.Checkout.CreateAsync("a", request, default);
        var order = await fixture.Db.Orders.SingleAsync();
        Assert.Null(order.CouponCode);
        Assert.Equal(0, order.DiscountTotal);
        Assert.Equal(quote.Total, (await fixture.Db.Payments.SingleAsync()).Amount);
        Assert.Empty(fixture.Db.PromotionRedemptions);
    }

    [Fact]
    public async Task RemovingCouponDoesNotBypassUnavailableGatewayOrCreateAnUnpayableOrder()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync(currency: "CHF");
        fixture.Gateway.Available = false;
        var request = new CheckoutSessionRequest
        {
            AddressId = 1, ShippingMethodId = 1, IdempotencyKey = Guid.NewGuid(),
        };
        var quote = (await fixture.Pricing.CalculateAsync("a", request, default)).Quote;
        Assert.False(quote.PaymentAvailable);
        Assert.False(quote.CanPay);
        Assert.Null(quote.CouponResult);
        request.QuoteVersion = quote.QuoteVersion;
        await Assert.ThrowsAsync<CommerceException>(() => fixture.Checkout.CreateAsync("a", request, default));
        Assert.Empty(fixture.Db.Orders);
        Assert.Empty(fixture.Db.Payments);
    }
}
