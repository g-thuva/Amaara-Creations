using be.Domain.Common;
using be.Application.Abstractions.Audit;
using be.Domain.CustomDesigns;
using be.Domain.Orders;
using System.Text;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;

namespace be.Application.Features.Reporting;
public sealed class ReportingQueries(IApplicationDbContext db, IAuditService audit, IRequestActor _actor)
{
    public async Task<object?> Reports(DateTime? from, DateTime? to, CancellationToken ct)
    {
        ValidateDates(from, to);
        var orders = db.Orders.AsNoTracking().Where(o => (!from.HasValue || o.OrderDate >= from) && (!to.HasValue || o.OrderDate < to));
        var payments = db.Payments.AsNoTracking().Where(p => (!from.HasValue || p.CreatedAt >= from) && (!to.HasValue || p.CreatedAt < to));
        var soldOrders = orders.Where(o => o.FinalizedAt != null && o.Status != OrderStatus.Cancelled && o.Payments.Any(p => p.SettledAmount > 0));
        return new
        {
            from,
            to,
            money = await payments.GroupBy(p => new { p.Currency, p.ProviderMode })
            .Select(g => new { g.Key.Currency, g.Key.ProviderMode, Attempts = g.Count(), Settled = g.Sum(p => p.SettledAmount), Refunded = g.Sum(p => p.RefundedAmount) })
            .ToListAsync(ct),
            orders = await orders.GroupBy(o => new { o.Status, o.Currency })
            .Select(g => new { Status = g.Key.Status.ToString(), g.Key.Currency, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .ToListAsync(ct),
            products = await soldOrders.SelectMany(o => o.OrderItems)
            .GroupBy(i => new { i.ProductNameSnapshot, i.ItemType, i.Order!.Currency })
            .Select(g => new { Product = g.Key.ProductNameSnapshot, Type = g.Key.ItemType.ToString(), g.Key.Currency, Quantity = g.Sum(i => i.Quantity), Total = g.Sum(i => i.Subtotal) })
            .ToListAsync(ct),
            production = await db.CustomDesigns.Where(d => d.Status == CustomDesignStatus.Ordered)
            .GroupBy(d => d.ProductionStatus)
            .Select(g => new { State = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct),
            inventory = await db.InventoryTransactions.Where(m => (!from.HasValue || m.CreatedAt >= from) && (!to.HasValue || m.CreatedAt < to))
            .GroupBy(m => m.MovementType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Delta = g.Sum(m => m.QuantityDelta) })
            .ToListAsync(ct),
            promotions = await db.PromotionRedemptions.Where(r => (!from.HasValue || r.CreatedAt >= from) && (!to.HasValue || r.CreatedAt < to))
            .GroupBy(r => new { r.Promotion.Code, r.Promotion.Currency, r.Status })
            .Select(g => new { g.Key.Code, g.Key.Currency, g.Key.Status, Count = g.Count(), Discount = g.Sum(r => r.Amount) })
            .ToListAsync(ct)
        };
    }

    public async Task<DownloadContent> Export(DateTime? from, DateTime? to, CancellationToken ct)
    {
        ValidateDates(from, to);
        var query = db.Orders.AsNoTracking().Where(o => (!from.HasValue || o.OrderDate >= from) && (!to.HasValue || o.OrderDate < to));
        if (await query.CountAsync(ct) > 10000)
            throw new CommerceException("EXPORT_TOO_LARGE", "Narrow the date range to at most 10,000 orders.");
        var rows = await query.OrderBy(o => o.Id)
            .Select(o => new { o.OrderNumber, o.OrderDate, o.Status, o.Currency, o.Subtotal, o.DiscountTotal, o.ShippingTotal, o.Total })
            .ToListAsync(ct);
        var csv = new StringBuilder("order_number,ordered_at_utc,status,currency,subtotal,discount,shipping,total\r\n");
        foreach (var r in rows)
            csv.AppendLine(string.Join(",", new[] { r.OrderNumber, r.OrderDate.ToString("O"), r.Status.ToString(), r.Currency, r.Subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.DiscountTotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.ShippingTotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.Total.ToString(System.Globalization.CultureInfo.InvariantCulture) }.Select(CsvCell)));
        await audit.RecordAsync("ReportExported", "Order", null, null, new { from, to, Count = rows.Count }, _actor.Audit, ct);
        return new DownloadContent(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "orders.csv");
    }

    public static string CsvCell(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void ValidateDates(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from >= to)
            throw new CommerceException("DATE_RANGE_INVALID", "The end date must follow the start date (exclusive).");
    }
}
