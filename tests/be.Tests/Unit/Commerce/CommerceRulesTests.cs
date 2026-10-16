using be.Application.Abstractions.Payments;
using be.Application.Features.Promotions;
using be.Application.Features.Shipping;
using be.Domain.Identity;
using be.Domain.Promotions;
using be.Domain.Shipping;
using be.Infrastructure.Payments.Datatrans;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace be.Tests;
public class CommerceRulesTests
{
    private static Promotion Coupon() => new()
    {
        Code = "TEST",
        IsActive = true,
        Currency = "LKR",
        StartsAt = DateTime.UtcNow.AddHours(-1),
        DiscountType = DiscountType.Percentage,
        DiscountValue = 20,
        MaximumDiscount = 15,
        PerCustomerUsageLimit = 2,
        UsageLimit = 10
    };
    [Fact]
    public void PercentageCouponCapsAndCannotMakeNegativeTotals()
    {
        Assert.Equal(15m, PromotionService.Evaluate(Coupon(), 200, 200, "LKR", 0, 0, DateTime.UtcNow).Discount);
        var p = Coupon();
        p.DiscountType = DiscountType.FixedAmount;
        p.DiscountValue = 1000;
        p.MaximumDiscount = null;
        Assert.Equal(10m, PromotionService.Evaluate(p, 10, 10, "LKR", 0, 0, DateTime.UtcNow).Discount);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("expired")]
    [InlineData("minimum")]
    [InlineData("usage")]
    [InlineData("customer")]
    [InlineData("currency")]
    [InlineData("future")]
    [InlineData("eligibility")]
    public void CouponRulesRejectInvalidRedemptions(string kind)
    {
        var p = Coupon();
        var usage = 0;
        var customer = 0;
        var eligible = 200m;
        switch (kind)
        {
            case "disabled":
                p.IsActive = false;
                break;
            case "expired":
                p.EndsAt = DateTime.UtcNow.AddSeconds(-1);
                break;
            case "minimum":
                p.MinimumSpend = 201;
                break;
            case "usage":
                usage = 10;
                break;
            case "customer":
                customer = 2;
                break;
            case "currency":
                p.Currency = "CHF";
                break;
            case "future":
                p.StartsAt = DateTime.UtcNow.AddDays(1);
                break;
            default:
                eligible = 0;
                break;
        }

        Assert.False(PromotionService.Evaluate(p, 200, eligible, "LKR", usage, customer, DateTime.UtcNow).Applied);
    }

    [Fact]
    public void ShippingMatchesDestinationAndThresholdAndRequiresPickupLocation()
    {
        var address = new Address
        {
            Country = "Sri Lanka",
            City = "Colombo",
            Phone = "0770000000"
        };
        var method = new ShippingMethod
        {
            Currency = "LKR",
            BasePrice = 30,
            FreeShippingThreshold = 100,
            ShippingZone = new ShippingZone
            {
                Country = "Sri Lanka"
            }
        };
        Assert.True(ShippingService.Eligible(method, address, 100, "LKR", false));
        Assert.Equal(0, ShippingService.Option(method, 100).Amount);
        address.Country = "Switzerland";
        Assert.False(ShippingService.Eligible(method, address, 100, "LKR", false));
        address.Country = "Sri Lanka";
        method.IsActive = false;
        Assert.False(ShippingService.Eligible(method, address, 100, "LKR", false));
        method.IsActive = true;
        method.IsPickup = true;
        Assert.False(ShippingService.Eligible(method, address, 100, "LKR", false));
        method.PickupLocation = "Test pickup";
        Assert.True(ShippingService.Eligible(method, address, 100, "LKR", false));
    }

    [Theory]
    [InlineData("body")]
    [InlineData("key")]
    [InlineData("expired")]
    [InlineData("duplicate")]
    public void WebhookSignatureRejectsTamperingAndReplay(string tamper)
    {
        var now = DateTimeOffset.UtcNow;
        var key = Enumerable.Repeat((byte)7, 32).ToArray();
        var o = new DatatransOptions
        {
            Enabled = true,
            HmacKey = Convert.ToHexString(key)
        };
        var body = Encoding.UTF8.GetBytes("{\"transactionId\":\"260000000000000000\"}");
        var timestamp = now.ToUnixTimeMilliseconds().ToString();
        var header = "t=" + timestamp + ",s0=" + Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(timestamp).Concat(body).ToArray()));
        var verifier = new DatatransWebhookVerifier(Options.Create(o));
        Assert.True(verifier.Verify(body, header, now));
        switch (tamper)
        {
            case "body":
                body = Encoding.UTF8.GetBytes("{}");
                break;
            case "key":
                o.HmacKey = Convert.ToHexString(Enumerable.Repeat((byte)8, 32).ToArray());
                break;
            case "expired":
                now = now.AddMinutes(6);
                break;
            default:
                header += ",t=" + timestamp;
                break;
        }

        Assert.False(verifier.Verify(body, header, now));
        Assert.False(verifier.Verify(body, null, now));
    }

    [Theory]
    [InlineData("settled", GatewayState.Paid)]
    [InlineData("transmitted", GatewayState.Paid)]
    [InlineData("authorized", GatewayState.Authorized)]
    [InlineData("canceled", GatewayState.Cancelled)]
    [InlineData("failed", GatewayState.Failed)]
    [InlineData("unrecognized", GatewayState.Unknown)]
    [InlineData("credited", GatewayState.Unknown)]
    public void ProviderStatusesAreMappedExactly(string status, GatewayState expected) => Assert.Equal(expected, DatatransPaymentGateway.MapStatus(status));
}
