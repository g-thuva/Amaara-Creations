using be.Application.Features.Identity;
using be.Application.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/collections")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public class AdminCollectionsController(CollectionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> GetCollections([FromQuery] bool includeInactive = true)
    {
        return Ok(await service.GetCollections(includeInactive));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CollectionResponse>> GetCollection(int id)
    {
        return Ok(await service.GetCollection(id));
    }

    [HttpPost]
    public async Task<ActionResult<CollectionResponse>> CreateCollection(CollectionRequest request)
    {
        var result = await service.CreateCollection(request);
        return CreatedAtAction(nameof(GetCollection), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CollectionResponse>> UpdateCollection(int id, CollectionRequest request)
    {
        return Ok(await service.UpdateCollection(id, request));
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> ArchiveCollection(int id)
    {
        await service.ArchiveCollection(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateCollection(int id)
    {
        await service.ReactivateCollection(id);
        return NoContent();
    }

    [HttpPut("{id:int}/products")]
    public async Task<IActionResult> ReplaceProducts(int id, CollectionProductsRequest request)
    {
        await service.ReplaceProducts(id, request);
        return NoContent();
    }

    [HttpDelete("{collectionId:int}/products/{productId:int}")]
    public async Task<IActionResult> RemoveProduct(int collectionId, int productId)
    {
        await service.RemoveProduct(collectionId, productId);
        return NoContent();
    }
}
