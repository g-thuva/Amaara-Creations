using be.Application.Features.Identity;
using be.Application.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public class AdminCategoriesController(CategoryManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories([FromQuery] bool includeInactive = true)
    {
        return Ok(await service.GetCategories(includeInactive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
    {
        return Ok(await service.GetCategory(id));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryRequest request)
    {
        var result = await service.CreateCategory(request);
        return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, CategoryRequest request)
    {
        return Ok(await service.UpdateCategory(id, request));
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> ArchiveCategory(int id)
    {
        await service.ArchiveCategory(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateCategory(int id)
    {
        await service.ReactivateCategory(id);
        return NoContent();
    }
}
