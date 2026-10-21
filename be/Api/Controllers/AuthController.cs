using be.Api.Identity;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/auth")]
public sealed class AuthController(IdentityService identity, RefreshCookie cookie) : ControllerBase
{
    [HttpPost("register"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        await identity.RegisterAsync(request, cancellationToken);
        return Ok(new { message = "Registration successful. Check your email to verify your account before signing in.", requiresEmailConfirmation = true });
    }

    [HttpPost("confirm-email"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await identity.ConfirmEmailAsync(request);
        return Ok(new { message = "Email verified. You can now sign in." });
    }

    [HttpPost("resend-confirmation"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        await identity.ResendConfirmationAsync(request, cancellationToken);
        return Ok(new { message = "If an account requires verification, a confirmation email has been sent." });
    }

    [HttpPost("login"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await identity.LoginAsync(request, cancellationToken);
        cookie.Write(Response, session);
        return Ok(session.Response);
    }

    [HttpPost("refresh"), EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var session = await identity.RefreshAsync(cookie.Read(Request), cancellationToken, cookie.ReadAssurance(Request));
        if (session == null)
        {
            cookie.Clear(Response);
            return Unauthorized(new { message = "Session expired." });
        }

        cookie.Write(Response, session);
        return Ok(session.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await identity.LogoutAsync(cookie.Read(Request), cancellationToken);
        cookie.Clear(Response);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("logout-all"), Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await identity.LogoutAllAsync(cancellationToken);
        cookie.Clear(Response);
        return Ok(new { message = "All sessions were revoked." });
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> GetCurrentUser() => Ok(await identity.CurrentUserAsync());
    [HttpPost("forgot-password"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await identity.ForgotPasswordAsync(request, cancellationToken);
        return Ok(new { message = "If an account with that email exists, password reset instructions have been sent." });
    }

    [HttpPost("reset-password"), EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await identity.ResetPasswordAsync(request, cancellationToken);
        cookie.Clear(Response);
        return Ok(new { message = "Password reset successfully. Please sign in again." });
    }

    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await identity.ChangePasswordAsync(request, cancellationToken);
        cookie.Clear(Response);
        return Ok(new { message = "Password changed successfully. Please sign in again." });
    }
}
