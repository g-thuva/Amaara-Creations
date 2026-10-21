using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/auth/sessions"), Authorize]
public sealed class AccountSessionsController(AccountSessionService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.List(ct));
    [HttpDelete("{id}"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Revoke(string id, CancellationToken ct) { await service.Revoke(id, ct); return NoContent(); }
}
