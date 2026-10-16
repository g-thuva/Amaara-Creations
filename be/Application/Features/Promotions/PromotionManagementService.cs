using be.Domain.Common;
using be.Application.Abstractions.Audit;
using be.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;

namespace be.Application.Features.Promotions;
public sealed class PromotionManagementService(IApplicationDbContext db, IAuditService audit, IRequestActor _actor)
{
    public async Task<object?> List(int page = 1, CancellationToken ct = default) => new
    {
        items = await db.Promotions.AsNoTracking().OrderByDescending(p => p.Id).Skip((Math.Max(1, page) - 1) * 30).Take(30).ToListAsync(ct),
        totalCount = await db.Promotions.CountAsync(ct)
    };
    public async Task<object?> History(int id, int page = 1, CancellationToken ct = default) => new
    {
        items = await db.PromotionRedemptions.AsNoTracking()
        .Where(r => r.PromotionId == id)
        .OrderByDescending(r => r.Id)
        .Skip((Math.Max(1, page) - 1) * 50)
        .Take(50)
        .Select(r => new { r.Id, r.OrderId, r.Status, r.Amount, r.CreatedAt, r.Promotion.Currency })
        .ToListAsync(ct)
    };
    public Task<object?> Create(PromotionRequest request, CancellationToken ct) => Save(null, request, ct);
    public Task<object?> Update(int id, PromotionRequest request, CancellationToken ct) => Save(id, request, ct);
    private async Task<object?> Save(int? id, PromotionRequest r, CancellationToken ct)
    {
        if (!Enum.TryParse<DiscountType>(r.DiscountType, out var type) || !Enum.IsDefined(type) || r.DiscountValue <= 0 || type == DiscountType.Percentage && r.DiscountValue > 100 || r.MinimumSpend < 0 || r.MaximumDiscount < 0 || r.UsageLimit <= 0 || r.PerCustomerUsageLimit <= 0 || r.EndsAt <= r.StartsAt || !r.AppliesToStandard && !r.AppliesToCustom || r.Currency is not ("LKR" or "CHF" or "USD" or "EUR" or "GBP") || Money.Round(r.DiscountValue, r.Currency) != r.DiscountValue)
            throw new CommerceException("PROMOTION_INVALID", "Review the discount, currency, dates, limits and eligible items.");
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var p = id.HasValue ? await db.Promotions.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PROMOTION_NOT_FOUND", "Promotion not found.", 404) : new Promotion();
        var code = r.Code.Trim().ToUpperInvariant();
        if (await db.Promotions.AnyAsync(p => p.Code == code && p.Id != (id ?? 0), ct))
            throw new CommerceException("PROMOTION_DUPLICATE", "This promotion code already exists.", 409);
        if (id.HasValue && (r.RowVersion == null || r.RowVersion != Convert.ToBase64String(p.RowVersion)))
            throw new CommerceException("PROMOTION_CHANGED", "Refresh this promotion before editing.", 409);
        var old = new
        {
            p.Code,
            p.Name,
            p.DiscountValue,
            p.IsActive,
            p.Currency
        };
        p.Code = code;
        p.Name = r.Name.Trim();
        p.DiscountType = type;
        p.DiscountValue = r.DiscountValue;
        p.Currency = r.Currency;
        p.StartsAt = r.StartsAt;
        p.EndsAt = r.EndsAt;
        p.MinimumSpend = r.MinimumSpend;
        p.MaximumDiscount = r.MaximumDiscount;
        p.UsageLimit = r.UsageLimit;
        p.PerCustomerUsageLimit = r.PerCustomerUsageLimit;
        p.IsActive = r.IsActive;
        p.AppliesToStandard = r.AppliesToStandard;
        p.AppliesToCustom = r.AppliesToCustom;
        p.UpdatedAt = DateTime.UtcNow;
        if (!id.HasValue)
            db.Promotions.Add(p);
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("PromotionSaved", "Promotion", p.Id.ToString(), old, new { p.Code, p.Name, p.DiscountValue, p.Currency, p.IsActive, p.UsageLimit, p.PerCustomerUsageLimit }, _actor.Audit, ct);
        if (tx != null)
            await tx.CommitAsync(ct);
        return p;
    }
}
