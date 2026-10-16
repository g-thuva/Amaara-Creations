using be.Application.Features.CustomDesigns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/custom-builder")]
public class CustomBuilderController(BuilderQueries service) : ControllerBase
{
    [HttpGet("config")]
    public async Task<ActionResult<CustomBuilderConfigurationDto>> GetConfiguration(CancellationToken cancellationToken)
    {
        return Ok(await service.GetConfiguration(cancellationToken));
    }

    [HttpPost("quote")]
    [EnableRateLimiting("custom-quote")]
    public async Task<ActionResult<CustomQuoteResponse>> Quote([FromBody] CustomQuoteRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.Quote(request, cancellationToken));
    }
}
