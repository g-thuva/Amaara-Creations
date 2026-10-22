using be.Api.Identity;
using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/auth/staff")]
public sealed class StaffAuthController(IdentityService identity, RefreshCookie cookie, IConfiguration configuration, IAuthorizationService authorization) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var session = await identity.LoginAsync(request, ct, management: true);
        cookie.Write(Response, session, management: true);
        return Ok(session.Response);
    }
    [HttpPost("refresh"), EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var session = await identity.RefreshAsync(cookie.Read(Request, true), ct, cookie.ReadAssurance(Request, true), management: true);
        if (session == null) { cookie.Clear(Response, true); return Unauthorized(new { message = "Management session expired." }); }
        cookie.Write(Response, session, true);
        return Ok(session.Response);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await identity.LogoutAsync(cookie.Read(Request, true), ct);
        cookie.Clear(Response, true);
        return Ok(new { message = "Management session signed out." });
    }
    [HttpGet("session"), Authorize]
    public async Task<IActionResult> Session(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!User.HasClaim("session_kind", "management") || !AppRoles.AdministrativeRoles.Any(User.IsInRole)) return Forbid();
        var user = await identity.CurrentUserAsync();
        var factor = await identity.SecondFactorStatusAsync(ct);
        var enabled = configuration.GetValue<bool>("Administration:Enabled");
        var permissions = new List<string>();
        foreach (var policy in AppPolicies.Roles.Keys)
            if ((await authorization.AuthorizeAsync(User, policy)).Succeeded) permissions.Add(policy);
        var status = !enabled ? "disabled" : !factor.Enabled ? "enrollment_required" : !permissions.Contains(AppPolicies.AdminAccess) ? "mfa_required" : "authorized";
        DateTimeOffset? expiresAt = long.TryParse(User.FindFirst("mfa_time")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var time) ? DateTimeOffset.FromUnixTimeSeconds(time).AddMinutes(15) : null;
        return Ok(new { status, user, permissions, expiresAt });
    }
}
