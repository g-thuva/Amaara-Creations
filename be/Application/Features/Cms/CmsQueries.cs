using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;

namespace be.Application.Features.Cms;
public class CmsQueries
{
    private readonly IApplicationDbContext _context;
    public CmsQueries(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CmsPageResponse> GetPublishedPage(string slug)
    {
        var page = await _context.CmsPages.AsNoTracking().Include(p => p.Sections).FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (page == null)
        {
            throw new CommerceException("CMS_NOT_FOUND", "Resource not found.", 404);
        }

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
            Sections = page.Sections.Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new CmsSectionResponse { Id = s.Id, CmsPageId = s.CmsPageId, SectionKey = s.SectionKey, ContentType = s.ContentType, Content = s.Content, SortOrder = s.SortOrder, IsActive = s.IsActive })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<SiteSettingResponse>> GetPublicSettings()
    {
        return await _context.SiteSettings.AsNoTracking()
            .Where(s => s.IsPublic)
            .OrderBy(s => s.Key)
            .Select(s => new SiteSettingResponse { Id = s.Id, Key = s.Key, Value = s.Value, ValueType = s.ValueType, IsPublic = s.IsPublic, Description = s.Description, UpdatedAt = s.UpdatedAt })
            .ToListAsync();
    }
}
