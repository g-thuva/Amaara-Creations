using be.Application.Abstractions.Audit;
using be.Application.Common;
using be.Domain.Catalog;
using be.Domain.Inventory;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public sealed class ProductManagementService(IApplicationDbContext _context, IAuditService _auditService, IRequestActor _actor)
{
    public async Task<PagedResult<AdminProductResponse>> GetProducts(AdminProductQuery query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, PaginationQuery.MaxPageSize);
        var products = _context.Products.AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.CategoryEntity)
            .Include(p => p.ProductCollections)
            .Include(p => p.Media)
            .Include(p => p.Variants)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(search) || p.Slug.Contains(search) || (p.BaseSku != null && p.BaseSku.Contains(search)) || p.Description.Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.CollectionId.HasValue)
        {
            products = products.Where(p => p.ProductCollections.Any(pc => pc.CollectionId == query.CollectionId.Value));
        }

        if (query.IsActive.HasValue)
        {
            products = products.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (query.IsFeatured.HasValue)
        {
            products = products.Where(p => p.IsFeatured == query.IsFeatured.Value);
        }

        products = ApplySort(products, query.SortBy, query.SortDirection);
        var total = await products.CountAsync();
        var productItems = await products.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = productItems.Select(ToProductResponse).ToList();
        return PagedResult<AdminProductResponse>.Create(items, page, pageSize, total);
    }

    public async Task<AdminProductResponse> GetProduct(int id)
    {
        var product = await _context.Products.AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.CategoryEntity)
            .Include(p => p.ProductCollections)
            .Include(p => p.Media)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
        return product == null ? throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404) : ToProductResponse(product);
    }

    public async Task<AdminProductResponse> CreateProduct(AdminProductRequest request)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(default);
        await ValidateProductRequestAsync(request);
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name),
            Description = request.Description.Trim(),
            ShortDescription = request.ShortDescription?.Trim(),
            Price = request.BasePrice,
            BasePrice = request.BasePrice,
            Currency = request.Currency,
            BaseSku = string.IsNullOrWhiteSpace(request.BaseSku) ? null : request.BaseSku.Trim(),
            CategoryId = request.CategoryId,
            Category = await ResolveCategoryLabelAsync(request.CategoryId, request.Category),
            Stock = request.Stock,
            ImageUrl = string.Empty,
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured,
            CreatedAt = now,
            UpdatedAt = now
        };
        foreach (var collectionId in request.CollectionIds.Distinct())
        {
            product.ProductCollections.Add(new ProductCollection { CollectionId = collectionId });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _context.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, PreviousQuantity = 0, QuantityDelta = product.Stock, NewQuantity = product.Stock, MovementType = "Opening", Reason = "Initial catalog stock", PerformedByUserId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier) });
        await _auditService.RecordAsync("create", nameof(Product), product.Id.ToString(), null, ToProductResponse(product), _actor.Audit);
        var savedProduct = await LoadProductAsync(product.Id);
        if (transaction != null)
            await transaction.CommitAsync();
        return ToProductResponse(savedProduct!);
    }

    public async Task<AdminProductResponse> UpdateProduct(int id, AdminProductRequest request)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(default);
        var product = await _context.Products.Include(p => p.ProductCollections).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        await ValidateProductRequestAsync(request, product.Id);
        if (request.Currency != product.Currency)
            throw new CommerceException("CATALOG_CONFLICT", "Existing prices cannot be relabeled. Create a separately priced product for a new currency.", 409);
        if (request.RowVersion == null || request.RowVersion != Convert.ToBase64String(product.RowVersion))
            throw new CommerceException("CATALOG_CONFLICT", "Refresh this product before editing.", 409);
        var oldValues = new
        {
            product.Name,
            product.Slug,
            product.BasePrice,
            product.Currency,
            product.Stock,
            product.IsActive,
            product.IsFeatured
        };
        product.Name = request.Name.Trim();
        product.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, product.Id);
        product.Description = request.Description.Trim();
        product.ShortDescription = request.ShortDescription?.Trim();
        product.Price = request.BasePrice;
        product.BasePrice = request.BasePrice;
        product.BaseSku = string.IsNullOrWhiteSpace(request.BaseSku) ? null : request.BaseSku.Trim();
        product.CategoryId = request.CategoryId;
        product.Category = await ResolveCategoryLabelAsync(request.CategoryId, request.Category);
        if (product.Stock != request.Stock)
            throw new CommerceException("CATALOG_CONFLICT", "Use Inventory to adjust stock with a reason.", 409);
        product.IsActive = request.IsActive;
        product.IsFeatured = request.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;
        product.ProductCollections.Clear();
        foreach (var collectionId in request.CollectionIds.Distinct())
        {
            product.ProductCollections.Add(new ProductCollection { ProductId = product.Id, CollectionId = collectionId });
        }

        await _context.SaveChangesAsync();
        var savedProduct = await LoadProductAsync(product.Id);
        await _auditService.RecordAsync("update", nameof(Product), product.Id.ToString(), oldValues, ToProductResponse(savedProduct!), _actor.Audit);
        if (transaction != null)
            await transaction.CommitAsync();
        return ToProductResponse(savedProduct!);
    }

    public async Task ArchiveProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("archive", nameof(Product), id.ToString(), null, new { product.IsActive }, _actor.Audit);
        return;
    }

    public async Task ReactivateProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("reactivate", nameof(Product), id.ToString(), null, new { product.IsActive }, _actor.Audit);
        return;
    }

    private async Task<Product?> LoadProductAsync(int id)
    {
        return await _context.Products.AsSplitQuery()
            .Include(p => p.CategoryEntity)
            .Include(p => p.ProductCollections)
            .Include(p => p.Media)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task ValidateProductRequestAsync(AdminProductRequest request, int? productId = null)
    {
        var validation = new RequestValidation();
        if (request.CategoryId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == request.CategoryId.Value))
        {
            validation.AddModelError(nameof(request.CategoryId), "Category does not exist.");
        }

        if (!string.IsNullOrWhiteSpace(request.BaseSku) && await _context.Products.AnyAsync(p => p.BaseSku == request.BaseSku && (!productId.HasValue || p.Id != productId.Value)))
        {
            validation.AddModelError(nameof(request.BaseSku), "Product SKU must be unique.");
        }

        var distinctCollectionIds = request.CollectionIds.Distinct().ToList();
        if (distinctCollectionIds.Count != request.CollectionIds.Count)
        {
            validation.AddModelError(nameof(request.CollectionIds), "Duplicate collection assignments are not allowed.");
        }

        var existingCollectionCount = await _context.Collections.CountAsync(c => distinctCollectionIds.Contains(c.Id));
        if (existingCollectionCount != distinctCollectionIds.Count)
        {
            validation.AddModelError(nameof(request.CollectionIds), "One or more collections do not exist.");
        }

        if (validation.IsValid)
        {
            return;
        }

        throw validation.Exception();
    }

    private async Task<string> ResolveCategoryLabelAsync(int? categoryId, string? fallback)
    {
        if (categoryId.HasValue)
        {
            return await _context.Categories.Where(c => c.Id == categoryId.Value).Select(c => c.Slug).FirstOrDefaultAsync() ?? "custom";
        }

        return string.IsNullOrWhiteSpace(fallback) ? "custom" : fallback.Trim();
    }

    private async Task<string> GenerateUniqueSlugAsync(string value, int? existingProductId = null)
    {
        var slug = Slugify(value);
        var candidate = slug;
        var suffix = 2;
        while (await _context.Products.AnyAsync(p => p.Slug == candidate && (!existingProductId.HasValue || p.Id != existingProductId.Value)))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}"[..20] : slug;
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sortBy, string? direction)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? "createdAt").ToLowerInvariant() switch
        {
            "name" => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
            "updatedat" => descending ? products.OrderByDescending(p => p.UpdatedAt) : products.OrderBy(p => p.UpdatedAt),
            "baseprice" or "price" => descending ? products.OrderByDescending(p => p.BasePrice) : products.OrderBy(p => p.BasePrice),
            "stock" => descending ? products.OrderByDescending(p => p.Stock) : products.OrderBy(p => p.Stock),
            "featured" => descending ? products.OrderByDescending(p => p.IsFeatured) : products.OrderBy(p => p.IsFeatured),
            _ => descending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt)};
    }

    private static AdminProductResponse ToProductResponse(Product product)
    {
        return new AdminProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            BasePrice = product.BasePrice,
            Currency = product.Currency,
            RowVersion = Convert.ToBase64String(product.RowVersion),
            Price = product.Price,
            BaseSku = product.BaseSku,
            Category = product.Category,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryEntity?.Name,
            Stock = product.Stock,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            ImageUrl = product.Media.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.SortOrder).FirstOrDefault()?.Url ?? product.ImageUrl,
            VariantCount = product.Variants.Count,
            MediaCount = product.Media.Count,
            CollectionIds = product.ProductCollections.Select(pc => pc.CollectionId).ToList(),
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
