using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Cms;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/content")]
public class ContentController(CmsQueries service) : ControllerBase
{
    [HttpGet("pages/{slug}")]
    public async Task<ActionResult<CmsPageResponse>> GetPublishedPage(string slug)
    {
        return Ok(await service.GetPublishedPage(slug));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<IReadOnlyList<SiteSettingResponse>>> GetPublicSettings()
    {
        return Ok(await service.GetPublicSettings());
    }
}
