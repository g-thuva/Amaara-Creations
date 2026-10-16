using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;

namespace be.Application.Features.Catalog;
public class CatalogQueries
{
    private readonly IApplicationDbContext _context;
    public CatalogQueries(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategories()
    {
        var categories = await _context.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, ParentCategoryId = c.ParentCategoryId, ImageMediaId = c.ImageMediaId, ImageUrl = c.ImageMedia != null ? c.ImageMedia.Url ?? c.ImageMedia.StorageKey : null, SortOrder = c.SortOrder, IsActive = c.IsActive, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt })
            .ToListAsync();
        return categories;
    }

    public async Task<IReadOnlyList<CollectionResponse>> GetCollections()
    {
        var collections = await _context.Collections.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CollectionResponse { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, SortOrder = c.SortOrder, IsActive = c.IsActive, ProductCount = c.ProductCollections.Count(p => p.Product!.IsActive), CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt })
            .ToListAsync();
        return collections;
    }
}
