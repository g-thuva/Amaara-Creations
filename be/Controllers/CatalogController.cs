using be.Data;
using be.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/catalog")]
    public class CatalogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ParentCategoryId = c.ParentCategoryId,
                    ImageMediaId = c.ImageMediaId,
                    ImageUrl = c.ImageMedia != null ? c.ImageMedia.Url ?? c.ImageMedia.StorageKey : null,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet("collections")]
        public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> GetCollections()
        {
            var collections = await _context.Collections
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CollectionResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    ProductCount = c.ProductCollections.Count(p => p.Product!.IsActive),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
            return Ok(collections);
        }
    }
}
