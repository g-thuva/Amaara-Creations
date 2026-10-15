using be.Data;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace be.Controllers;

[ApiController, Route("api/v1/admin/inventory"), Authorize(Policy = AppPolicies.ManageInventory)]
public sealed class AdminInventoryController(ApplicationDbContext db, InventoryOperations inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search) || (p.BaseSku != null && p.BaseSku.Contains(search)) || p.Variants.Any(v => v.Sku.Contains(search)));
        var totalCount = await query.CountAsync(ct);
        var products = await query.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.Id, p.Name, p.BaseSku, p.Stock, p.LowStockThreshold,
                Variants = p.Variants.Select(v => new { v.Id, v.Name, v.Sku, v.StockQuantity, v.LowStockThreshold }).ToList() }).ToListAsync(ct);
        var ids = products.Select(p => p.Id).ToArray();
        var reserved = await db.InventoryReservations.Where(r => ids.Contains(r.ProductId) && r.Status == "Reserved")
            .GroupBy(r => new { r.ProductId, r.ProductVariantId }).Select(g => new { g.Key.ProductId, g.Key.ProductVariantId, Quantity = g.Sum(r => r.Quantity) }).ToListAsync(ct);
        var rows = products.SelectMany(p => new[] { new { ProductId = p.Id, VariantId = (int?)null, Product = p.Name, Variant = "Base stock", Sku = p.BaseSku, Available = p.Stock, p.LowStockThreshold } }
            .Concat(p.Variants.Select(v => new { ProductId = p.Id, VariantId = (int?)v.Id, Product = p.Name, Variant = v.Name, Sku = (string?)v.Sku, Available = v.StockQuantity, v.LowStockThreshold })))
            .Select(r => new { r.ProductId, r.VariantId, r.Product, r.Variant, r.Sku, r.Available, r.LowStockThreshold,
                Reserved = reserved.Where(x => x.ProductId == r.ProductId && x.ProductVariantId == r.VariantId).Sum(x => x.Quantity),
                OnHand = r.Available + reserved.Where(x => x.ProductId == r.ProductId && x.ProductVariantId == r.VariantId).Sum(x => x.Quantity) });
        return Ok(new { items = rows, totalCount, page, pageSize });
    }

    [HttpGet("movements")]
    public async Task<IActionResult> Movements(int? productId, int page = 1, CancellationToken ct = default)
    {
        var query = db.InventoryTransactions.AsNoTracking().Where(m => !productId.HasValue || m.ProductId == productId);
        return Ok(new { items = await query.OrderByDescending(m => m.Id).Skip((Math.Max(1, page) - 1) * 50).Take(50)
            .Select(m => new { m.Id, m.ProductId, m.ProductVariantId, m.PreviousQuantity, m.QuantityDelta, m.NewQuantity, m.MovementType, m.OrderId, m.Reason, m.PerformedByUserId, m.CreatedAt }).ToListAsync(ct), totalCount = await query.CountAsync(ct) });
    }

    [HttpPost("{productId:int}/adjustments")]
    public async Task<IActionResult> Adjust(int productId, InventoryAdjustment request, CancellationToken ct)
    {
        await inventory.AdjustAsync(productId, request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return NoContent();
    }
}
