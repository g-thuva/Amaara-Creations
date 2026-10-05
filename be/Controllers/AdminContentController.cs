using System.Text.RegularExpressions;
using be.Data;
using be.DTOs.Admin;
using be.Models;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/admin/content/pages")]
    [Authorize(Policy = AppPolicies.ManageContent)]
    public class AdminContentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AdminContentController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CmsPageResponse>>> GetPages()
        {
            var pages = await _context.CmsPages.AsNoTracking().Include(p => p.Sections).OrderBy(p => p.Title).ToListAsync();
            return Ok(pages.Select(ToResponse).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CmsPageResponse>> GetPage(int id)
        {
            var page = await _context.CmsPages.AsNoTracking().Include(p => p.Sections).FirstOrDefaultAsync(p => p.Id == id);
            return page == null ? NotFound() : Ok(ToResponse(page));
        }

        [HttpPost]
        public async Task<ActionResult<CmsPageResponse>> CreatePage(CmsPageRequest request)
        {
            var page = new CmsPage
            {
                Title = request.Title.Trim(),
                Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Title),
                Summary = request.Summary?.Trim(),
                IsPublished = request.IsPublished,
                PublishedAt = request.IsPublished ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ApplySections(page, request.Sections);
            _context.CmsPages.Add(page);
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("create", nameof(CmsPage), page.Id.ToString(), null, ToResponse(page), HttpContext);
            return CreatedAtAction(nameof(GetPage), new { id = page.Id }, ToResponse(page));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CmsPageResponse>> UpdatePage(int id, CmsPageRequest request)
        {
            var page = await _context.CmsPages.Include(p => p.Sections).FirstOrDefaultAsync(p => p.Id == id);
            if (page == null)
            {
                return NotFound();
            }

            page.Title = request.Title.Trim();
            page.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Title, id);
            page.Summary = request.Summary?.Trim();
            if (request.IsPublished && !page.IsPublished)
            {
                page.PublishedAt = DateTime.UtcNow;
            }
            else if (!request.IsPublished)
            {
                page.PublishedAt = null;
            }

            page.IsPublished = request.IsPublished;
            page.UpdatedAt = DateTime.UtcNow;
            page.Sections.Clear();
            ApplySections(page, request.Sections);
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("update", nameof(CmsPage), id.ToString(), null, ToResponse(page), HttpContext);
            return Ok(ToResponse(page));
        }

        [HttpPost("{id:int}/publish")]
        public async Task<IActionResult> PublishPage(int id)
        {
            var page = await _context.CmsPages.FindAsync(id);
            if (page == null)
            {
                return NotFound();
            }

            page.IsPublished = true;
            page.PublishedAt ??= DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("publish", nameof(CmsPage), id.ToString(), null, new { page.IsPublished }, HttpContext);
            return NoContent();
        }

        [HttpPost("{id:int}/unpublish")]
        public async Task<IActionResult> UnpublishPage(int id)
        {
            var page = await _context.CmsPages.FindAsync(id);
            if (page == null)
            {
                return NotFound();
            }

            page.IsPublished = false;
            page.PublishedAt = null;
            page.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("unpublish", nameof(CmsPage), id.ToString(), null, new { page.IsPublished }, HttpContext);
            return NoContent();
        }

        private static void ApplySections(CmsPage page, IEnumerable<CmsSectionRequest> requests)
        {
            foreach (var section in requests.GroupBy(s => s.SectionKey.Trim()).Select(g => g.First()))
            {
                page.Sections.Add(new CmsSection
                {
                    SectionKey = section.SectionKey.Trim(),
                    ContentType = section.ContentType.Trim(),
                    Content = section.Content.Trim(),
                    SortOrder = section.SortOrder,
                    IsActive = section.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task<string> GenerateUniqueSlugAsync(string value, int? pageId = null)
        {
            var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = $"page-{Guid.NewGuid():N}"[..20];
            }

            var candidate = slug;
            var suffix = 2;
            while (await _context.CmsPages.AnyAsync(p => p.Slug == candidate && (!pageId.HasValue || p.Id != pageId.Value)))
            {
                candidate = $"{slug}-{suffix++}";
            }

            return candidate;
        }

        private static CmsPageResponse ToResponse(CmsPage page)
        {
            return new CmsPageResponse
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Summary = page.Summary,
                IsPublished = page.IsPublished,
                PublishedAt = page.PublishedAt,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt,
                Sections = page.Sections.OrderBy(s => s.SortOrder).Select(s => new CmsSectionResponse
                {
                    Id = s.Id,
                    CmsPageId = s.CmsPageId,
                    SectionKey = s.SectionKey,
                    ContentType = s.ContentType,
                    Content = s.Content,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive
                }).ToList()
            };
        }
    }
}
