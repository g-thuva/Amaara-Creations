using be.Application.Abstractions.Audit;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public class CollectionManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    public CollectionManagementService(IApplicationDbContext context, IAuditService auditService, IRequestActor actor)
    {
        _context = context;
        _auditService = auditService;
        _actor = actor;
    }

    public async Task<IReadOnlyList<CollectionResponse>> GetCollections(bool includeInactive = true)
    {
        var query = _context.Collections.AsNoTracking().Include(c => c.ProductCollections).AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var collections = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        return collections.Select(ToResponse).ToList();
    }

    public async Task<CollectionResponse> GetCollection(int id)
    {
        var collection = await _context.Collections.AsNoTracking().Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
        return collection == null ? throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404) : ToResponse(collection);
    }

    public async Task<CollectionResponse> CreateCollection(CollectionRequest request)
    {
        var collection = new be.Domain.Catalog.Collection
        {
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name),
            Description = request.Description?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("create", nameof(be.Domain.Catalog.Collection), collection.Id.ToString(), null, ToResponse(collection), _actor.Audit);
        return ToResponse(collection);
    }

    public async Task<CollectionResponse> UpdateCollection(int id, CollectionRequest request)
    {
        var collection = await _context.Collections.Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
        if (collection == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        collection.Name = request.Name.Trim();
        collection.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, id);
        collection.Description = request.Description?.Trim();
        collection.SortOrder = request.SortOrder;
        collection.IsActive = request.IsActive;
        collection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("update", nameof(be.Domain.Catalog.Collection), id.ToString(), null, ToResponse(collection), _actor.Audit);
        return ToResponse(collection);
    }

    public async Task ArchiveCollection(int id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        collection.IsActive = false;
        collection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("archive", nameof(be.Domain.Catalog.Collection), id.ToString(), null, new { collection.IsActive }, _actor.Audit);
        return;
    }

    public async Task ReactivateCollection(int id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        collection.IsActive = true;
        collection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("reactivate", nameof(be.Domain.Catalog.Collection), id.ToString(), null, new { collection.IsActive }, _actor.Audit);
        return;
    }

    public async Task ReplaceProducts(int id, CollectionProductsRequest request)
    {
        var validation = new RequestValidation();
        var collection = await _context.Collections.Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
        if (collection == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        var productIds = request.ProductIds.Distinct().ToList();
        if (productIds.Count != request.ProductIds.Count)
        {
            validation.AddModelError(nameof(request.ProductIds), "Duplicate product assignments are not allowed.");
        }

        var existingCount = await _context.Products.CountAsync(p => productIds.Contains(p.Id));
        if (existingCount != productIds.Count)
        {
            validation.AddModelError(nameof(request.ProductIds), "One or more products do not exist.");
        }

        collection.ProductCollections.Clear();
        for (var i = 0; i < productIds.Count; i++)
        {
            collection.ProductCollections.Add(new be.Domain.Catalog.ProductCollection { CollectionId = id, ProductId = productIds[i], SortOrder = i });
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("assign-products", nameof(be.Domain.Catalog.Collection), id.ToString(), null, new { ProductIds = productIds }, _actor.Audit);
        return;
    }

    public async Task RemoveProduct(int collectionId, int productId)
    {
        var link = await _context.ProductCollections.FindAsync(productId, collectionId);
        if (link == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        _context.ProductCollections.Remove(link);
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("remove-product", nameof(be.Domain.Catalog.Collection), collectionId.ToString(), null, new { productId }, _actor.Audit);
        return;
    }

    private async Task<string> GenerateUniqueSlugAsync(string value, int? collectionId = null)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"collection-{Guid.NewGuid():N}"[..26];
        }

        var candidate = slug;
        var suffix = 2;
        while (await _context.Collections.AnyAsync(c => c.Slug == candidate && (!collectionId.HasValue || c.Id != collectionId.Value)))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private static CollectionResponse ToResponse(be.Domain.Catalog.Collection collection)
    {
        return new CollectionResponse
        {
            Id = collection.Id,
            Name = collection.Name,
            Slug = collection.Slug,
            Description = collection.Description,
            SortOrder = collection.SortOrder,
            IsActive = collection.IsActive,
            ProductCount = collection.ProductCollections.Count,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt
        };
    }

    private readonly IRequestActor _actor;
}
