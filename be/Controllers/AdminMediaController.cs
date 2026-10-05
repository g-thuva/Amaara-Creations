using be.Data;
using be.DTOs.Admin;
using be.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/admin/media")]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    public class AdminMediaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminMediaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductMediaResponse>>> GetMedia([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.ProductMedia.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => (m.OriginalFileName != null && m.OriginalFileName.Contains(search)) || (m.AltText != null && m.AltText.Contains(search)));
            }

            var media = await query.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(media.Select(ToResponse).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductMediaResponse>> GetMediaById(int id)
        {
            var media = await _context.ProductMedia.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            return media == null ? NotFound() : Ok(ToResponse(media));
        }

        private static ProductMediaResponse ToResponse(Models.ProductMedia media)
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
}
