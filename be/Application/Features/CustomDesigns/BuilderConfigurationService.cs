using be.Application.Abstractions.Audit;
using be.Domain.CustomDesigns;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.CustomDesigns;
public class BuilderConfigurationService
{
    private readonly IApplicationDbContext _context;
    private readonly CustomStickerPricingService _pricing;
    private readonly IAuditService _audit;
    public BuilderConfigurationService(IApplicationDbContext context, CustomStickerPricingService pricing, IAuditService audit, IRequestActor actor)
    {
        _context = context;
        _pricing = pricing;
        _audit = audit;
        _actor = actor;
    }

    public async Task<object?> Get(CancellationToken cancellationToken)
    {
        var versions = await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .AsNoTracking()
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        return new
        {
            draft = versions.FirstOrDefault(version => version.Status == BuilderVersionStatus.Draft)?.ToDto(true),
            published = versions.FirstOrDefault(version => version.Status == BuilderVersionStatus.Published)?.ToDto(true),
            versions = versions.Select(version => version.ToDto(true))
        };
    }

    public async Task<CustomBuilderConfigurationDto> UpdateDraft(int id, UpdateBuilderConfigurationRequest request, CancellationToken cancellationToken)
    {
        var draft = await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .FirstOrDefaultAsync(version => version.Id == id, cancellationToken);
        if (draft == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (draft.Status != BuilderVersionStatus.Draft)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Published configuration is immutable", 409);
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            try
            {
                _context.Entry(draft).Property(version => version.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                throw new CommerceException("CUSTOM_DESIGNS_REQUEST_INVALID", "Invalid row version", 400);
            }
        }

        Apply(draft, request);
        var errors = _pricing.ValidateConfiguration(draft).ToList();
        errors.AddRange(ValidateOptions(draft.Options));
        if (errors.Count > 0)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = errors.ToArray() });
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Configuration changed", 409);
        }

        await _audit.RecordAsync("CustomBuilderDraftUpdated", nameof(CustomBuilderConfigurationVersion), draft.Id.ToString(), null, new { draft.VersionNumber }, _actor.Audit, cancellationToken);
        return draft.ToDto(true);
    }

    public async Task<CustomQuoteResponse> TestQuote(int id, CustomQuoteRequest request, CancellationToken cancellationToken)
    {
        var draft = await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.Id == id && version.Status == BuilderVersionStatus.Draft, cancellationToken);
        if (draft == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        try
        {
            return _pricing.Quote(draft, request);
        }
        catch (CustomBuilderValidationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() });
        }
    }

    public async Task<object?> Publish(int id, CancellationToken cancellationToken)
    {
        var draft = await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
            .FirstOrDefaultAsync(version => version.Id == id, cancellationToken);
        if (draft == null)
            throw new CommerceException("CUSTOM_DESIGNS_NOT_FOUND", "Resource not found.", 404);
        if (draft.Status != BuilderVersionStatus.Draft)
            throw new CommerceException("CUSTOM_DESIGNS_CONFLICT", "Only a draft can be published", 409);
        var errors = _pricing.ValidateConfiguration(draft).Concat(ValidateOptions(draft.Options)).ToArray();
        if (errors.Length > 0)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["configuration"] = errors });
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var current = await _context.CustomBuilderConfigurationVersions.FirstOrDefaultAsync(version => version.Status == BuilderVersionStatus.Published, cancellationToken);
        if (current != null)
            current.Status = BuilderVersionStatus.Superseded;
        draft.Status = BuilderVersionStatus.Published;
        draft.PublishedAt = DateTime.UtcNow;
        draft.PublishedByUserId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        draft.UpdatedAt = DateTime.UtcNow;
        var nextVersion = await _context.CustomBuilderConfigurationVersions.MaxAsync(version => version.VersionNumber, cancellationToken) + 1;
        var nextDraft = Clone(draft, nextVersion);
        _context.CustomBuilderConfigurationVersions.Add(nextDraft);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _audit.RecordAsync("CustomBuilderPublished", nameof(CustomBuilderConfigurationVersion), draft.Id.ToString(), current == null ? null : new { current.VersionNumber }, new { draft.VersionNumber }, _actor.Audit, cancellationToken);
        return new
        {
            published = draft.ToDto(true),
            draft = nextDraft.ToDto(true)
        };
    }

    private void Apply(CustomBuilderConfigurationVersion draft, UpdateBuilderConfigurationRequest request)
    {
        draft.MeasurementUnit = request.MeasurementUnit.Trim().ToLowerInvariant();
        draft.MinimumWidth = request.MinimumWidth;
        draft.MaximumWidth = request.MaximumWidth;
        draft.WidthStep = request.WidthStep;
        draft.MinimumHeight = request.MinimumHeight;
        draft.MaximumHeight = request.MaximumHeight;
        draft.HeightStep = request.HeightStep;
        draft.MinimumQuantity = request.MinimumQuantity;
        draft.MaximumQuantity = request.MaximumQuantity;
        draft.QuantityStep = request.QuantityStep;
        draft.PricePerSquareUnit = request.PricePerSquareUnit;
        draft.MinimumLinePrice = request.MinimumLinePrice;
        draft.Currency = request.Currency.Trim().ToUpperInvariant();
        draft.ArtworkRequired = request.ArtworkRequired;
        draft.ProofRequired = request.ProofRequired;
        draft.InternalNotes = request.InternalNotes?.Trim();
        draft.UpdatedAt = DateTime.UtcNow;
        _context.CustomBuilderOptions.RemoveRange(draft.Options);
        draft.Options = request.Options.Select(option => new CustomBuilderOption { ConfigurationVersionId = draft.Id, Group = Enum.TryParse<BuilderOptionGroup>(option.Group, true, out var group) ? group : (BuilderOptionGroup)(-1), Code = option.Code.Trim().ToLowerInvariant(), Label = option.Label.Trim(), UnitPriceAdjustment = option.UnitPriceAdjustment ?? 0, IsActive = option.IsActive, SortOrder = option.SortOrder })
            .ToList();
    }

    private static IEnumerable<string> ValidateOptions(IEnumerable<CustomBuilderOption> options)
    {
        var list = options.ToList();
        if (list.Any(option => !Enum.IsDefined(option.Group)))
            yield return "Every option group must be valid.";
        if (list.Any(option => string.IsNullOrWhiteSpace(option.Code) || string.IsNullOrWhiteSpace(option.Label)))
            yield return "Option code and label are required.";
        if (list.GroupBy(option => $"{(int)option.Group}:{option.Code}", StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            yield return "Option codes must be unique within each group.";
    }

    private static CustomBuilderConfigurationVersion Clone(CustomBuilderConfigurationVersion source, int versionNumber)
    {
        var clone = new CustomBuilderConfigurationVersion
        {
            VersionNumber = versionNumber,
            Status = BuilderVersionStatus.Draft,
            MeasurementUnit = source.MeasurementUnit,
            MinimumWidth = source.MinimumWidth,
            MaximumWidth = source.MaximumWidth,
            WidthStep = source.WidthStep,
            MinimumHeight = source.MinimumHeight,
            MaximumHeight = source.MaximumHeight,
            HeightStep = source.HeightStep,
            MinimumQuantity = source.MinimumQuantity,
            MaximumQuantity = source.MaximumQuantity,
            QuantityStep = source.QuantityStep,
            PricePerSquareUnit = source.PricePerSquareUnit,
            MinimumLinePrice = source.MinimumLinePrice,
            Currency = source.Currency,
            ArtworkRequired = source.ArtworkRequired,
            ProofRequired = source.ProofRequired,
            InternalNotes = source.InternalNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        clone.Options = source.Options.Select(option => new CustomBuilderOption { ConfigurationVersion = clone, Group = option.Group, Code = option.Code, Label = option.Label, UnitPriceAdjustment = option.UnitPriceAdjustment, IsActive = option.IsActive, SortOrder = option.SortOrder })
            .ToList();
        return clone;
    }

    private readonly IRequestActor _actor;
}
