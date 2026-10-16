using be.Api.Files;
using be.Application.Features.CustomDesigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/custom-designs")]
[Authorize]
public class CustomDesignsController(CustomDesignService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await service.List(page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomDesignDto>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.Get(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CustomDesignDto>> Create([FromBody] SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        var result = await service.Create(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomDesignDto>> Update(int id, [FromBody] SaveCustomDesignRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.Update(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<CustomDesignDto>> Duplicate(int id, CancellationToken cancellationToken)
    {
        var result = await service.Duplicate(id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        await service.Archive(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/artwork")]
    [EnableRateLimiting("custom-upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<ActionResult<CustomDesignAssetDto>> UploadArtwork(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Ok(await service.UploadArtwork(id, file.ToMediaUpload(), cancellationToken));
    }

    [HttpGet("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> DownloadAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsset(id, assetId, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName, result.EnableRangeProcessing);
    }

    [HttpDelete("{id:int}/assets/{assetId:int}")]
    public async Task<IActionResult> DeleteAsset(int id, int assetId, CancellationToken cancellationToken)
    {
        await service.DeleteAsset(id, assetId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/cart")]
    public async Task<ActionResult> AddToCart(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.AddToCart(id, cancellationToken));
    }

    [HttpPost("{id:int}/proofs/approve")]
    public async Task<ActionResult<CustomDesignDto>> ApproveProof(int id, [FromBody] ProofResponseRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.ApproveProof(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/proofs/request-changes")]
    public async Task<ActionResult<CustomDesignDto>> RequestProofChanges(int id, [FromBody] ProofResponseRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.RequestProofChanges(id, request, cancellationToken));
    }

    [HttpGet("{id:int}/proofs/{revision:int}/file")]
    public async Task<IActionResult> DownloadProof(int id, int revision, CancellationToken cancellationToken)
    {
        var result = await service.DownloadProof(id, revision, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName, result.EnableRangeProcessing);
    }
}
