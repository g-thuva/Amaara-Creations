using be.Application.Abstractions.Audit;
using be.Application.Common;
using be.Domain.Catalog;
using be.Domain.Inventory;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public sealed class ProductVariantService(IApplicationDbContext _context, IAuditService _auditService, IRequestActor _actor)
{
    public async Task<IReadOnlyList<ProductVariantResponse>> GetVariants(int id)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id))
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        return await _context.ProductVariants.AsNoTracking().Where(v => v.ProductId == id).OrderBy(v => v.Name).Select(v => ToVariantResponse(v)).ToListAsync();
    }

    public async Task<ProductVariantResponse> CreateVariant(int id, ProductVariantRequest request)
    {
        var validation = new RequestValidation();
        if (!await _context.Products.AnyAsync(p => p.Id == id))
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        if (await _context.ProductVariants.AnyAsync(v => v.Sku == request.Sku))
        {
            validation.AddModelError(nameof(request.Sku), "Variant SKU must be unique.");
            throw validation.Exception();
        }

        var variant = new ProductVariant
        {
            ProductId = id,
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            PriceOverride = request.PriceOverride,
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        _context.InventoryTransactions.Add(new InventoryTransaction { ProductId = id, ProductVariantId = variant.Id, PreviousQuantity = 0, QuantityDelta = variant.StockQuantity, NewQuantity = variant.StockQuantity, MovementType = "Opening", Reason = "Initial variant stock", PerformedByUserId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier) });
        await _auditService.RecordAsync("create", nameof(ProductVariant), variant.Id.ToString(), null, ToVariantResponse(variant), _actor.Audit);
        return ToVariantResponse(variant);
    }

    public async Task<ProductVariantResponse> UpdateVariant(int productId, int variantId, ProductVariantRequest request)
    {
        var validation = new RequestValidation();
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
        if (variant == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        if (request.RowVersion != null)
        {
            _context.Entry(variant).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
        }

        if (await _context.ProductVariants.AnyAsync(v => v.Sku == request.Sku && v.Id != variantId))
        {
            validation.AddModelError(nameof(request.Sku), "Variant SKU must be unique.");
            throw validation.Exception();
        }

        variant.Sku = request.Sku.Trim();
        variant.Name = request.Name.Trim();
        variant.PriceOverride = request.PriceOverride;
        if (variant.StockQuantity != request.StockQuantity)
            throw new CommerceException("CATALOG_CONFLICT", "Use Inventory to adjust stock with a reason.", 409);
        variant.IsActive = request.IsActive;
        variant.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CommerceException("CATALOG_CONFLICT", "Variant was modified by another request.", 409);
        }

        await _auditService.RecordAsync("update", nameof(ProductVariant), variant.Id.ToString(), null, ToVariantResponse(variant), _actor.Audit);
        return ToVariantResponse(variant);
    }

    public async Task ArchiveVariant(int productId, int variantId)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
        if (variant == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        variant.IsActive = false;
        variant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("archive", nameof(ProductVariant), variant.Id.ToString(), null, new { variant.IsActive }, _actor.Audit);
        return;
    }

    public async Task ReactivateVariant(int productId, int variantId)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
        if (variant == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        variant.IsActive = true;
        variant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("reactivate", nameof(ProductVariant), variant.Id.ToString(), null, new { variant.IsActive }, _actor.Audit);
        return;
    }

    private static ProductVariantResponse ToVariantResponse(ProductVariant variant)
    {
        return new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            Name = variant.Name,
            PriceOverride = variant.PriceOverride,
            StockQuantity = variant.StockQuantity,
            IsActive = variant.IsActive,
            RowVersion = Convert.ToBase64String(variant.RowVersion),
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };
    }
}
