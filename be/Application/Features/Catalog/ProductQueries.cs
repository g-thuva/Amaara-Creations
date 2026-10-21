using be.Application.Features.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public class ProductQueries
{
    private readonly IApplicationDbContext _context;
    private readonly IOptions<CheckoutOptions> _checkoutOptions;
    public ProductQueries(IApplicationDbContext context, IOptions<CheckoutOptions> checkoutOptions)
    {
        _context = context;
        _checkoutOptions = checkoutOptions;
    }

    public async Task<ProductListResponse> GetProducts(
        string? category = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        int? categoryId = null,
        int? collectionId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStock = null,
        bool? featured = null,
        string sort = "newest")
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100 || pageNumber > 1000000 || minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice))
            throw new CommerceException("CATALOG_REQUEST_INVALID", "Invalid pagination or price range.", 400);
        if (!new[]
        {
            "newest",
            "price-asc",
            "price-desc",
            "name",
            "featured"
        }.Contains(sort))
            throw new CommerceException("CATALOG_REQUEST_INVALID", "Unsupported sort order.", 400);
        var currency = _checkoutOptions.Value.Currency;
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive && p.Currency == currency);
        var publishedCount = await query.CountAsync();
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId && p.CategoryEntity!.IsActive);
        if (collectionId.HasValue)
            query = query.Where(p => p.ProductCollections.Any(c => c.CollectionId == collectionId && c.Collection!.IsActive));
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);
        if (inStock.HasValue)
            query = query.Where(p => (p.Stock > 0) == inStock);
        if (featured.HasValue)
            query = query.Where(p => p.IsFeatured == featured);
        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        // Search by name or description
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();
        // Apply pagination
        var sorted = sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            "featured" => query.OrderByDescending(p => p.IsFeatured),
            _ => query.OrderByDescending(p => p.CreatedAt)};
        var products = await sorted.ThenBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse { Id = p.Id, Name = p.Name, Slug = p.Slug, Price = p.Price, BasePrice = p.BasePrice, Currency = p.Currency, Description = p.Description, ShortDescription = p.ShortDescription, ImageUrl = p.ImageUrl, Category = p.Category, CategoryId = p.CategoryId, BaseSku = p.BaseSku, Stock = p.Stock, IsActive = p.IsActive, IsFeatured = p.IsFeatured, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt })
            .ToListAsync();
        var response = new ProductListResponse
        {
            Products = products,
            Currency = currency,
            TotalCount = totalCount,
            PublishedCount = publishedCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
        return response;
    }

    public async Task<ProductResponse> GetProduct(int id)
    {
        var currency = _checkoutOptions.Value.Currency;
        var product = await _context.Products.AsNoTracking()
            .Where(p => p.Id == id && p.IsActive && p.Currency == currency)
            .Select(p => new ProductResponse { Id = p.Id, Name = p.Name, Slug = p.Slug, Price = p.Price, BasePrice = p.BasePrice, Currency = p.Currency, Description = p.Description, ShortDescription = p.ShortDescription, ImageUrl = p.ImageUrl, Category = p.Category, CategoryId = p.CategoryId, BaseSku = p.BaseSku, Stock = p.Stock, IsActive = p.IsActive, IsFeatured = p.IsFeatured, Media = p.Media.Where(m => m.MediaType == "image")
                .OrderByDescending(m => m.IsPrimary)
                .ThenBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .Select(m => new StorefrontMediaResponse { Id = m.Id, Url = m.Url ?? m.StorageKey, AltText = m.AltText, Width = m.Width, Height = m.Height, IsPrimary = m.IsPrimary })
                .ToList(), Variants = p.Variants.Where(v => v.IsActive)
                .OrderBy(v => v.Name)
                .ThenBy(v => v.Id)
                .Select(v => new StorefrontVariantResponse { Id = v.Id, Name = v.Name, PriceOverride = v.PriceOverride, StockQuantity = v.StockQuantity })
                .ToList(), CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt })
            .FirstOrDefaultAsync();
        if (product == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Product not found", 404);
        }

        return product;
    }
}
