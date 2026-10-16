using be.Application.Abstractions.Audit;
using be.Domain.Cms;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Cms;
public class CmsManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    public CmsManagementService(IApplicationDbContext context, IAuditService auditService, IRequestActor actor)
    {
        _context = context;
        _auditService = auditService;
        _actor = actor;
    }

    public async Task<IReadOnlyList<CmsPageResponse>> GetPages()
    {
        var pages = await _context.CmsPages.AsNoTracking().Include(p => p.Sections).OrderBy(p => p.Title).ToListAsync();
        return pages.Select(ToResponse).ToList();
    }

    public async Task<CmsPageResponse> GetPage(int id)
    {
        var page = await _context.CmsPages.AsNoTracking().Include(p => p.Sections).FirstOrDefaultAsync(p => p.Id == id);
        return page == null ? throw new CommerceException("CMS_NOT_FOUND", "Resource not found.", 404) : ToResponse(page);
    }

    public async Task<CmsPageResponse> CreatePage(CmsPageRequest request)
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
        await _auditService.RecordAsync("create", nameof(CmsPage), page.Id.ToString(), null, ToResponse(page), _actor.Audit);
        return ToResponse(page);
    }

    public async Task<CmsPageResponse> UpdatePage(int id, CmsPageRequest request)
    {
        var page = await _context.CmsPages.Include(p => p.Sections).FirstOrDefaultAsync(p => p.Id == id);
        if (page == null)
        {
            throw new CommerceException("CMS_NOT_FOUND", "Resource not found.", 404);
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
        await _auditService.RecordAsync("update", nameof(CmsPage), id.ToString(), null, ToResponse(page), _actor.Audit);
        return ToResponse(page);
    }

    public async Task PublishPage(int id)
    {
        var page = await _context.CmsPages.FindAsync(id);
        if (page == null)
        {
            throw new CommerceException("CMS_NOT_FOUND", "Resource not found.", 404);
        }

        page.IsPublished = true;
        page.PublishedAt ??= DateTime.UtcNow;
        page.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("publish", nameof(CmsPage), id.ToString(), null, new { page.IsPublished }, _actor.Audit);
        return;
    }

    public async Task UnpublishPage(int id)
    {
        var page = await _context.CmsPages.FindAsync(id);
        if (page == null)
        {
            throw new CommerceException("CMS_NOT_FOUND", "Resource not found.", 404);
        }

        page.IsPublished = false;
        page.PublishedAt = null;
        page.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("unpublish", nameof(CmsPage), id.ToString(), null, new { page.IsPublished }, _actor.Audit);
        return;
    }

    private static void ApplySections(CmsPage page, IEnumerable<CmsSectionRequest> requests)
    {
        foreach (var section in requests.GroupBy(s => s.SectionKey.Trim()).Select(g => g.First()))
        {
            page.Sections.Add(new CmsSection { SectionKey = section.SectionKey.Trim(), ContentType = section.ContentType.Trim(), Content = section.Content.Trim(), SortOrder = section.SortOrder, IsActive = section.IsActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
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
            Sections = page.Sections.OrderBy(s => s.SortOrder)
                .Select(s => new CmsSectionResponse { Id = s.Id, CmsPageId = s.CmsPageId, SectionKey = s.SectionKey, ContentType = s.ContentType, Content = s.Content, SortOrder = s.SortOrder, IsActive = s.IsActive })
                .ToList()
        };
    }

    private readonly IRequestActor _actor;
}
