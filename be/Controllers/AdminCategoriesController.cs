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
    [Route("api/v1/admin/categories")]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AdminCategoriesController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories([FromQuery] bool includeInactive = true)
        {
            var query = _context.Categories.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var categories = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            return Ok(categories.Select(ToResponse).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return category == null ? NotFound() : Ok(ToResponse(category));
        }

        [HttpPost]
        public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryRequest request)
        {
            var validation = await ValidateAsync(request);
            if (validation != null)
            {
                return validation;
            }

            var category = new Models.Category
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
            await _auditService.RecordAsync("create", nameof(Models.Category), category.Id.ToString(), null, ToResponse(category), HttpContext);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, ToResponse(category));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, CategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var validation = await ValidateAsync(request, id);
            if (validation != null)
            {
                return validation;
            }

            category.Name = request.Name.Trim();
            category.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, id);
            category.Description = request.Description?.Trim();
            category.ParentCategoryId = request.ParentCategoryId;
            category.ImageMediaId = request.ImageMediaId;
            category.SortOrder = request.SortOrder;
            category.IsActive = request.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("update", nameof(Models.Category), id.ToString(), null, ToResponse(category), HttpContext);
            return Ok(ToResponse(category));
        }

        [HttpPost("{id:int}/archive")]
        public async Task<IActionResult> ArchiveCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("archive", nameof(Models.Category), id.ToString(), null, new { category.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> ReactivateCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("reactivate", nameof(Models.Category), id.ToString(), null, new { category.IsActive }, HttpContext);
            return NoContent();
        }

        private async Task<ActionResult<CategoryResponse>?> ValidateAsync(CategoryRequest request, int? categoryId = null)
        {
            if (request.ParentCategoryId.HasValue)
            {
                if (request.ParentCategoryId == categoryId)
                {
                    ModelState.AddModelError(nameof(request.ParentCategoryId), "A category cannot be its own parent.");
                }
                else if (!await _context.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value))
                {
                    ModelState.AddModelError(nameof(request.ParentCategoryId), "Parent category does not exist.");
                }
                else if (categoryId.HasValue && await WouldCreateCycleAsync(categoryId.Value, request.ParentCategoryId.Value))
                {
                    ModelState.AddModelError(nameof(request.ParentCategoryId), "Category hierarchy cycles are not allowed.");
                }
            }

            if (request.ImageMediaId.HasValue && !await _context.ProductMedia.AnyAsync(m => m.Id == request.ImageMediaId.Value))
            {
                ModelState.AddModelError(nameof(request.ImageMediaId), "Image media does not exist.");
            }

            if (ModelState.IsValid)
            {
                return null;
            }

            return BadRequest(ModelState);
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

        private static CategoryResponse ToResponse(Models.Category category)
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
    }
}
