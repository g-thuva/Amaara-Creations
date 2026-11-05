using be.Api.Identity;
using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/auth/staff")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class StaffAuthController(IdentityService identity, RefreshCookie cookie, IConfiguration configuration, IAuthorizationService authorization) : ControllerBase
{
    [HttpPost("login"), HttpPost("sign-in"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        return SignInResponse.Present(this, cookie, await identity.SignInAsync(request, ct, managementEntry: true), managementEntry: true);
    }

    [HttpGet("sign-in"), EnableRateLimiting("auth-refresh")]
    public Task<IActionResult> SignInState(CancellationToken ct) => SignInResponse.State(this, identity, cookie, ct, managementEntry: true);

    [HttpPost("sign-in/verify"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> VerifySignIn([FromBody] SignInVerification request, CancellationToken ct) => Present(await identity.AdvanceSignInAsync(cookie.ReadPending(Request, true), "verify", request.Code, request.RecoveryCode, false, ct, management: true));

    [HttpPost("sign-in/enrollment/confirm"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ConfirmSignInEnrollment([FromBody] SignInVerification request, CancellationToken ct) => Present(await identity.AdvanceSignInAsync(cookie.ReadPending(Request, true), "confirm", request.Code, null, false, ct, management: true));

    [HttpPost("sign-in/complete"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> CompleteSignIn([FromBody] SignInAcknowledgment request, CancellationToken ct) => Present(await identity.AdvanceSignInAsync(cookie.ReadPending(Request, true), "complete", null, null, request.SavedRecoveryCodes, ct, management: true));

    [HttpPost("sign-in/cancel"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> CancelSignIn(CancellationToken ct) => Present(await identity.AdvanceSignInAsync(cookie.ReadPending(Request, true), "cancel", null, null, false, ct, management: true));

    private IActionResult Present(be.Application.Features.Identity.SignInResult result) => SignInResponse.Present(this, cookie, result, managementEntry: true);
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
        await identity.CancelPendingSignInAsync(cookie.ReadPending(Request, true), ct, management: true);
        await identity.LogoutAsync(cookie.Read(Request, true), ct);
        cookie.Clear(Response, true);
        cookie.ClearPending(Response, true);
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
        DateTimeOffset? expiresAt = long.TryParse(User.FindFirst("mfa_time")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var time) ? DateTimeOffset.FromUnixTimeSeconds(time).Add(SessionPolicy.Absolute(configuration, true)) : null;
        return Ok(new { status, user, permissions, expiresAt, stepUpExpiresAt = expiresAt?.Subtract(SessionPolicy.Absolute(configuration, true)).AddMinutes(5) });
    }
}
