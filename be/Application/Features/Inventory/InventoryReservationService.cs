using be.Domain.Common;
using be.Application.Features.Checkout;
using be.Domain.CustomDesigns;
using be.Domain.Inventory;
using be.Domain.Orders;
using be.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Application.Features.Inventory;
public sealed class InventoryReservationService(IApplicationDbContext db, IOptions<CheckoutOptions> options)
{
    // Every mutation is called within CommerceTransaction.
    public async Task ReserveAsync(Order order, CancellationToken ct)
    {
        var existing = await db.InventoryReservations.Where(r => r.OrderId == order.Id && r.Status != "Released").ToListAsync(ct);
        if (existing.Any())
            return;
        foreach (var group in order.OrderItems.Where(i => i.ItemType == OrderItemType.Product)
            .GroupBy(i => new { i.ProductId, i.ProductVariantId })
            .OrderBy(g => g.Key.ProductId)
            .ThenBy(g => g.Key.ProductVariantId))
        {
            var product = await db.Products.SingleOrDefaultAsync(p => p.Id == group.Key.ProductId, ct);
            var quantity = group.Sum(i => i.Quantity);
            if (product is not { IsActive: true })
                throw new CommerceException("PRODUCT_UNAVAILABLE", "A product is no longer available.", 409);
            if (group.Key.ProductVariantId.HasValue)
            {
                var variant = await db.ProductVariants.SingleOrDefaultAsync(v => v.Id == group.Key.ProductVariantId && v.ProductId == product.Id, ct);
                if (variant is not { IsActive: true } || variant.StockQuantity < quantity)
                    throw Insufficient(product.Name);
                variant.StockQuantity -= quantity;
                variant.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                if (product.Stock < quantity)
                    throw Insufficient(product.Name);
                product.Stock -= quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            var balance = group.Key.ProductVariantId.HasValue ? db.ProductVariants.Local.Single(v => v.Id == group.Key.ProductVariantId).StockQuantity : product.Stock;
            db.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, ProductVariantId = group.Key.ProductVariantId, OrderId = order.Id, PreviousQuantity = balance + quantity, QuantityDelta = -quantity, NewQuantity = balance, MovementType = "Reserved", Reason = "Checkout reservation" });
            db.InventoryReservations.Add(new InventoryReservation { Order = order, ProductId = product.Id, ProductVariantId = group.Key.ProductVariantId, Quantity = quantity, ExpiresAt = DateTime.UtcNow.AddMinutes(options.Value.InventoryReservationMinutes) });
        }
    }

    public async Task ConsumeAsync(int orderId, CancellationToken ct)
    {
        foreach (var reservation in await db.InventoryReservations.Where(r => r.OrderId == orderId && r.Status == "Reserved").ToListAsync(ct))
        {
            var balance = reservation.ProductVariantId.HasValue ? await db.ProductVariants.Where(v => v.Id == reservation.ProductVariantId).Select(v => v.StockQuantity).SingleAsync(ct) : await db.Products.Where(p => p.Id == reservation.ProductId).Select(p => p.Stock).SingleAsync(ct);
            db.InventoryTransactions.Add(new InventoryTransaction { ProductId = reservation.ProductId, ProductVariantId = reservation.ProductVariantId, OrderId = orderId, PreviousQuantity = balance, QuantityDelta = 0, NewQuantity = balance, MovementType = "Consumed", Reason = "Reserved goods sold; available stock already reduced" });
            reservation.Status = "Consumed";
            reservation.ConsumedAt = DateTime.UtcNow;
        }
    }

    public async Task ReleaseAsync(int orderId, CancellationToken ct)
    {
        var reservations = await db.InventoryReservations.Include(r => r.Product)
            .Include(r => r.ProductVariant)
            .Where(r => r.OrderId == orderId && r.Status == "Reserved")
            .ToListAsync(ct);
        foreach (var r in reservations)
        {
            if (r.ProductVariant != null)
            {
                r.ProductVariant.StockQuantity += r.Quantity;
                r.ProductVariant.UpdatedAt = DateTime.UtcNow;
            }
            else if (r.Product != null)
            {
                r.Product.Stock += r.Quantity;
                r.Product.UpdatedAt = DateTime.UtcNow;
            }

            var balance = r.ProductVariant?.StockQuantity ?? r.Product?.Stock ?? 0;
            db.InventoryTransactions.Add(new InventoryTransaction { ProductId = r.ProductId, ProductVariantId = r.ProductVariantId, OrderId = orderId, PreviousQuantity = balance - r.Quantity, QuantityDelta = r.Quantity, NewQuantity = balance, MovementType = "Released", Reason = "Reservation released" });
            r.Status = "Released";
            r.ReleasedAt = DateTime.UtcNow;
        }

        foreach (var redemption in await db.PromotionRedemptions.Where(r => r.OrderId == orderId && r.Status == "Reserved").ToListAsync(ct))
            redemption.Status = "Released";
        var designIds = await db.OrderItems.Where(i => i.OrderId == orderId && i.CustomDesignId != null).Select(i => i.CustomDesignId!.Value).ToListAsync(ct);
        foreach (var design in await db.CustomDesigns.Where(d => designIds.Contains(d.Id)).ToListAsync(ct))
        {
            if (design.Status != CustomDesignStatus.Ordered)
                continue;
            design.Status = await db.CartItems.AnyAsync(i => i.CustomDesignId == design.Id, ct) ? CustomDesignStatus.InCart : CustomDesignStatus.Ready;
        }
    }

    private static CommerceException Insufficient(string name) => new("INSUFFICIENT_STOCK", $"There is insufficient stock for {name}.", 409);
}
