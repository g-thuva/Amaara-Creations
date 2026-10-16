using be.Application.Features.Identity;
using be.Application.Features.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/reviews")]
public sealed class ReviewsController(ReviewService service) : ControllerBase
{
    [HttpGet("/api/v1/products/{productId:int}/reviews")]
    public async Task<IActionResult> Public(int productId, int page = 1, CancellationToken ct = default)
    {
        return Ok(await service.Public(productId, page, ct));
    }

    [HttpPost("/api/v1/products/{productId:int}/reviews"), Authorize]
    public async Task<IActionResult> Create(int productId, CreateReviewRequest request, CancellationToken ct)
    {
        return StatusCode(201, await service.Create(productId, request, ct));
    }

    [HttpGet("mine"), Authorize]
    public async Task<IActionResult> Mine(int? productId, CancellationToken ct)
    {
        return Ok(await service.Mine(productId, ct));
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Edit(int id, UpdateReviewRequest request, CancellationToken ct)
    {
        return Ok(await service.Edit(id, request, ct));
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> DeleteOwn(int id, CancellationToken ct)
    {
        await service.DeleteOwn(id, ct);
        return NoContent();
    }

    [HttpGet("/api/v1/admin/reviews"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> List(
        string? status,
        string? search,
        int? productId,
        int? rating,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await service.List(status, search, productId, rating, pageNumber, pageSize, ct));
    }

    [HttpPut("/api/v1/admin/reviews/{id:int}/moderation"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> Moderate(int id, ModerationRequest request, CancellationToken ct)
    {
        await service.Moderate(id, request, ct);
        return NoContent();
    }

    [HttpDelete("/api/v1/admin/reviews/{id:int}"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> DeleteAdmin(int id, [FromBody] ModerationRequest request, CancellationToken ct)
    {
        await service.DeleteAdmin(id, request, ct);
        return NoContent();
    }

    [HttpGet("/api/v1/admin/reviews/stats"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        return Ok(await service.Stats(ct));
    }
}
