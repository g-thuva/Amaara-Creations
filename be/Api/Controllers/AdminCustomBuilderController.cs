using be.Application.Features.Identity;
using be.Application.Features.CustomDesigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/custom-builder")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public class AdminCustomBuilderController(BuilderConfigurationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(await service.Get(cancellationToken));
    }

    [HttpPut("draft/{id:int}")]
    public async Task<ActionResult<CustomBuilderConfigurationDto>> UpdateDraft(int id, [FromBody] UpdateBuilderConfigurationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateDraft(id, request, cancellationToken));
    }

    [HttpPost("draft/{id:int}/quote")]
    public async Task<ActionResult<CustomQuoteResponse>> TestQuote(int id, [FromBody] CustomQuoteRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.TestQuote(id, request, cancellationToken));
    }

    [HttpPost("draft/{id:int}/publish")]
    public async Task<ActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.Publish(id, cancellationToken));
    }
}
