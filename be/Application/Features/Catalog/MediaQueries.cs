using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public class MediaQueries
{
    private readonly IApplicationDbContext _context;
    public MediaQueries(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductMediaResponse>> GetMedia(string? search = null, int page = 1, int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.ProductMedia.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => (m.OriginalFileName != null && m.OriginalFileName.Contains(search)) || (m.AltText != null && m.AltText.Contains(search)));
        }

        var media = await query.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return media.Select(ToResponse).ToList();
    }

    public async Task<ProductMediaResponse> GetMediaById(int id)
    {
        var media = await _context.ProductMedia.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        return media == null ? throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404) : ToResponse(media);
    }

    private static ProductMediaResponse ToResponse(be.Domain.Catalog.ProductMedia media)
    {
        return new ProductMediaResponse
        {
            Id = media.Id,
            ProductId = media.ProductId,
            StorageKey = media.StorageKey,
            Url = media.Url,
            MediaType = media.MediaType,
            AltText = media.AltText,
            OriginalFileName = media.OriginalFileName,
            ContentType = media.ContentType,
            FileSize = media.FileSize,
            Width = media.Width,
            Height = media.Height,
            StorageProvider = media.StorageProvider,
            SortOrder = media.SortOrder,
            IsPrimary = media.IsPrimary,
            CreatedAt = media.CreatedAt
        };
    }
}
