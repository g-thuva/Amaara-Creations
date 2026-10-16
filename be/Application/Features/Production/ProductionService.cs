using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Storage;
using be.Application.Features.CustomDesigns;
using be.Domain.CustomDesigns;
using be.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.Production;
public class ProductionService
{
    private readonly IApplicationDbContext _context;
    private readonly IMediaStorageService _storage;
    private readonly IAuditService _audit;
    public ProductionService(IApplicationDbContext context, IMediaStorageService storage, IAuditService audit, IRequestActor actor)
    {
        _context = context;
        _storage = storage;
        _audit = audit;
        _actor = actor;
    }

    public async Task<object?> List(
        string? search,
        string? proofStatus,
        string? productionStatus,
        string? designStatus,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var state = CustomDesignStatus.Ordered;
        if (!string.IsNullOrWhiteSpace(designStatus) && (!Enum.TryParse(designStatus, true, out state) || state is not (CustomDesignStatus.Ordered or CustomDesignStatus.Archived)))
            throw new CommerceException("PRODUCTION_REQUEST_INVALID", "Choose Ordered or Archived jobs", 400);
        var query = _context.CustomDesigns.Include(design => design.User)
            .Where(design => design.Status == state && _context.OrderItems.Any(i => i.CustomDesignId == design.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(design => design.Name.Contains(search) || _context.OrderItems.Any(i => i.CustomDesignId == design.Id && i.Order!.OrderNumber.Contains(search)) || (design.User != null && ((design.User.Name != null && design.User.Name.Contains(search)) || (design.User.Email != null && design.User.Email.Contains(search)))));
        }

        if (Enum.TryParse<ProofStatus>(proofStatus, true, out var parsedProof))
            query = query.Where(design => design.ProofStatus == parsedProof);
        if (Enum.TryParse<ProductionStatus>(productionStatus, true, out var parsedProduction))
            query = query.Where(design => design.ProductionStatus == parsedProduction);
        var totalCount = await query.CountAsync(cancellationToken);
        var designs = await query.AsNoTracking()
            .OrderByDescending(design => design.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(design => new { design.Id, design.Name, Status = design.Status.ToString(), ProofStatus = design.ProofStatus.ToString(), ProductionStatus = design.ProductionStatus.ToString(), design.Width, design.Height, design.Quantity, design.MaterialCode, design.FinishCode, design.CalculatedPrice, design.UpdatedAt, Currency = design.BuilderConfigurationVersion!.Currency, CustomerName = design.User != null ? design.User.Name : null, CustomerEmail = design.User != null ? design.User.Email : null, OrderNumber = _context.OrderItems.Where(item => item.CustomDesignId == design.Id).Select(item => item.Order!.OrderNumber).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        return new
        {
            items = designs,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<object?> Get(int id, CancellationToken cancellationToken)
    {
        var design = await DesignQuery().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null)
            throw new CommerceException("PRODUCTION_NOT_FOUND", "Resource not found.", 404);
        var order = await _context.OrderItems.Where(item => item.CustomDesignId == id)
            .Select(item => new { item.OrderId, item.Order!.OrderNumber, item.ConfigurationSnapshotJson })
            .FirstOrDefaultAsync(cancellationToken);
        var timeline = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == nameof(CustomDesign) && a.EntityId == id.ToString())
            .OrderBy(a => a.Id)
            .Select(a => new { a.Action, a.ActorUserId, a.CreatedAt, a.NewValuesJson })
            .ToListAsync(cancellationToken);
        var confirmed = await _context.OrderItems.AnyAsync(i => i.CustomDesignId == id && i.Order!.FinalizedAt != null && (i.Order.Status == OrderStatus.Confirmed || i.Order.Status == OrderStatus.Processing), cancellationToken);
        var availableProductionTransitions = confirmed && design.Status == CustomDesignStatus.Ordered && design.ProofStatus is ProofStatus.Approved or ProofStatus.NotRequired ? Enum.GetValues<ProductionStatus>()
            .Where(next => ProductionTransitionPolicy.CanTransition(design.ProductionStatus, next))
            .Select(next => next.ToString())
            .ToArray() : [];
        var canArchive = design.Status == CustomDesignStatus.Ordered && design.ProductionStatus == ProductionStatus.Ready && await _context.OrderItems.AnyAsync(i => i.CustomDesignId == id && (i.Order!.Status == OrderStatus.Completed || i.Order.Status == OrderStatus.Delivered), cancellationToken);
        return new
        {
            design = design.ToDto(),
            customer = new
            {
                design.UserId,
                design.User?.Name,
                design.User?.Email
            },
            order,
            timeline,
            availableProductionTransitions,
            canArchive,
            proofRequired = design.ProofStatus != ProofStatus.NotRequired
        };
    }

    public async Task<ProofRevisionDto> UploadProof(int id, MediaUpload file, string? customerVisibleMessage, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(cancellationToken);
        var design = await DesignQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null)
            throw new CommerceException("PRODUCTION_NOT_FOUND", "Resource not found.", 404);
        if (design.Status != CustomDesignStatus.Ordered)
            throw new CommerceException("PRODUCTION_CONFLICT", "Proofs can only be uploaded for ordered designs", 409);
        if (design.ProductionStatus is not (ProductionStatus.NotStarted or ProductionStatus.Queued))
            throw new CommerceException("PRODUCTION_CONFLICT", "Production has started; resolve this job before revising its proof", 409);
        if (customerVisibleMessage?.Length > 1000)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["customerVisibleMessage"] = ["Message cannot exceed 1000 characters."] });
        try
        {
            var stored = await _storage.UploadPrivateAsync(file, $"custom-proofs/{design.Id}", cancellationToken);
            var asset = new CustomDesignAsset
            {
                CustomDesignId = design.Id,
                StorageKey = stored.StorageKey,
                AssetType = "proof",
                OriginalFileName = stored.OriginalFileName,
                ContentType = stored.ContentType,
                SizeBytes = stored.FileSize,
                Width = stored.Width,
                Height = stored.Height,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignAssets.Add(asset);
            foreach (var current in design.ProofRevisions.Where(proof => proof.Status == ProofRevisionStatus.AwaitingApproval))
                current.Status = ProofRevisionStatus.Superseded;
            var revision = new CustomDesignProofRevision
            {
                CustomDesign = design,
                Asset = asset,
                RevisionNumber = design.ProofRevisions.Select(proof => proof.RevisionNumber).DefaultIfEmpty(0).Max() + 1,
                Status = ProofRevisionStatus.AwaitingApproval,
                CustomerVisibleMessage = customerVisibleMessage?.Trim(),
                UploadedByUserId = _actor.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignProofRevisions.Add(revision);
            design.ProofStatus = ProofStatus.AwaitingApproval;
            design.UpdatedAt = DateTime.UtcNow;
            var order = await _context.OrderItems.Where(i => i.CustomDesignId == id).Select(i => i.Order).FirstOrDefaultAsync(cancellationToken);
            if (order != null)
                be.Application.Features.Notifications.CommerceNotifications.Enqueue(_context, order, $"proof-ready:{id}:{revision.RevisionNumber}", "A production proof is ready", $"Sign in to your account to review proof revision {revision.RevisionNumber} for order {order.OrderNumber}.");
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.RecordAsync("CustomProofUploaded", nameof(CustomDesign), design.Id.ToString(), null, new { revision.RevisionNumber }, _actor.Audit, cancellationToken);
            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);
            return new ProofRevisionDto
            {
                Id = revision.Id,
                RevisionNumber = revision.RevisionNumber,
                Status = revision.Status.ToString(),
                CustomerVisibleMessage = revision.CustomerVisibleMessage,
                CreatedAt = revision.CreatedAt,
                AssetId = asset.Id,
                DownloadUrl = $"/api/v1/custom-designs/{design.Id}/proofs/{revision.RevisionNumber}/file"};
        }
        catch (InvalidOperationException exception)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["file"] = [exception.Message] });
        }
    }

    public async Task<CustomDesignDto> UpdateProductionStatus(int id, UpdateProductionStatusRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(cancellationToken);
        var design = await DesignQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null)
            throw new CommerceException("PRODUCTION_NOT_FOUND", "Resource not found.", 404);
        if (design.Status != CustomDesignStatus.Ordered)
            throw new CommerceException("PRODUCTION_CONFLICT", "Only ordered designs enter production", 409);
        if (!await _context.OrderItems.AnyAsync(i => i.CustomDesignId == id && i.Order!.FinalizedAt != null && (i.Order.Status == OrderStatus.Confirmed || i.Order.Status == OrderStatus.Processing), cancellationToken))
            throw new CommerceException("PRODUCTION_CONFLICT", "A confirmed paid order is required for production", 409);
        if (request.RowVersion != null && request.RowVersion != Convert.ToBase64String(design.RowVersion))
            throw new CommerceException("PRODUCTION_CONFLICT", "Refresh the changed design", 409);
        if (design.ProofStatus is not (ProofStatus.Approved or ProofStatus.NotRequired))
            throw new CommerceException("PRODUCTION_CONFLICT", "Proof approval is required before production", 409);
        if (!Enum.TryParse<ProductionStatus>(request.Status, true, out var next) || !Enum.IsDefined(next))
            throw new RequestValidationException(new Dictionary<string, string[]> { ["status"] = ["Unknown production status."] });
        if (!ProductionTransitionPolicy.CanTransition(design.ProductionStatus, next))
            throw new CommerceException("PRODUCTION_CONFLICT", "Invalid production transition", 409);
        var previous = design.ProductionStatus;
        design.ProductionStatus = next;
        design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync("CustomProductionStatusChanged", nameof(CustomDesign), design.Id.ToString(), new { Status = previous.ToString() }, new { Status = next.ToString(), request.Note }, _actor.Audit, cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
        return design.ToDto();
    }

    public async Task Archive(int id, UpdateProductionStatusRequest request, CancellationToken ct)
    {
        await using var transaction = await _context.BeginCommerceTransactionAsync(ct);
        var design = await _context.CustomDesigns.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (design == null)
            throw new CommerceException("PRODUCTION_NOT_FOUND", "Resource not found.", 404);
        if (design.Status != CustomDesignStatus.Ordered || design.ProductionStatus != ProductionStatus.Ready || !await _context.OrderItems.AnyAsync(i => i.CustomDesignId == id && (i.Order!.Status == OrderStatus.Completed || i.Order.Status == OrderStatus.Delivered), ct))
            throw new CommerceException("PRODUCTION_CONFLICT", "Only ready, fulfilled jobs can be archived", 409);
        if (request.RowVersion == null || request.RowVersion != Convert.ToBase64String(design.RowVersion))
            throw new CommerceException("PRODUCTION_CONFLICT", "Refresh the changed job", 409);
        design.Status = CustomDesignStatus.Archived;
        design.UpdatedAt = DateTime.UtcNow;
        await _audit.RecordAsync("CustomProductionArchived", nameof(CustomDesign), id.ToString(), new { Status = "Ordered" }, new { Status = "Archived", request.Note }, _actor.Audit, ct);
        if (transaction != null)
            await transaction.CommitAsync(ct);
        return;
    }

    private IQueryable<CustomDesign> DesignQuery() => _context.CustomDesigns.Include(design => design.User).Include(design => design.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options)
        .Include(design => design.Assets)
        .Include(design => design.ProofRevisions)
        .ThenInclude(proof => proof.Asset);
    private readonly IRequestActor _actor;
}
