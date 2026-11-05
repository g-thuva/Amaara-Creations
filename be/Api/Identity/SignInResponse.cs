using be.Application.Features.Identity;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Identity;

public static class SignInResponse
{
    public static async Task<IActionResult> State(ControllerBase controller, IdentityService identity, RefreshCookie cookie, CancellationToken ct, bool managementEntry = false)
    {
        controller.Response.Headers.CacheControl = "private, no-store";
        var pending = cookie.ReadPending(controller.Request, managementEntry);
        if (!string.IsNullOrWhiteSpace(pending))
            return Present(controller, cookie, await identity.AdvanceSignInAsync(pending, "status", null, null, false, ct, managementEntry), managementEntry);

        // Reading anonymous state is not a failed authentication. Renewal stays
        // a separate POST, protected by the existing Origin checks.
        var status = string.IsNullOrWhiteSpace(cookie.Read(controller.Request, managementEntry)) ? "credentials" : "session";
        return controller.Accepted(new be.Application.Abstractions.Identity.SignInView(status, DateTimeOffset.UtcNow));
    }

    public static IActionResult Present(ControllerBase controller, RefreshCookie cookie, be.Application.Features.Identity.SignInResult result, bool managementEntry = false)
    {
        controller.Response.Headers.CacheControl = "private, no-store";
        if (result.Pending is { } pending)
        {
            if (pending.Ticket != null)
            {
                // Starting staff MFA on the shared page must preserve any
                // independent shopping session in this browser.
                if (managementEntry || !pending.View.Mandatory) cookie.Clear(controller.Response, managementEntry);
                cookie.ClearPending(controller.Response, managementEntry);
                cookie.WritePending(controller.Response, pending.Ticket, pending.View.ExpiresAt, managementEntry);
            }
            if (pending.View.Status == "credentials") cookie.ClearPending(controller.Response, managementEntry);
            return controller.StatusCode(StatusCodes.Status202Accepted, pending.View);
        }
        var sessions = result.Sessions ?? throw new InvalidOperationException("Sign-in did not produce a completion state.");
        cookie.ClearPending(controller.Response, managementEntry);
        if (sessions.Storefront != null) cookie.Write(controller.Response, sessions.Storefront);
        if (sessions.Management != null) cookie.Write(controller.Response, sessions.Management, management: true);
        var completed = managementEntry ? sessions.Management : sessions.Storefront ?? sessions.Management;
        return controller.Ok((completed ?? throw new InvalidOperationException("Sign-in did not produce a session.")).Response);
    }
}
