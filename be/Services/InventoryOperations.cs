using be.Data;
using be.Models;
using be.Services.Checkout;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace be.Services;

public sealed record InventoryAdjustment(int? VariantId, int ExpectedAvailable, int QuantityDelta,
    [Required, StringLength(300)] string Reason, [Range(0, int.MaxValue)] int LowStockThreshold);

public sealed class InventoryOperations(ApplicationDbContext db)
{
    public async Task AdjustAsync(int productId, InventoryAdjustment request, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 300 || request.LowStockThreshold < 0)
            throw new CommerceException("ADJUSTMENT_INVALID", "A reason and valid threshold are required.");
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new CommerceException("PRODUCT_NOT_FOUND", "Product not found.", 404);
        var variant = request.VariantId.HasValue ? await db.ProductVariants.SingleOrDefaultAsync(v => v.Id == request.VariantId && v.ProductId == productId, ct)
            ?? throw new CommerceException("VARIANT_NOT_FOUND", "Variant not found.", 404) : null;
        var previous = variant?.StockQuantity ?? product.Stock;
        if (previous != request.ExpectedAvailable) throw new CommerceException("STOCK_CHANGED", "Stock changed. Refresh before adjusting.", 409);
        var next = (long)previous + request.QuantityDelta;
        if (next < 0 || next > int.MaxValue) throw new CommerceException("STOCK_INVALID", "Adjustment would create an invalid stock balance.", 409);
        if (variant != null) { variant.StockQuantity = (int)next; variant.LowStockThreshold = request.LowStockThreshold; variant.UpdatedAt = DateTime.UtcNow; }
        else { product.Stock = (int)next; product.LowStockThreshold = request.LowStockThreshold; product.UpdatedAt = DateTime.UtcNow; }
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = productId, ProductVariantId = request.VariantId,
            PreviousQuantity = previous, QuantityDelta = request.QuantityDelta, NewQuantity = (int)next,
            Reason = request.Reason.Trim(), PerformedByUserId = actor, MovementType = "Adjustment" });
        db.AuditLogs.Add(new AuditLog { ActorUserId = actor, Action = "InventoryAdjusted", EntityType = "Product", EntityId = productId.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Available = previous }),
            NewValuesJson = JsonSerializer.Serialize(new { Available = next, request.VariantId, request.Reason, request.LowStockThreshold }) });
        await db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);
    }
}
