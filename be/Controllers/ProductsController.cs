using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Product;
using be.Models;
using be.Security;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<ProductListResponse>> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Products.Where(p => p.IsActive);

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
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
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

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
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

        // POST: api/products (Admin only)
        [HttpPost]
        [Authorize(Policy = AppPolicies.ManageCatalog)]
        public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = new Product
                {
                    Name = request.Name,
                    Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name),
                    Price = request.Price,
                    BasePrice = request.BasePrice ?? request.Price,
                    Description = request.Description,
                    ShortDescription = request.ShortDescription,
                    ImageUrl = request.ImageUrl,
                    Category = request.Category,
                    CategoryId = request.CategoryId,
                    BaseSku = request.BaseSku,
                    Stock = request.Stock,
                    IsActive = true,
                    IsFeatured = request.IsFeatured,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var response = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Slug = product.Slug,
                    Price = product.Price,
                    BasePrice = product.BasePrice,
                    Description = product.Description,
                    ShortDescription = product.ShortDescription,
                    ImageUrl = product.ImageUrl,
                    Category = product.Category,
                    CategoryId = product.CategoryId,
                    BaseSku = product.BaseSku,
                    Stock = product.Stock,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "An error occurred while creating the product" });
            }
        }

        // PUT: api/products/{id} (Admin only)
        [HttpPut("{id}")]
        [Authorize(Policy = AppPolicies.ManageCatalog)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Update only provided fields
                if (request.Name != null) product.Name = request.Name;
                if (request.Slug != null) product.Slug = await GenerateUniqueSlugAsync(request.Slug, product.Id);
                else if (string.IsNullOrWhiteSpace(product.Slug) && request.Name != null) product.Slug = await GenerateUniqueSlugAsync(request.Name, product.Id);
                if (request.Price.HasValue) product.Price = request.Price.Value;
                if (request.BasePrice.HasValue) product.BasePrice = request.BasePrice.Value;
                if (request.Description != null) product.Description = request.Description;
                if (request.ShortDescription != null) product.ShortDescription = request.ShortDescription;
                if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
                if (request.Category != null) product.Category = request.Category;
                if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
                if (request.BaseSku != null) product.BaseSku = request.BaseSku;
                if (request.Stock.HasValue) product.Stock = request.Stock.Value;
                if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
                if (request.IsFeatured.HasValue) product.IsFeatured = request.IsFeatured.Value;

                product.UpdatedAt = DateTime.UtcNow;
                if (product.BasePrice <= 0)
                {
                    product.BasePrice = product.Price;
                }
                if (string.IsNullOrWhiteSpace(product.Slug))
                {
                    product.Slug = await GenerateUniqueSlugAsync(product.Name, product.Id);
                }

                await _context.SaveChangesAsync();

                var response = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Slug = product.Slug,
                    Price = product.Price,
                    BasePrice = product.BasePrice,
                    Description = product.Description,
                    ShortDescription = product.ShortDescription,
                    ImageUrl = product.ImageUrl,
                    Category = product.Category,
                    CategoryId = product.CategoryId,
                    BaseSku = product.BaseSku,
                    Stock = product.Stock,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the product" });
            }
        }

        // DELETE: api/products/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Policy = AppPolicies.ManageCatalog)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Soft delete (set IsActive to false) instead of hard delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the product" });
            }
        }

        private async Task<string> GenerateUniqueSlugAsync(string value, int? existingProductId = null)
        {
            var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = $"product-{Guid.NewGuid():N}"[..20];
            }

            var candidate = slug;
            var suffix = 2;
            while (await _context.Products.AnyAsync(p => p.Slug == candidate && (!existingProductId.HasValue || p.Id != existingProductId.Value)))
            {
                candidate = $"{slug}-{suffix++}";
            }

            return candidate;
        }
    }
}

