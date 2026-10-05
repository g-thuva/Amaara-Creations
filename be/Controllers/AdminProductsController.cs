using System.Security.Claims;
using System.Text.RegularExpressions;
using be.Data;
using be.DTOs.Admin;
using be.DTOs.Common;
using be.Models;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/admin/products")]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    public class AdminProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _mediaStorage;
        private readonly IAuditService _auditService;

        public AdminProductsController(ApplicationDbContext context, IMediaStorageService mediaStorage, IAuditService auditService)
        {
            _context = context;
            _mediaStorage = mediaStorage;
            _auditService = auditService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AdminProductResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<AdminProductResponse>>> GetProducts([FromQuery] AdminProductQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, PaginationQuery.MaxPageSize);
            var products = _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.CategoryEntity)
                .Include(p => p.ProductCollections)
                .Include(p => p.Media)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                products = products.Where(p => p.Name.Contains(search) || p.Slug.Contains(search) || (p.BaseSku != null && p.BaseSku.Contains(search)) || p.Description.Contains(search));
            }

            if (query.CategoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == query.CategoryId.Value);
            }

            if (query.CollectionId.HasValue)
            {
                products = products.Where(p => p.ProductCollections.Any(pc => pc.CollectionId == query.CollectionId.Value));
            }

            if (query.IsActive.HasValue)
            {
                products = products.Where(p => p.IsActive == query.IsActive.Value);
            }

            if (query.IsFeatured.HasValue)
            {
                products = products.Where(p => p.IsFeatured == query.IsFeatured.Value);
            }

            products = ApplySort(products, query.SortBy, query.SortDirection);
            var total = await products.CountAsync();
            var productItems = await products.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var items = productItems.Select(ToProductResponse).ToList();
            return Ok(PagedResult<AdminProductResponse>.Create(items, page, pageSize, total));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminProductResponse>> GetProduct(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.CategoryEntity)
                .Include(p => p.ProductCollections)
                .Include(p => p.Media)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? NotFound() : Ok(ToProductResponse(product));
        }

        [HttpPost]
        [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AdminProductResponse>> CreateProduct(AdminProductRequest request)
        {
            var validation = await ValidateProductRequestAsync(request);
            if (validation != null)
            {
                return validation;
            }

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = request.Name.Trim(),
                Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name),
                Description = request.Description.Trim(),
                ShortDescription = request.ShortDescription?.Trim(),
                Price = request.BasePrice,
                BasePrice = request.BasePrice,
                BaseSku = string.IsNullOrWhiteSpace(request.BaseSku) ? null : request.BaseSku.Trim(),
                CategoryId = request.CategoryId,
                Category = await ResolveCategoryLabelAsync(request.CategoryId, request.Category),
                Stock = request.Stock,
                ImageUrl = string.Empty,
                IsActive = request.IsActive,
                IsFeatured = request.IsFeatured,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var collectionId in request.CollectionIds.Distinct())
            {
                product.ProductCollections.Add(new ProductCollection { CollectionId = collectionId });
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("create", nameof(Product), product.Id.ToString(), null, ToProductResponse(product), HttpContext);

            var savedProduct = await LoadProductAsync(product.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToProductResponse(savedProduct!));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminProductResponse>> UpdateProduct(int id, AdminProductRequest request)
        {
            var product = await _context.Products.Include(p => p.ProductCollections).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var validation = await ValidateProductRequestAsync(request, product.Id);
            if (validation != null)
            {
                return validation;
            }

            var oldValues = new { product.Name, product.Slug, product.BasePrice, product.Stock, product.IsActive, product.IsFeatured };
            product.Name = request.Name.Trim();
            product.Slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Name, product.Id);
            product.Description = request.Description.Trim();
            product.ShortDescription = request.ShortDescription?.Trim();
            product.Price = request.BasePrice;
            product.BasePrice = request.BasePrice;
            product.BaseSku = string.IsNullOrWhiteSpace(request.BaseSku) ? null : request.BaseSku.Trim();
            product.CategoryId = request.CategoryId;
            product.Category = await ResolveCategoryLabelAsync(request.CategoryId, request.Category);
            product.Stock = request.Stock;
            product.IsActive = request.IsActive;
            product.IsFeatured = request.IsFeatured;
            product.UpdatedAt = DateTime.UtcNow;

            product.ProductCollections.Clear();
            foreach (var collectionId in request.CollectionIds.Distinct())
            {
                product.ProductCollections.Add(new ProductCollection { ProductId = product.Id, CollectionId = collectionId });
            }

            await _context.SaveChangesAsync();
            var savedProduct = await LoadProductAsync(product.Id);
            await _auditService.RecordAsync("update", nameof(Product), product.Id.ToString(), oldValues, ToProductResponse(savedProduct!), HttpContext);
            return Ok(ToProductResponse(savedProduct!));
        }

        [HttpPost("{id:int}/archive")]
        public async Task<IActionResult> ArchiveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("archive", nameof(Product), id.ToString(), null, new { product.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> ReactivateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("reactivate", nameof(Product), id.ToString(), null, new { product.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpGet("{id:int}/variants")]
        public async Task<ActionResult<IReadOnlyList<ProductVariantResponse>>> GetVariants(int id)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            return Ok(await _context.ProductVariants.AsNoTracking().Where(v => v.ProductId == id).OrderBy(v => v.Name).Select(v => ToVariantResponse(v)).ToListAsync());
        }

        [HttpPost("{id:int}/variants")]
        public async Task<ActionResult<ProductVariantResponse>> CreateVariant(int id, ProductVariantRequest request)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            if (await _context.ProductVariants.AnyAsync(v => v.Sku == request.Sku))
            {
                ModelState.AddModelError(nameof(request.Sku), "Variant SKU must be unique.");
                return ValidationProblem(ModelState);
            }

            var variant = new ProductVariant
            {
                ProductId = id,
                Sku = request.Sku.Trim(),
                Name = request.Name.Trim(),
                PriceOverride = request.PriceOverride,
                StockQuantity = request.StockQuantity,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("create", nameof(ProductVariant), variant.Id.ToString(), null, ToVariantResponse(variant), HttpContext);
            return CreatedAtAction(nameof(GetVariants), new { id }, ToVariantResponse(variant));
        }

        [HttpPut("{productId:int}/variants/{variantId:int}")]
        public async Task<ActionResult<ProductVariantResponse>> UpdateVariant(int productId, int variantId, ProductVariantRequest request)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant == null)
            {
                return NotFound();
            }

            if (request.RowVersion != null)
            {
                _context.Entry(variant).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }

            if (await _context.ProductVariants.AnyAsync(v => v.Sku == request.Sku && v.Id != variantId))
            {
                ModelState.AddModelError(nameof(request.Sku), "Variant SKU must be unique.");
                return ValidationProblem(ModelState);
            }

            variant.Sku = request.Sku.Trim();
            variant.Name = request.Name.Trim();
            variant.PriceOverride = request.PriceOverride;
            variant.StockQuantity = request.StockQuantity;
            variant.IsActive = request.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new ProblemDetails { Title = "Variant was modified by another request.", Status = StatusCodes.Status409Conflict });
            }

            await _auditService.RecordAsync("update", nameof(ProductVariant), variant.Id.ToString(), null, ToVariantResponse(variant), HttpContext);
            return Ok(ToVariantResponse(variant));
        }

        [HttpPost("{productId:int}/variants/{variantId:int}/archive")]
        public async Task<IActionResult> ArchiveVariant(int productId, int variantId)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant == null)
            {
                return NotFound();
            }

            variant.IsActive = false;
            variant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("archive", nameof(ProductVariant), variant.Id.ToString(), null, new { variant.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPost("{productId:int}/variants/{variantId:int}/reactivate")]
        public async Task<IActionResult> ReactivateVariant(int productId, int variantId)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant == null)
            {
                return NotFound();
            }

            variant.IsActive = true;
            variant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("reactivate", nameof(ProductVariant), variant.Id.ToString(), null, new { variant.IsActive }, HttpContext);
            return NoContent();
        }

        [HttpPost("{id:int}/stock-adjustments")]
        public async Task<IActionResult> AdjustProductStock(int id, StockAdjustmentRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var newQuantity = product.Stock + request.QuantityDelta;
            if (newQuantity < 0)
            {
                ModelState.AddModelError(nameof(request.QuantityDelta), "Stock adjustment cannot make stock negative.");
                return ValidationProblem(ModelState);
            }

            var previous = product.Stock;
            product.Stock = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                PreviousQuantity = previous,
                QuantityDelta = request.QuantityDelta,
                NewQuantity = newQuantity,
                Reason = request.Reason.Trim(),
                PerformedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("stock-adjust", nameof(Product), id.ToString(), new { Stock = previous }, new { Stock = newQuantity }, HttpContext);
            return NoContent();
        }

        [HttpGet("{id:int}/media")]
        public async Task<ActionResult<IReadOnlyList<ProductMediaResponse>>> GetProductMedia(int id)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            var media = await _context.ProductMedia.AsNoTracking().Where(m => m.ProductId == id).OrderBy(m => m.SortOrder).Select(m => ToMediaResponse(m)).ToListAsync();
            return Ok(media);
        }

        [HttpPost("{id:int}/media")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductMediaResponse>> UploadProductMedia(int id, IFormFile file, [FromForm] string? altText, [FromForm] int sortOrder = 0, [FromForm] bool isPrimary = false)
        {
            var product = await _context.Products.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            StoredMedia stored;
            try
            {
                stored = await _mediaStorage.UploadAsync(file, "products", HttpContext.RequestAborted);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status400BadRequest });
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
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
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
            await _auditService.RecordAsync("upload", nameof(ProductMedia), media.Id.ToString(), null, ToMediaResponse(media), HttpContext);
            return CreatedAtAction(nameof(GetProductMedia), new { id }, ToMediaResponse(media));
        }

        [HttpPut("{productId:int}/media/{mediaId:int}")]
        public async Task<ActionResult<ProductMediaResponse>> UpdateProductMedia(int productId, int mediaId, ProductMediaUpdateRequest request)
        {
            var media = await _context.ProductMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.ProductId == productId);
            if (media == null)
            {
                return NotFound();
            }

            if (request.AltText != null) media.AltText = request.AltText;
            if (request.SortOrder.HasValue) media.SortOrder = request.SortOrder.Value;
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
            await _auditService.RecordAsync("update", nameof(ProductMedia), media.Id.ToString(), null, ToMediaResponse(media), HttpContext);
            return Ok(ToMediaResponse(media));
        }

        [HttpDelete("{productId:int}/media/{mediaId:int}")]
        public async Task<IActionResult> RemoveProductMedia(int productId, int mediaId)
        {
            var media = await _context.ProductMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.ProductId == productId);
            if (media == null)
            {
                return NotFound();
            }

            var response = ToMediaResponse(media);
            var storageKey = media.StorageKey;
            if (media.IsPrimary)
            {
                var nextPrimary = await _context.ProductMedia
                    .Where(m => m.ProductId == productId && m.Id != mediaId)
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
            await _context.SaveChangesAsync();
            await _mediaStorage.DeleteAsync(storageKey, HttpContext.RequestAborted);
            await _auditService.RecordAsync("remove", nameof(ProductMedia), media.Id.ToString(), response, null, HttpContext);
            return NoContent();
        }

        private async Task<Product?> LoadProductAsync(int id)
        {
            return await _context.Products
                .AsSplitQuery()
                .Include(p => p.CategoryEntity)
                .Include(p => p.ProductCollections)
                .Include(p => p.Media)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<ActionResult<AdminProductResponse>?> ValidateProductRequestAsync(AdminProductRequest request, int? productId = null)
        {
            if (request.CategoryId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == request.CategoryId.Value))
            {
                ModelState.AddModelError(nameof(request.CategoryId), "Category does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(request.BaseSku) && await _context.Products.AnyAsync(p => p.BaseSku == request.BaseSku && (!productId.HasValue || p.Id != productId.Value)))
            {
                ModelState.AddModelError(nameof(request.BaseSku), "Product SKU must be unique.");
            }

            var distinctCollectionIds = request.CollectionIds.Distinct().ToList();
            if (distinctCollectionIds.Count != request.CollectionIds.Count)
            {
                ModelState.AddModelError(nameof(request.CollectionIds), "Duplicate collection assignments are not allowed.");
            }

            var existingCollectionCount = await _context.Collections.CountAsync(c => distinctCollectionIds.Contains(c.Id));
            if (existingCollectionCount != distinctCollectionIds.Count)
            {
                ModelState.AddModelError(nameof(request.CollectionIds), "One or more collections do not exist.");
            }

            if (ModelState.IsValid)
            {
                return null;
            }

            return BadRequest(ModelState);
        }

        private async Task<string> ResolveCategoryLabelAsync(int? categoryId, string? fallback)
        {
            if (categoryId.HasValue)
            {
                return await _context.Categories.Where(c => c.Id == categoryId.Value).Select(c => c.Slug).FirstOrDefaultAsync() ?? "custom";
            }

            return string.IsNullOrWhiteSpace(fallback) ? "custom" : fallback.Trim();
        }

        private async Task<string> GenerateUniqueSlugAsync(string value, int? existingProductId = null)
        {
            var slug = Slugify(value);
            var candidate = slug;
            var suffix = 2;
            while (await _context.Products.AnyAsync(p => p.Slug == candidate && (!existingProductId.HasValue || p.Id != existingProductId.Value)))
            {
                candidate = $"{slug}-{suffix++}";
            }

            return candidate;
        }

        private static string Slugify(string value)
        {
            var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}"[..20] : slug;
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sortBy, string? direction)
        {
            var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
            return (sortBy ?? "createdAt").ToLowerInvariant() switch
            {
                "name" => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
                "updatedat" => descending ? products.OrderByDescending(p => p.UpdatedAt) : products.OrderBy(p => p.UpdatedAt),
                "baseprice" or "price" => descending ? products.OrderByDescending(p => p.BasePrice) : products.OrderBy(p => p.BasePrice),
                "stock" => descending ? products.OrderByDescending(p => p.Stock) : products.OrderBy(p => p.Stock),
                "featured" => descending ? products.OrderByDescending(p => p.IsFeatured) : products.OrderBy(p => p.IsFeatured),
                _ => descending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt)
            };
        }

        private static AdminProductResponse ToProductResponse(Product product)
        {
            return new AdminProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                ShortDescription = product.ShortDescription,
                BasePrice = product.BasePrice,
                Price = product.Price,
                BaseSku = product.BaseSku,
                Category = product.Category,
                CategoryId = product.CategoryId,
                CategoryName = product.CategoryEntity?.Name,
                Stock = product.Stock,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                ImageUrl = product.Media.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.SortOrder).FirstOrDefault()?.Url ?? product.ImageUrl,
                VariantCount = product.Variants.Count,
                MediaCount = product.Media.Count,
                CollectionIds = product.ProductCollections.Select(pc => pc.CollectionId).ToList(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        private static ProductVariantResponse ToVariantResponse(ProductVariant variant)
        {
            return new ProductVariantResponse
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Sku = variant.Sku,
                Name = variant.Name,
                PriceOverride = variant.PriceOverride,
                StockQuantity = variant.StockQuantity,
                IsActive = variant.IsActive,
                RowVersion = Convert.ToBase64String(variant.RowVersion),
                CreatedAt = variant.CreatedAt,
                UpdatedAt = variant.UpdatedAt
            };
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
}
