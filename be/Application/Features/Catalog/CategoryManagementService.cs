using be.Application.Abstractions.Audit;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public class CategoryManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    public CategoryManagementService(IApplicationDbContext context, IAuditService auditService, IRequestActor actor)
    {
        _context = context;
        _auditService = auditService;
        _actor = actor;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategories(bool includeInactive = true)
    {
        var query = _context.Categories.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        return categories.Select(ToResponse).ToList();
    }

    public async Task<CategoryResponse> GetCategory(int id)
    {
        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return category == null ? throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404) : ToResponse(category);
    }

    public async Task<CategoryResponse> CreateCategory(CategoryRequest request)
    {
        await ValidateAsync(request);
        var category = new be.Domain.Catalog.Category
        {
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name),
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            ImageMediaId = request.ImageMediaId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("create", nameof(be.Domain.Catalog.Category), category.Id.ToString(), null, ToResponse(category), _actor.Audit);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        await ValidateAsync(request, id);
        category.Name = request.Name.Trim();
        category.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, id);
        category.Description = request.Description?.Trim();
        category.ParentCategoryId = request.ParentCategoryId;
        category.ImageMediaId = request.ImageMediaId;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("update", nameof(be.Domain.Catalog.Category), id.ToString(), null, ToResponse(category), _actor.Audit);
        return ToResponse(category);
    }

    public async Task ArchiveCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("archive", nameof(be.Domain.Catalog.Category), id.ToString(), null, new { category.IsActive }, _actor.Audit);
        return;
    }

    public async Task ReactivateCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        category.IsActive = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("reactivate", nameof(be.Domain.Catalog.Category), id.ToString(), null, new { category.IsActive }, _actor.Audit);
        return;
    }

    private async Task ValidateAsync(CategoryRequest request, int? categoryId = null)
    {
        var validation = new RequestValidation();
        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId == categoryId)
            {
                validation.AddModelError(nameof(request.ParentCategoryId), "A category cannot be its own parent.");
            }
            else if (!await _context.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value))
            {
                validation.AddModelError(nameof(request.ParentCategoryId), "Parent category does not exist.");
            }
            else if (categoryId.HasValue && await WouldCreateCycleAsync(categoryId.Value, request.ParentCategoryId.Value))
            {
                validation.AddModelError(nameof(request.ParentCategoryId), "Category hierarchy cycles are not allowed.");
            }
        }

        if (request.ImageMediaId.HasValue && !await _context.ProductMedia.AnyAsync(m => m.Id == request.ImageMediaId.Value))
        {
            validation.AddModelError(nameof(request.ImageMediaId), "Image media does not exist.");
        }

        if (validation.IsValid)
        {
            return;
        }

        throw validation.Exception();
    }

    private async Task<bool> WouldCreateCycleAsync(int categoryId, int parentId)
    {
        var current = parentId;
        while (true)
        {
            if (current == categoryId)
            {
                return true;
            }

            var next = await _context.Categories.Where(c => c.Id == current).Select(c => c.ParentCategoryId).FirstOrDefaultAsync();
            if (!next.HasValue)
            {
                return false;
            }

            current = next.Value;
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string value, int? categoryId = null)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"category-{Guid.NewGuid():N}"[..24];
        }

        var candidate = slug;
        var suffix = 2;
        while (await _context.Categories.AnyAsync(c => c.Slug == candidate && (!categoryId.HasValue || c.Id != categoryId.Value)))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private static CategoryResponse ToResponse(be.Domain.Catalog.Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            ImageMediaId = category.ImageMediaId,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private readonly IRequestActor _actor;
}
