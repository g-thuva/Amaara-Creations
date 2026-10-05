using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Product;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/v1/products
        [HttpGet]
        public async Task<ActionResult<ProductListResponse>> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? collectionId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] bool? featured = null,
            [FromQuery] string sort = "newest")
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1 || pageSize > 100 || pageNumber > 1000000 ||
                    minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice))
                    return BadRequest(new { message = "Invalid pagination or price range." });
                if (!new[] { "newest", "price-asc", "price-desc", "name", "featured" }.Contains(sort))
                    return BadRequest(new { message = "Unsupported sort order." });
                var query = _context.Products.AsNoTracking().Where(p => p.IsActive);
                if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId && p.CategoryEntity!.IsActive);
                if (collectionId.HasValue) query = query.Where(p => p.ProductCollections.Any(c => c.CollectionId == collectionId && c.Collection!.IsActive));
                if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
                if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
                if (inStock.HasValue) query = query.Where(p => (p.Stock > 0) == inStock);
                if (featured.HasValue) query = query.Where(p => p.IsFeatured == featured);

                // Filter by category
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());
                }

                // Search by name or description
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.Name.Contains(search) || 
                        p.Description.Contains(search));
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
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };
                var products = await sorted.ThenBy(p => p.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.Price,
                        BasePrice = p.BasePrice,
                        Description = p.Description,
                        ShortDescription = p.ShortDescription,
                        ImageUrl = p.ImageUrl,
                        Category = p.Category,
                        CategoryId = p.CategoryId,
                        BaseSku = p.BaseSku,
                        Stock = p.Stock,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                var response = new ProductListResponse
                {
                    Products = products,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { message = "An error occurred while retrieving products" });
            }
        }

        // GET: api/v1/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == id && p.IsActive)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.Price,
                        BasePrice = p.BasePrice,
                        Description = p.Description,
                        ShortDescription = p.ShortDescription,
                        ImageUrl = p.ImageUrl,
                        Category = p.Category,
                        CategoryId = p.CategoryId,
                        BaseSku = p.BaseSku,
                        Stock = p.Stock,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        Media = p.Media.Where(m => m.MediaType == "image").OrderByDescending(m => m.IsPrimary).ThenBy(m => m.SortOrder).ThenBy(m => m.Id)
                            .Select(m => new StorefrontMediaResponse { Id = m.Id, Url = m.Url ?? m.StorageKey, AltText = m.AltText, Width = m.Width, Height = m.Height, IsPrimary = m.IsPrimary }).ToList(),
                        Variants = p.Variants.Where(v => v.IsActive).OrderBy(v => v.Name).ThenBy(v => v.Id)
                            .Select(v => new StorefrontVariantResponse { Id = v.Id, Name = v.Name, PriceOverride = v.PriceOverride, StockQuantity = v.StockQuantity }).ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the product" });
            }
        }
    }
}

