using System.Text.RegularExpressions;
using be.Data;
using be.DTOs.Admin;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/admin/collections")]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    public class AdminCollectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AdminCollectionsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> GetCollections([FromQuery] bool includeInactive = true)
        {
            var query = _context.Collections.AsNoTracking().Include(c => c.ProductCollections).AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var collections = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            return Ok(collections.Select(ToResponse).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CollectionResponse>> GetCollection(int id)
        {
            var collection = await _context.Collections.AsNoTracking().Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
            return collection == null ? NotFound() : Ok(ToResponse(collection));
        }

        [HttpPost]
        public async Task<ActionResult<CollectionResponse>> CreateCollection(CollectionRequest request)
        {
            var collection = new Models.Collection
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
            await _auditService.RecordAsync("create", nameof(Models.Collection), collection.Id.ToString(), null, ToResponse(collection), HttpContext);
            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, ToResponse(collection));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CollectionResponse>> UpdateCollection(int id, CollectionRequest request)
        {
            var collection = await _context.Collections.Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
            if (collection == null)
            {
                return NotFound();
            }

            collection.Name = request.Name.Trim();
            collection.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, id);
            collection.Description = request.Description?.Trim();
            collection.SortOrder = request.SortOrder;
            collection.IsActive = request.IsActive;
            collection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("update", nameof(Models.Collection), id.ToString(), null, ToResponse(collection), HttpContext);
            return Ok(ToResponse(collection));
        }

        [HttpPost("{id:int}/archive")]
        public async Task<IActionResult> ArchiveCollection(int id)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            collection.IsActive = false;
            collection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("archive", nameof(Models.Collection), id.ToString(), null, new { collection.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> ReactivateCollection(int id)
        {
            var collection = await _context.Collections.FindAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            collection.IsActive = true;
            collection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("reactivate", nameof(Models.Collection), id.ToString(), null, new { collection.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPut("{id:int}/products")]
        public async Task<IActionResult> ReplaceProducts(int id, CollectionProductsRequest request)
        {
            var collection = await _context.Collections.Include(c => c.ProductCollections).FirstOrDefaultAsync(c => c.Id == id);
            if (collection == null)
            {
                return NotFound();
            }

            var productIds = request.ProductIds.Distinct().ToList();
            if (productIds.Count != request.ProductIds.Count)
            {
                ModelState.AddModelError(nameof(request.ProductIds), "Duplicate product assignments are not allowed.");
            }

            var existingCount = await _context.Products.CountAsync(p => productIds.Contains(p.Id));
            if (existingCount != productIds.Count)
            {
                ModelState.AddModelError(nameof(request.ProductIds), "One or more products do not exist.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            collection.ProductCollections.Clear();
            for (var i = 0; i < productIds.Count; i++)
            {
                collection.ProductCollections.Add(new Models.ProductCollection { CollectionId = id, ProductId = productIds[i], SortOrder = i });
            }

            collection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("assign-products", nameof(Models.Collection), id.ToString(), null, new { ProductIds = productIds }, HttpContext);
            return NoContent();
        }

        [HttpDelete("{collectionId:int}/products/{productId:int}")]
        public async Task<IActionResult> RemoveProduct(int collectionId, int productId)
        {
            var link = await _context.ProductCollections.FindAsync(productId, collectionId);
            if (link == null)
            {
                return NotFound();
            }

            _context.ProductCollections.Remove(link);
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("remove-product", nameof(Models.Collection), collectionId.ToString(), null, new { productId }, HttpContext);
            return NoContent();
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

        private static CollectionResponse ToResponse(Models.Collection collection)
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
    }
}
