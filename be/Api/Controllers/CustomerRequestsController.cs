using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/account/requests"), Authorize]
public sealed class CustomerRequestsController(CustomerRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, CancellationToken ct = default) => Ok(await service.Own(page, ct));
    [HttpPost("quotes"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Quote([FromBody] CreateCustomerRequest request, CancellationToken ct) => Ok(await service.Create(CustomerRequestKind.CustomQuote, request, ct));
    [HttpPost("{id:guid}/cancel"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) { await service.Cancel(id, ct); return NoContent(); }
}
