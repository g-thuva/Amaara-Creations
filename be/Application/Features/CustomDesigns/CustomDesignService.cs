using be.Application.Abstractions.Storage;
using be.Application.Features.Checkout;
using be.Domain.Audit;
using be.Domain.Cart;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;

namespace be.Application.Features.CustomDesigns;
public class CustomDesignService
{
    private readonly IApplicationDbContext _context;
    private readonly CustomStickerPricingService _pricing;
    private readonly IMediaStorageService _storage;
    private readonly IOptions<CheckoutOptions> _checkoutOptions;
    private readonly IAuthorizationService _authorization;
    public CustomDesignService(
        IApplicationDbContext context,
        CustomStickerPricingService pricing,
        IMediaStorageService storage,
        IOptions<CheckoutOptions> checkoutOptions,
        IRequestActor actor,
        IAuthorizationService authorization)
    {
        _context = context;
        _pricing = pricing;
        _storage = storage;
        _checkoutOptions = checkoutOptions;
        _actor = actor;
        _authorization = authorization;
    }

    public async Task<object?> List(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CUSTOM_DESIGNS_UNAUTHORIZED", "Authentication is required.", 401);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.CustomDesigns.Where(design => design.UserId == userId && design.Status != CustomDesignStatus.Archived);
        var totalCount = await query.CountAsync(cancellationToken);
        var designs = await IncludeDesign(query)
            .AsNoTracking()
            .OrderByDescending(design => design.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new
        {
            items = designs.Select(design => design.ToDto(false)),
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<CustomDesignDto> Get(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, true, cancellationToken);
        return design == null ? throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404) : design.ToDto();
    }

    public async Task<CustomDesignDto> Create(SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CUSTOM_DESIGNS_UNAUTHORIZED", "Authentication is required.", 401);
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null)
            throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", 503);
        try
        {
            ValidateEditorState(request.EditorStateJson);
            var quote = _pricing.Quote(configuration, request);
            var design = new CustomDesign
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            ApplyDesign(design, request, configuration, quote);
            _context.CustomDesigns.Add(design);
            await _context.SaveChangesAsync(cancellationToken);
            var created = await OwnDesign(design.Id, true, cancellationToken);
            return created!.ToDto();
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
        catch (JsonException)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["editorStateJson"] = ["Editor state must be valid JSON."] });
        }
    }

    public async Task<CustomDesignDto> Update(int id, SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "This design is locked", 409);
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            try
            {
                _context.Entry(design).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Invalid row version", 400);
            }
        }

        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null)
            throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", 503);
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
            return updated!.ToDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Design changed", 409);
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
        catch (JsonException)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["editorStateJson"] = ["Editor state must be valid JSON."] });
        }
    }

    public async Task<CustomDesignDto> Duplicate(int id, CancellationToken cancellationToken)
    {
        var source = await OwnDesign(id, false, cancellationToken);
        if (source == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null)
            throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", 503);
        var request = RequestFrom(source);
        try
        {
            var quote = _pricing.Quote(configuration, request);
            var copy = new CustomDesign
            {
                UserId = source.UserId,
                Name = $"Copy of {source.Name}",
                CreatedAt = DateTime.UtcNow
            };
            request.Name = copy.Name;
            ApplyDesign(copy, request, configuration, quote);
            _context.CustomDesigns.Add(copy);
            await _context.SaveChangesAsync(cancellationToken);
            var created = await OwnDesign(copy.Id, true, cancellationToken);
            return created!.ToDto();
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
    }

    public async Task Archive(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.InCart)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Remove the design from the cart before archiving it", 409);
        design.Status = CustomDesignStatus.Archived;
        design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return;
    }

    public async Task<CustomDesignAssetDto> UploadArtwork(int id, MediaUpload file, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Artwork for this design is locked", 409);
        try
        {
            var stored = await _storage.UploadPrivateAsync(file, $"custom-artwork/{design.Id}", cancellationToken);
            var asset = new CustomDesignAsset
            {
                CustomDesignId = design.Id,
                StorageKey = stored.StorageKey,
                AssetType = "artwork",
                OriginalFileName = stored.OriginalFileName,
                ContentType = stored.ContentType,
                SizeBytes = stored.FileSize,
                Width = stored.Width,
                Height = stored.Height,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignAssets.Add(asset);
            design.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return new CustomDesignAssetDto
            {
                Id = asset.Id,
                AssetType = asset.AssetType,
                OriginalFileName = asset.OriginalFileName,
                ContentType = asset.ContentType,
                SizeBytes = asset.SizeBytes,
                Width = asset.Width,
                Height = asset.Height,
                CreatedAt = asset.CreatedAt,
                DownloadUrl = $"/api/v1/custom-designs/{id}/assets/{asset.Id}"};
        }
        catch (InvalidOperationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["file"] = [exception.Message] });
        }
    }

    public async Task<DownloadContent> DownloadAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        var asset = await _context.CustomDesignAssets.Include(item => item.CustomDesign)
            .FirstOrDefaultAsync(item => item.Id == assetId && item.CustomDesignId == id, cancellationToken);
        if (asset == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (!await CanAccess(asset.CustomDesign?.UserId))
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (!await _storage.ExistsAsync(asset.StorageKey, cancellationToken))
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        var stream = await _storage.OpenReadAsync(asset.StorageKey, cancellationToken);
        return new DownloadContent(stream, asset.ContentType ?? "application/octet-stream", DownloadName(asset.OriginalFileName, asset.ContentType, $"asset-{asset.Id}"), true);
    }

    public async Task DeleteAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, false, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Artwork for this design is retained", 409);
        var asset = await _context.CustomDesignAssets.FirstOrDefaultAsync(item => item.Id == assetId && item.CustomDesignId == id && item.AssetType == "artwork", cancellationToken);
        if (asset == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        _context.CustomDesignAssets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(asset.StorageKey, cancellationToken);
        return;
    }

    public async Task<object?> AddToCart(int id, CancellationToken cancellationToken)
    {
        var design = await OwnDesign(id, true, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (design.Status is CustomDesignStatus.Ordered or CustomDesignStatus.Archived)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "This design cannot be added to the cart", 409);
        var configuration = await ActiveConfiguration(cancellationToken);
        if (configuration == null)
            throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Custom builder is not configured", 503);
        if (configuration.ArtworkRequired && !design.Assets.Any(asset => asset.AssetType == "artwork"))
            throw new RequestValidationException(new Dictionary<string, string[]> { ["artwork"] = ["Artwork is required before this design can be added to the cart."] });
        try
        {
            var request = RequestFrom(design);
            var quote = _pricing.Quote(configuration, request);
            ApplyDesign(design, request, configuration, quote);
            design.Status = CustomDesignStatus.InCart;
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(item => item.UserId == design.UserId && item.CustomDesignId == design.Id, cancellationToken);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    UserId = design.UserId!,
                    ItemType = CartItemType.CustomDesign,
                    CustomDesignId = design.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            cartItem.Quantity = design.Quantity;
            cartItem.UnitPriceSnapshot = quote.UnitPrice;
            cartItem.BuilderConfigurationVersionId = configuration.Id;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return new
            {
                cartItemId = cartItem.Id,
                design = design.ToDto(false)
            };
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
    }

    public async Task<CustomDesignDto> ApproveProof(int id, ProofResponseRequest request, CancellationToken cancellationToken)
    {
        return await RespondToProof(id, request, true, cancellationToken);
    }

    public async Task<CustomDesignDto> RequestProofChanges(int id, ProofResponseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new RequestValidationException(new Dictionary<string, string[]> { ["comment"] = ["Describe the changes you need."] });
        return await RespondToProof(id, request, false, cancellationToken);
    }

    public async Task<DownloadContent> DownloadProof(int id, int revision, CancellationToken cancellationToken)
    {
        var proof = await _context.CustomDesignProofRevisions.Include(item => item.CustomDesign)
            .Include(item => item.Asset)
            .FirstOrDefaultAsync(item => item.CustomDesignId == id && item.RevisionNumber == revision, cancellationToken);
        if (proof?.Asset == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (!await CanAccess(proof.CustomDesign?.UserId))
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        var stream = await _storage.OpenReadAsync(proof.Asset.StorageKey, cancellationToken);
        return new DownloadContent(stream, proof.Asset.ContentType ?? "application/octet-stream", DownloadName(proof.Asset.OriginalFileName, proof.Asset.ContentType, $"proof-{revision}"), true);
    }

    private async Task<CustomDesignDto> RespondToProof(int id, ProofResponseRequest request, bool approve, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(cancellationToken);
        var design = await OwnDesign(id, true, cancellationToken);
        if (design == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        var latest = design.ProofRevisions.OrderByDescending(proof => proof.RevisionNumber).FirstOrDefault();
        if (latest == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "No proof is available", 404);
        if (latest.RevisionNumber != request.RevisionNumber)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "A newer proof revision is available", 409);
        if (approve && latest.Status == ProofRevisionStatus.Approved)
            return design.ToDto();
        if (latest.Status != ProofRevisionStatus.AwaitingApproval)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "This proof is not awaiting a response", 409);
        latest.Status = approve ? ProofRevisionStatus.Approved : ProofRevisionStatus.ChangesRequested;
        latest.CustomerResponse = approve ? request.Comment?.Trim() : request.Comment!.Trim();
        latest.CustomerResponseAt = DateTime.UtcNow;
        _context.AuditLogs.Add(new AuditLog { ActorUserId = UserId(), Action = approve ? "ProofApprovedByCustomer" : "ProofRevisionRequested", EntityType = nameof(CustomDesign), EntityId = id.ToString(), NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { request.RevisionNumber, request.Comment }) });
        design.ProofStatus = approve ? ProofStatus.Approved : ProofStatus.ChangesRequested;
        var proofOrder = await _context.OrderItems.Where(i => i.CustomDesignId == id).Select(i => i.Order).FirstOrDefaultAsync(cancellationToken);
        if (proofOrder != null)
            be.Application.Features.Notifications.CommerceNotifications.Enqueue(_context, proofOrder, $"proof-response:{id}:{request.RevisionNumber}", approve ? "Production proof approved" : "Proof revision requested", $"Your proof response for order {proofOrder.OrderNumber} was recorded.");
        design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
        return design.ToDto();
    }

    private string? UserId() => _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
    private async Task<bool> CanAccess(string? ownerId) =>
        ownerId != null && (ownerId == UserId() || (await _authorization.AuthorizeAsync(_actor.User, null, AppPolicies.ManageOrders)).Succeeded);
    private IQueryable<CustomDesign> IncludeDesign(IQueryable<CustomDesign> query, bool includeProofs = false)
    {
        var included = query.Include(design => design.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options).Include(design => design.Assets);
        return includeProofs ? included.Include(design => design.ProofRevisions).ThenInclude(proof => proof.Asset) : included;
    }

    private async Task<CustomDesign?> OwnDesign(int id, bool includeProofs, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            return null;
        return await IncludeDesign(_context.CustomDesigns.Where(design => design.Id == id && design.UserId == userId), includeProofs)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<CustomBuilderConfigurationVersion?> ActiveConfiguration(CancellationToken cancellationToken)
    {
        return await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .Where(version => version.Status == BuilderVersionStatus.Published && version.Currency == _checkoutOptions.Value.Currency)
            .OrderByDescending(version => version.PublishedAt)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ApplyDesign(
        CustomDesign design,
        SaveCustomDesignRequest request,
        CustomBuilderConfigurationVersion configuration,
        CustomQuoteResponse quote)
    {
        design.Name = request.Name.Trim();
        design.DesignSchemaVersion = EditorLayout.Version(request.EditorStateJson);
        design.BuilderConfigurationVersionId = configuration.Id;
        design.Width = quote.Width;
        design.Height = quote.Height;
        design.Quantity = quote.Quantity;
        design.ShapeCode = request.ShapeCode;
        design.MaterialCode = request.MaterialCode;
        design.FinishCode = request.FinishCode;
        design.FontCode = request.FontCode;
        design.ColourCode = request.ColourCode;
        design.CustomText = request.CustomText?.Trim();
        design.TextAlignment = request.TextAlignment;
        design.DesignJson = string.IsNullOrWhiteSpace(request.EditorStateJson) ? "{}" : request.EditorStateJson;
        design.UnitPrice = quote.UnitPrice;
        design.CalculatedPrice = quote.Subtotal;
        design.PricingRuleVersion = quote.PricingVersion;
        design.QuotedAt = quote.QuotedAt;
        design.Status = CustomDesignStatus.Ready;
        design.UpdatedAt = DateTime.UtcNow;
    }

    private static SaveCustomDesignRequest RequestFrom(CustomDesign design) => new()
    {
        Name = design.Name,
        Width = design.Width,
        Height = design.Height,
        Quantity = design.Quantity,
        ShapeCode = design.ShapeCode,
        MaterialCode = design.MaterialCode,
        FinishCode = design.FinishCode,
        FontCode = design.FontCode,
        ColourCode = design.ColourCode,
        CustomText = design.CustomText,
        TextAlignment = design.TextAlignment,
        EditorStateJson = design.DesignJson
    };
    private static void ValidateEditorState(string? json)
    {
        EditorLayout.Validate(json);
    }

    private static string DownloadName(string? original, string? contentType, string fallback)
    {
        var name = Path.GetFileName(original ?? fallback);
        return contentType == "image/png" ? Path.ChangeExtension(name, ".png") : name;
    }

    private readonly IRequestActor _actor;
}
