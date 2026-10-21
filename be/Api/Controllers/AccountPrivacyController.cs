using System.Text.Json;
using be.Api.Identity;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/auth/account"), Authorize]
public sealed class AccountPrivacyController(AccountPrivacyService service, CustomerRequestService requests, RefreshCookie cookie) : ControllerBase
{
    [HttpGet("capabilities")]
    public IActionResult Capabilities() => Ok(requests.Capabilities());
    [HttpPost("email-change"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> EmailChange([FromBody] EmailChangeRequest request, CancellationToken ct)
    {
        await service.RequestEmailChange(request, ct);
        return Ok(new { message = "If this address is available, a verification token has been sent. Your current email is unchanged." });
    }
    [HttpPost("email-change/confirm"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailChangeRequest request, CancellationToken ct)
    {
        await service.ConfirmEmailChange(request, ct); cookie.Clear(Response); return Ok(new { requiresSignIn = true });
    }
    [HttpPost("export"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Export([FromBody] SecondFactorRequest request, CancellationToken ct)
    {
        var export = await service.Export(request, ct);
        return File(JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), "application/json", $"amaara-account-data-{DateTime.UtcNow:yyyy-MM-dd}.json");
    }
    [HttpPost("deletion-review"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> DeletionReview([FromBody] DeletionReviewRequest request, CancellationToken ct) => Ok(await service.DeletionReview(request, ct));
}
