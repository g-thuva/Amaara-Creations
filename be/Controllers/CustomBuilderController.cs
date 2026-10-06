using be.Data;
using be.DTOs.CustomBuilder;
using be.Models;
using be.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Route("api/v1/custom-builder")]
public class CustomBuilderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomStickerPricingService _pricing;

    public CustomBuilderController(ApplicationDbContext context, ICustomStickerPricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    [HttpGet("config")]
    public async Task<ActionResult<CustomBuilderConfigurationDto>> GetConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await ActiveConfiguration().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return configuration == null
            ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Custom builder is not configured", detail: "An administrator must publish a builder configuration before quotes can be requested.")
            : Ok(configuration.ToDto(false));
    }

    [HttpPost("quote")]
    [EnableRateLimiting("custom-quote")]
    public async Task<ActionResult<CustomQuoteResponse>> Quote([FromBody] CustomQuoteRequest request, CancellationToken cancellationToken)
    {
        var configuration = await ActiveConfiguration().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (configuration == null)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Custom builder is not configured");
        }

        try
        {
            return Ok(_pricing.Quote(configuration, request));
        }
        catch (CustomBuilderValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["configuration"] = exception.Errors.ToArray() }));
        }
    }

    private IQueryable<CustomBuilderConfigurationVersion> ActiveConfiguration()
    {
        return _context.CustomBuilderConfigurationVersions
            .Include(version => version.Options)
            .Where(version => version.Status == BuilderVersionStatus.Published)
            .OrderByDescending(version => version.PublishedAt)
            .ThenByDescending(version => version.VersionNumber);
    }
}
