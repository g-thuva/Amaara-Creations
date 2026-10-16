using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Cms;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/content/pages")]
[Authorize(Policy = AppPolicies.ManageContent)]
public class AdminContentController(CmsManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CmsPageResponse>>> GetPages()
    {
        return Ok(await service.GetPages());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CmsPageResponse>> GetPage(int id)
    {
        return Ok(await service.GetPage(id));
    }

    [HttpPost]
    public async Task<ActionResult<CmsPageResponse>> CreatePage(CmsPageRequest request)
    {
        var result = await service.CreatePage(request);
        return CreatedAtAction(nameof(GetPage), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CmsPageResponse>> UpdatePage(int id, CmsPageRequest request)
    {
        return Ok(await service.UpdatePage(id, request));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> PublishPage(int id)
    {
        await service.PublishPage(id);
        return NoContent();
    }

    [HttpPost("{id:int}/unpublish")]
    public async Task<IActionResult> UnpublishPage(int id)
    {
        await service.UnpublishPage(id);
        return NoContent();
    }
}
