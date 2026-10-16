using be.Application.Features.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/catalog")]
public class CatalogController(CatalogQueries service) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories()
    {
        return Ok(await service.GetCategories());
    }

    [HttpGet("collections")]
    public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> GetCollections()
    {
        return Ok(await service.GetCollections());
    }
}
