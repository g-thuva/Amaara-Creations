using System.Security.Claims;
using be.Application.Features.Identity;
using be.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin/inventory"), Authorize(Policy = AppPolicies.ManageInventory)]
public sealed class AdminInventoryController(InventoryQueries service, InventoryOperations inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await service.List(search, page, pageSize, ct));
    }

    [HttpGet("movements")]
    public async Task<IActionResult> Movements(int? productId, int page = 1, CancellationToken ct = default)
    {
        return Ok(await service.Movements(productId, page, ct));
    }

    [HttpPost("{productId:int}/adjustments")]
    public async Task<IActionResult> Adjust(int productId, InventoryAdjustment request, CancellationToken ct)
    {
        await inventory.AdjustAsync(productId, request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return NoContent();
    }
}
