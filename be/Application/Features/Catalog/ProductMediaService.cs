using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Storage;
using be.Domain.Catalog;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Catalog;
public sealed class ProductMediaService(IApplicationDbContext _context, IMediaStorageService _mediaStorage, IAuditService _auditService, IRequestActor _actor, IMediaCleanupQueue? cleanup = null)
{
    public async Task<IReadOnlyList<ProductMediaResponse>> GetProductMedia(int id)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == id))
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        var media = await _context.ProductMedia.AsNoTracking().Where(m => m.ProductId == id).OrderBy(m => m.SortOrder).Select(m => ToMediaResponse(m)).ToListAsync();
        return media;
    }

    public async Task<ProductMediaResponse> UploadProductMedia(int id, MediaUpload file, string? altText, int sortOrder = 0, bool isPrimary = false)
    {
        var product = await _context.Products.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        StoredMedia stored;
        try
        {
            stored = await _mediaStorage.UploadAsync(file, "products", _actor.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            throw new CommerceException("CATALOG_REQUEST_INVALID", ex.Message, 400);
        }

        if (isPrimary)
        {
            foreach (var existing in product.Media)
            {
                existing.IsPrimary = false;
            }
        }

        var media = new ProductMedia
        {
            ProductId = product.Id,
            StorageKey = stored.StorageKey,
            Url = stored.PublicUrl,
            MediaType = "image",
            AltText = altText,
            OriginalFileName = stored.OriginalFileName,
            ContentType = stored.ContentType,
            FileSize = stored.FileSize,
            Width = stored.Width,
            Height = stored.Height,
            StorageProvider = stored.StorageProvider,
            CreatedByUserId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier),
            SortOrder = sortOrder,
            IsPrimary = isPrimary || !product.Media.Any(),
            CreatedAt = DateTime.UtcNow
        };
        _context.ProductMedia.Add(media);
        if (media.IsPrimary)
        {
            product.ImageUrl = media.Url ?? string.Empty;
        }

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("upload", nameof(ProductMedia), media.Id.ToString(), null, ToMediaResponse(media), _actor.Audit);
        return ToMediaResponse(media);
    }

    public async Task<ProductMediaResponse> UpdateProductMedia(int productId, int mediaId, ProductMediaUpdateRequest request)
    {
        var media = await _context.ProductMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.ProductId == productId);
        if (media == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        if (request.AltText != null)
            media.AltText = request.AltText;
        if (request.SortOrder.HasValue)
            media.SortOrder = request.SortOrder.Value;
        if (request.IsPrimary == true)
        {
            var siblings = await _context.ProductMedia.Where(m => m.ProductId == productId && m.Id != mediaId).ToListAsync();
            foreach (var sibling in siblings)
            {
                sibling.IsPrimary = false;
            }

            media.IsPrimary = true;
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.ImageUrl = media.Url ?? string.Empty;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (request.IsPrimary == false)
        {
            media.IsPrimary = false;
        }

        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("update", nameof(ProductMedia), media.Id.ToString(), null, ToMediaResponse(media), _actor.Audit);
        return ToMediaResponse(media);
    }

    public async Task RemoveProductMedia(int productId, int mediaId)
    {
        var media = await _context.ProductMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.ProductId == productId);
        if (media == null)
        {
            throw new CommerceException("CATALOG_NOT_FOUND", "Resource not found.", 404);
        }

        var response = ToMediaResponse(media);
        var storageKey = media.StorageKey;
        if (media.IsPrimary)
        {
            var nextPrimary = await _context.ProductMedia.Where(m => m.ProductId == productId && m.Id != mediaId)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .FirstOrDefaultAsync();
            if (nextPrimary != null)
            {
                nextPrimary.IsPrimary = true;
            }

            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.ImageUrl = nextPrimary?.Url ?? string.Empty;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.ProductMedia.Remove(media);
        cleanup?.EnqueueDelete(storageKey);
        await _context.SaveChangesAsync();
        if (!storageKey.StartsWith("s3:", StringComparison.Ordinal)) await _mediaStorage.DeleteAsync(storageKey, _actor.RequestAborted);
        await _auditService.RecordAsync("remove", nameof(ProductMedia), media.Id.ToString(), response, null, _actor.Audit);
        return;
    }

    private static ProductMediaResponse ToMediaResponse(ProductMedia media)
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
