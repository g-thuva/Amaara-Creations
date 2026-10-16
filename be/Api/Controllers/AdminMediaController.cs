using be.Application.Features.Identity;
using be.Application.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/media")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public class AdminMediaController(MediaQueries service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductMediaResponse>>> GetMedia([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return Ok(await service.GetMedia(search, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductMediaResponse>> GetMediaById(int id)
    {
        return Ok(await service.GetMediaById(id));
    }
}
