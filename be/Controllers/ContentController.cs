using be.Data;
using be.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/content")]
    public class ContentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("pages/{slug}")]
        public async Task<ActionResult<CmsPageResponse>> GetPublishedPage(string slug)
        {
            var page = await _context.CmsPages
                .AsNoTracking()
                .Include(p => p.Sections)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
            if (page == null)
            {
                return NotFound();
            }

            return Ok(new CmsPageResponse
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Summary = page.Summary,
                IsPublished = page.IsPublished,
                PublishedAt = page.PublishedAt,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt,
                Sections = page.Sections.Where(s => s.IsActive).OrderBy(s => s.SortOrder).Select(s => new CmsSectionResponse
                {
                    Id = s.Id,
                    CmsPageId = s.CmsPageId,
                    SectionKey = s.SectionKey,
                    ContentType = s.ContentType,
                    Content = s.Content,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive
                }).ToList()
            });
        }

        [HttpGet("settings")]
        public async Task<ActionResult<IReadOnlyList<SiteSettingResponse>>> GetPublicSettings()
        {
            return Ok(await _context.SiteSettings
                .AsNoTracking()
                .Where(s => s.IsPublic)
                .OrderBy(s => s.Key)
                .Select(s => new SiteSettingResponse
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    ValueType = s.ValueType,
                    IsPublic = s.IsPublic,
                    Description = s.Description,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync());
        }
    }
}
