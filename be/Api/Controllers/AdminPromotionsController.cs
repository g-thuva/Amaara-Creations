using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Promotions;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin/promotions"), Authorize(Policy = AppPolicies.ManagePromotions)]
public sealed class AdminPromotionsController(PromotionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, CancellationToken ct = default)
    {
        return Ok(await service.List(page, ct));
    }

    [HttpGet("{id:int}/redemptions")]
    public async Task<IActionResult> History(int id, int page = 1, CancellationToken ct = default)
    {
        return Ok(await service.History(id, page, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PromotionRequest request, CancellationToken ct)
    {
        return Ok(await service.Create(request, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PromotionRequest request, CancellationToken ct)
    {
        return Ok(await service.Update(id, request, ct));
    }
}
