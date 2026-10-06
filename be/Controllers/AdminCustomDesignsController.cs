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
[Route("api/v1/admin/custom-designs")]
[Authorize(Policy = AppPolicies.ManageOrders)]
public class AdminCustomDesignsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaStorageService _storage;
    private readonly IAuditService _audit;

    public AdminCustomDesignsController(ApplicationDbContext context, IMediaStorageService storage, IAuditService audit)
    {
        _context = context;
        _storage = storage;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? search, [FromQuery] string? proofStatus, [FromQuery] string? productionStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.CustomDesigns.Include(design => design.User).Where(design => design.Status == CustomDesignStatus.Ordered);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(design => design.Name.Contains(search) || (design.User != null && ((design.User.Name != null && design.User.Name.Contains(search)) || (design.User.Email != null && design.User.Email.Contains(search)))));
        }
        if (Enum.TryParse<ProofStatus>(proofStatus, true, out var parsedProof)) query = query.Where(design => design.ProofStatus == parsedProof);
        if (Enum.TryParse<ProductionStatus>(productionStatus, true, out var parsedProduction)) query = query.Where(design => design.ProductionStatus == parsedProduction);
        var totalCount = await query.CountAsync(cancellationToken);
        var designs = await query.AsNoTracking().OrderByDescending(design => design.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(design => new
            {
                design.Id, design.Name, Status = design.Status.ToString(), ProofStatus = design.ProofStatus.ToString(), ProductionStatus = design.ProductionStatus.ToString(),
                design.Width, design.Height, design.Quantity, design.MaterialCode, design.FinishCode, design.CalculatedPrice, design.UpdatedAt,
                CustomerName = design.User != null ? design.User.Name : null, CustomerEmail = design.User != null ? design.User.Email : null,
                OrderNumber = _context.OrderItems.Where(item => item.CustomDesignId == design.Id).Select(item => item.Order!.OrderNumber).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        return Ok(new { items = designs, totalCount, page, pageSize, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var design = await DesignQuery().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null) return NotFound();
        var order = await _context.OrderItems.Where(item => item.CustomDesignId == id).Select(item => new { item.OrderId, item.Order!.OrderNumber, item.ConfigurationSnapshotJson }).FirstOrDefaultAsync(cancellationToken);
        return Ok(new { design = design.ToDto(), customer = new { design.UserId, design.User?.Name, design.User?.Email }, order });
    }

    [HttpPost("{id:int}/proofs")]
    [EnableRateLimiting("custom-upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<ActionResult<ProofRevisionDto>> UploadProof(int id, IFormFile file, [FromForm] string? customerVisibleMessage, CancellationToken cancellationToken)
    {
        var design = await DesignQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null) return NotFound();
        if (design.Status != CustomDesignStatus.Ordered) return Conflict(new ProblemDetails { Title = "Proofs can only be uploaded for ordered designs", Status = 409 });
        if (customerVisibleMessage?.Length > 1000) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["customerVisibleMessage"] = ["Message cannot exceed 1000 characters."] }));
        try
        {
            var stored = await _storage.UploadPrivateAsync(file, $"custom-proofs/{design.Id}", cancellationToken);
            var asset = new CustomDesignAsset
            {
                CustomDesignId = design.Id, StorageKey = stored.StorageKey, AssetType = "proof", OriginalFileName = stored.OriginalFileName,
                ContentType = stored.ContentType, SizeBytes = stored.FileSize, Width = stored.Width, Height = stored.Height, CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignAssets.Add(asset);
            foreach (var current in design.ProofRevisions.Where(proof => proof.Status == ProofRevisionStatus.AwaitingApproval)) current.Status = ProofRevisionStatus.Superseded;
            var revision = new CustomDesignProofRevision
            {
                CustomDesign = design, Asset = asset, RevisionNumber = design.ProofRevisions.Select(proof => proof.RevisionNumber).DefaultIfEmpty(0).Max() + 1,
                Status = ProofRevisionStatus.AwaitingApproval, CustomerVisibleMessage = customerVisibleMessage?.Trim(),
                UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, CreatedAt = DateTime.UtcNow
            };
            _context.CustomDesignProofRevisions.Add(revision);
            design.ProofStatus = ProofStatus.AwaitingApproval; design.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.RecordAsync("CustomProofUploaded", nameof(CustomDesign), design.Id.ToString(), null, new { revision.RevisionNumber }, HttpContext, cancellationToken);
            return Ok(new ProofRevisionDto
            {
                Id = revision.Id, RevisionNumber = revision.RevisionNumber, Status = revision.Status.ToString(), CustomerVisibleMessage = revision.CustomerVisibleMessage,
                CreatedAt = revision.CreatedAt, AssetId = asset.Id, DownloadUrl = $"/api/v1/custom-designs/{design.Id}/proofs/{revision.RevisionNumber}/file"
            });
        }
        catch (InvalidOperationException exception) { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["file"] = [exception.Message] })); }
    }

    [HttpPut("{id:int}/production-status")]
    public async Task<ActionResult<CustomDesignDto>> UpdateProductionStatus(int id, [FromBody] UpdateProductionStatusRequest request, CancellationToken cancellationToken)
    {
        var design = await DesignQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design == null) return NotFound();
        if (!Enum.TryParse<ProductionStatus>(request.Status, true, out var next)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["status"] = ["Unknown production status."] }));
        if (!CanTransition(design.ProductionStatus, next)) return Conflict(new ProblemDetails { Title = "Invalid production transition", Detail = $"Cannot move from {design.ProductionStatus} to {next}.", Status = 409 });
        var previous = design.ProductionStatus; design.ProductionStatus = next; design.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync("CustomProductionStatusChanged", nameof(CustomDesign), design.Id.ToString(), new { Status = previous.ToString() }, new { Status = next.ToString() }, HttpContext, cancellationToken);
        return Ok(design.ToDto());
    }

    private IQueryable<CustomDesign> DesignQuery() => _context.CustomDesigns
        .Include(design => design.User)
        .Include(design => design.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options)
        .Include(design => design.Assets)
        .Include(design => design.ProofRevisions).ThenInclude(proof => proof.Asset);

    private static bool CanTransition(ProductionStatus current, ProductionStatus next)
    {
        if (current == next) return true;
        return (current, next) switch
        {
            (ProductionStatus.NotStarted, ProductionStatus.Queued) => true,
            (ProductionStatus.Queued, ProductionStatus.Printing) => true,
            (ProductionStatus.Printing, ProductionStatus.Finishing) => true,
            (ProductionStatus.Finishing, ProductionStatus.QualityCheck) => true,
            (ProductionStatus.QualityCheck, ProductionStatus.Ready) => true,
            _ => false
        };
    }
}
