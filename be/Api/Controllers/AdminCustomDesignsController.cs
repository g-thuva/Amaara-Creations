using be.Api.Files;
using be.Application.Features.Identity;
using be.Application.Features.CustomDesigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be.Application.Features.Production;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/custom-designs")]
[Authorize(Policy = AppPolicies.ManageOrders)]
public class AdminCustomDesignsController(ProductionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? proofStatus,
        [FromQuery] string? productionStatus,
        [FromQuery] string? designStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.List(search, proofStatus, productionStatus, designStatus, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.Get(id, cancellationToken));
    }

    [HttpPost("{id:int}/proofs")]
    [EnableRateLimiting("custom-upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<ActionResult<ProofRevisionDto>> UploadProof(int id, IFormFile file, [FromForm] string? customerVisibleMessage, CancellationToken cancellationToken)
    {
        return Ok(await service.UploadProof(id, file.ToMediaUpload(), customerVisibleMessage, cancellationToken));
    }

    [HttpPut("{id:int}/production-status")]
    public async Task<ActionResult<CustomDesignDto>> UpdateProductionStatus(int id, [FromBody] UpdateProductionStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateProductionStatus(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id, [FromBody] UpdateProductionStatusRequest request, CancellationToken ct)
    {
        await service.Archive(id, request, ct);
        return NoContent();
    }
}
