using System.Security.Claims;
using System.Text.Json;
using be.Data;
using be.DTOs.CustomBuilder;
using be.Models;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Route("api/v1/custom-designs")]
[Authorize]
public class CustomDesignsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomStickerPricingService _pricing;
    private readonly IMediaStorageService _storage;

    public CustomDesignsController(ApplicationDbContext context, ICustomStickerPricingService pricing, IMediaStorageService storage)
    {
        _context = context;
        _pricing = pricing;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.CustomDesigns.Where(design => design.UserId == userId && design.Status != CustomDesignStatus.Archived);
        var totalCount = await query.CountAsync(cancellationToken);
        var designs = await IncludeDesign(query).AsNoTracking().OrderByDescending(design => design.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items = designs.Select(design => design.ToDto(false)), totalCount, page, pageSize, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomDesignDto>> Get(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, true, cancellationToken);
        return design == null ? NotFound() : Ok(design.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<CustomDesignDto>> Create([FromBody] SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null) return Problem(statusCode: 503, title: "Custom builder is not configured");
        try
        {
            ValidateEditorState(request.EditorStateJson);
            var quote = _pricing.Quote(configuration, request);
            var design = new CustomDesign { UserId = userId, CreatedAt = DateTime.UtcNow };
            ApplyDesign(design, request, configuration, quote);
            _context.CustomDesigns.Add(design);
            await _context.SaveChangesAsync(cancellationToken);
            var created = await OwnDesign(design.Id, true, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = design.Id }, created!.ToDto());
        }
        catch (CustomBuilderValidationException exception) { return BuilderValidation(exception); }
        catch (JsonException) { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["editorStateJson"] = ["Editor state must be valid JSON."] })); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomDesignDto>> Update(int id, [FromBody] SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived) return Conflict(new ProblemDetails { Title = "This design is locked", Status = 409 });
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            try { _context.Entry(design).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion); }
            catch (FormatException) { return BadRequest(new ProblemDetails { Title = "Invalid row version" }); }
        }

        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null) return Problem(statusCode: 503, title: "Custom builder is not configured");
        try
        {
            ValidateEditorState(request.EditorStateJson);
            var quote = _pricing.Quote(configuration, request);
            ApplyDesign(design, request, configuration, quote);
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(item => item.UserId == design.UserId && item.CustomDesignId == design.Id, cancellationToken);
            if (cartItem != null)
            {
                cartItem.Quantity = design.Quantity;
                cartItem.UnitPriceSnapshot = quote.UnitPrice;
                cartItem.BuilderConfigurationVersionId = configuration.Id;
                cartItem.UpdatedAt = DateTime.UtcNow;
                design.Status = CustomDesignStatus.InCart;
            }
            await _context.SaveChangesAsync(cancellationToken);
            var updated = await OwnDesign(id, true, cancellationToken);
            return Ok(updated!.ToDto());
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Title = "Design changed", Detail = "Reload the design before saving again.", Status = 409 }); }
        catch (CustomBuilderValidationException exception) { return BuilderValidation(exception); }
        catch (JsonException) { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["editorStateJson"] = ["Editor state must be valid JSON."] })); }
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<CustomDesignDto>> Duplicate(int id, CancellationToken cancellationToken)
    {
        var source = await OwnDesign(id, false, cancellationToken);
        if (source == null) return NotFound();
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null) return Problem(statusCode: 503, title: "Custom builder is not configured");
        var request = RequestFrom(source);
        try
        {
            var quote = _pricing.Quote(configuration, request);
            var copy = new CustomDesign { UserId = source.UserId, Name = $"Copy of {source.Name}", CreatedAt = DateTime.UtcNow };
            request.Name = copy.Name;
            ApplyDesign(copy, request, configuration, quote);
            _context.CustomDesigns.Add(copy);
            await _context.SaveChangesAsync(cancellationToken);
            var created = await OwnDesign(copy.Id, true, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = copy.Id }, created!.ToDto());
        }
        catch (CustomBuilderValidationException exception) { return BuilderValidation(exception); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.InCart) return Conflict(new ProblemDetails { Title = "Remove the design from the cart before archiving it", Status = 409 });
        design.Status = CustomDesignStatus.Archived; design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/artwork")]
    [EnableRateLimiting("custom-upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<ActionResult<CustomDesignAssetDto>> UploadArtwork(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived) return Conflict(new ProblemDetails { Title = "Artwork for this design is locked", Status = 409 });
        try
        {
            var stored = await _storage.UploadPrivateAsync(file, $"custom-artwork/{design.Id}", cancellationToken);
            var asset = new CustomDesignAsset
            {
                CustomDesignId = design.Id, StorageKey = stored.StorageKey, AssetType = "artwork", OriginalFileName = stored.OriginalFileName,
                ContentType = stored.ContentType, SizeBytes = stored.FileSize, Width = stored.Width, Height = stored.Height, CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignAssets.Add(asset);
            design.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new CustomDesignAssetDto { Id = asset.Id, AssetType = asset.AssetType, OriginalFileName = asset.OriginalFileName, ContentType = asset.ContentType, SizeBytes = asset.SizeBytes, Width = asset.Width, Height = asset.Height, CreatedAt = asset.CreatedAt, DownloadUrl = $"/api/v1/custom-designs/{id}/assets/{asset.Id}" });
        }
        catch (InvalidOperationException exception) { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["file"] = [exception.Message] })); }
    }

    [HttpGet("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> DownloadAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        var asset = await _context.CustomDesignAssets.Include(item => item.CustomDesign).FirstOrDefaultAsync(item => item.Id == assetId && item.CustomDesignId == id, cancellationToken);
        if (asset == null) return NotFound();
        if (!CanAccess(asset.CustomDesign?.UserId)) return NotFound();
        if (!await _storage.ExistsAsync(asset.StorageKey, cancellationToken)) return NotFound();
        var stream = await _storage.OpenReadAsync(asset.StorageKey, cancellationToken);
        return File(stream, asset.ContentType ?? "application/octet-stream", Path.GetFileName(asset.OriginalFileName ?? $"asset-{asset.Id}"), true);
    }

    [HttpDelete("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> DeleteAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived) return Conflict(new ProblemDetails { Title = "Artwork for this design is retained", Status = 409 });
        var asset = await _context.CustomDesignAssets.FirstOrDefaultAsync(item => item.Id == assetId && item.CustomDesignId == id && item.AssetType == "artwork", cancellationToken);
        if (asset == null) return NotFound();
        _context.CustomDesignAssets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(asset.StorageKey, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/cart")]
    public async Task<ActionResult> AddToCart(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, true, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived) return Conflict(new ProblemDetails { Title = "This design cannot be added to the cart", Status = 409 });
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null) return Problem(statusCode: 503, title: "Custom builder is not configured");
        if (configuration.ArtworkRequired && !design.Assets.Any(asset => asset.AssetType == "artwork"))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["artwork"] = ["Artwork is required before this design can be added to the cart."] }));
        try
        {
            var request = RequestFrom(design);
            var quote = _pricing.Quote(configuration, request);
            ApplyDesign(design, request, configuration, quote);
            design.Status = CustomDesignStatus.InCart;
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(item => item.UserId == design.UserId && item.CustomDesignId == design.Id, cancellationToken);
            if (cartItem == null)
            {
                cartItem = new CartItem { UserId = design.UserId!, ItemType = CartItemType.CustomDesign, CustomDesignId = design.Id, CreatedAt = DateTime.UtcNow };
                _context.CartItems.Add(cartItem);
            }
            cartItem.Quantity = design.Quantity; cartItem.UnitPriceSnapshot = quote.UnitPrice; cartItem.BuilderConfigurationVersionId = configuration.Id; cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { cartItemId = cartItem.Id, design = design.ToDto(false) });
        }
        catch (CustomBuilderValidationException exception) { return BuilderValidation(exception); }
    }

    [HttpPost("{id:int}/proofs/approve")]
    public async Task<ActionResult<CustomDesignDto>> ApproveProof(int id, [FromBody] ProofResponseRequest request, CancellationToken cancellationToken)
    {
        return await RespondToProof(id, request, true, cancellationToken);
    }

    [HttpPost("{id:int}/proofs/request-changes")]
    public async Task<ActionResult<CustomDesignDto>> RequestProofChanges(int id, [FromBody] ProofResponseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["comment"] = ["Describe the changes you need."] }));
        return await RespondToProof(id, request, false, cancellationToken);
    }

    [HttpGet("{id:int}/proofs/{revision:int}/file")]
    public async Task<IActionResult> DownloadProof(int id, int revision, CancellationToken cancellationToken)
    {
        var proof = await _context.CustomDesignProofRevisions.Include(item => item.CustomDesign).Include(item => item.Asset)
            .FirstOrDefaultAsync(item => item.CustomDesignId == id && item.RevisionNumber == revision, cancellationToken);
        if (proof?.Asset == null) return NotFound();
        if (!CanAccess(proof.CustomDesign?.UserId)) return NotFound();
        var stream = await _storage.OpenReadAsync(proof.Asset.StorageKey, cancellationToken);
        return File(stream, proof.Asset.ContentType ?? "application/octet-stream", Path.GetFileName(proof.Asset.OriginalFileName ?? $"proof-{revision}"), true);
    }

    private async Task<ActionResult<CustomDesignDto>> RespondToProof(int id, ProofResponseRequest request, bool approve, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, true, cancellationToken);
        if (design == null) return NotFound();
        var latest = design.ProofRevisions.OrderByDescending(proof => proof.RevisionNumber).FirstOrDefault();
        if (latest == null) return NotFound(new ProblemDetails { Title = "No proof is available" });
        if (latest.RevisionNumber != request.RevisionNumber) return Conflict(new ProblemDetails { Title = "A newer proof revision is available", Detail = "Reload the design before responding.", Status = 409 });
        if (approve && latest.Status == ProofRevisionStatus.Approved) return Ok(design.ToDto());
        if (latest.Status != ProofRevisionStatus.AwaitingApproval) return Conflict(new ProblemDetails { Title = "This proof is not awaiting a response", Status = 409 });
        latest.Status = approve ? ProofRevisionStatus.Approved : ProofRevisionStatus.ChangesRequested;
        latest.CustomerResponse = approve ? request.Comment?.Trim() : request.Comment!.Trim();
        latest.CustomerResponseAt = DateTime.UtcNow;
        design.ProofStatus = approve ? ProofStatus.Approved : ProofStatus.ChangesRequested;
        design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(design.ToDto());
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool CanAccess(string? ownerId) => ownerId == UserId() || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SuperAdmin);

    private IQueryable<CustomDesign> IncludeDesign(IQueryable<CustomDesign> query, bool includeProofs = false)
    {
        var included = query.Include(design => design.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options).Include(design => design.Assets);
        return includeProofs ? included.Include(design => design.ProofRevisions).ThenInclude(proof => proof.Asset) : included;
    }

    private async Task<CustomDesign?> OwnDesign(int id, bool includeProofs, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return null;
        return await IncludeDesign(_context.CustomDesigns.Where(design => design.Id == id && design.UserId == userId), includeProofs).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<CustomBuilderConfigurationVersion?> ActiveConfiguration(CancellationToken cancellationToken)
    {
        return await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .Where(version => version.Status == BuilderVersionStatus.Published).OrderByDescending(version => version.PublishedAt).ThenByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ApplyDesign(CustomDesign design, SaveCustomDesignRequest request, CustomBuilderConfigurationVersion configuration, CustomQuoteResponse quote)
    {
        design.Name = request.Name.Trim(); design.DesignSchemaVersion = 1; design.BuilderConfigurationVersionId = configuration.Id;
        design.Width = quote.Width; design.Height = quote.Height; design.Quantity = quote.Quantity; design.ShapeCode = request.ShapeCode;
        design.MaterialCode = request.MaterialCode; design.FinishCode = request.FinishCode; design.FontCode = request.FontCode;
        design.ColourCode = request.ColourCode; design.CustomText = request.CustomText?.Trim(); design.TextAlignment = request.TextAlignment;
        design.DesignJson = string.IsNullOrWhiteSpace(request.EditorStateJson) ? "{}" : request.EditorStateJson;
        design.UnitPrice = quote.UnitPrice; design.CalculatedPrice = quote.Subtotal; design.PricingRuleVersion = quote.PricingVersion;
        design.QuotedAt = quote.QuotedAt; design.Status = CustomDesignStatus.Ready; design.UpdatedAt = DateTime.UtcNow;
    }

    private static SaveCustomDesignRequest RequestFrom(CustomDesign design) => new()
    {
        Name = design.Name, Width = design.Width, Height = design.Height, Quantity = design.Quantity, ShapeCode = design.ShapeCode,
        MaterialCode = design.MaterialCode, FinishCode = design.FinishCode, FontCode = design.FontCode, ColourCode = design.ColourCode,
        CustomText = design.CustomText, TextAlignment = design.TextAlignment, EditorStateJson = design.DesignJson
    };

    private static void ValidateEditorState(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json)) JsonDocument.Parse(json).Dispose();
    }

    private ActionResult BuilderValidation(CustomBuilderValidationException exception) => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() }));
}
