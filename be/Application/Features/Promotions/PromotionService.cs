using be.Domain.Common;
using be.Application.Features.Checkout;
using be.Domain.Orders;
using be.Domain.Promotions;
using be.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Promotions;
public sealed class PromotionService(IApplicationDbContext db)
{
    public async Task<(Promotion? Promotion, CouponResultDto? Result)> EvaluateAsync(
        string? code,
        string userId,
        IReadOnlyList<OrderItem> items,
        decimal subtotal,
        string currency,
        CancellationToken ct,
        int? excludeOrderId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (null, null);
        code = code.Trim().ToUpperInvariant();
        var promotion = await db.Promotions.SingleOrDefaultAsync(p => p.Code == code, ct);
        if (promotion == null)
            return (null, Rejected(code, "COUPON_INVALID", "This coupon is unavailable."));
        var redemptions = db.PromotionRedemptions.Where(r => r.PromotionId == promotion.Id && r.Status != "Released" && (!excludeOrderId.HasValue || r.OrderId != excludeOrderId));
        var activeUsage = await redemptions.CountAsync(ct);
        var customerUsage = await redemptions.CountAsync(r => r.UserId == userId, ct);
        // Preserve any historical usage which predates the redemption ledger.
        var committed = await db.PromotionRedemptions.CountAsync(r => r.PromotionId == promotion.Id && r.Status == "Committed", ct);
        activeUsage += Math.Max(0, promotion.UsageCount - committed);
        var eligible = items.Where(i => i.ItemType == OrderItemType.Product ? promotion.AppliesToStandard : promotion.AppliesToCustom).Sum(i => i.Subtotal);
        return (promotion, Evaluate(promotion, subtotal, eligible, currency, activeUsage, customerUsage, DateTime.UtcNow));
    }

    public static CouponResultDto Evaluate(Promotion p, decimal subtotal, decimal eligible, string currency, int usage, int customerUsage, DateTime now)
    {
        if (!p.IsActive || p.Currency != currency || p.StartsAt > now)
            return Rejected(p.Code, "COUPON_INVALID", "This coupon is unavailable.");
        if (p.EndsAt.HasValue && p.EndsAt <= now)
            return Rejected(p.Code, "COUPON_EXPIRED", "This coupon has expired.");
        if (p.MinimumSpend.HasValue && subtotal < p.MinimumSpend)
            return Rejected(p.Code, "COUPON_MINIMUM_SPEND", "The minimum order value has not been reached.");
        if (p.UsageLimit.HasValue && usage >= p.UsageLimit || p.PerCustomerUsageLimit.HasValue && customerUsage >= p.PerCustomerUsageLimit)
            return Rejected(p.Code, "COUPON_USAGE_LIMIT", "This coupon's usage limit has been reached.");
        if (eligible <= 0 || p.DiscountValue < 0 || p.DiscountType == DiscountType.Percentage && p.DiscountValue > 100)
            return Rejected(p.Code, "COUPON_INVALID", "This coupon does not apply to these items.");
        var discount = p.DiscountType == DiscountType.Percentage ? eligible * p.DiscountValue / 100m : p.DiscountValue;
        if (p.MaximumDiscount.HasValue)
            discount = Math.Min(discount, p.MaximumDiscount.Value);
        discount = Money.Round(Math.Clamp(discount, 0, eligible), currency);
        return new(p.Code, true, null, "Coupon applied.", discount);
    }

    private static CouponResultDto Rejected(string code, string error, string message) => new(code, false, error, message, 0);
}
