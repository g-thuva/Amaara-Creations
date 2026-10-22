using be.Application.Features.Identity;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Identity;

public static class SignInResponse
{
    public static IActionResult Present(ControllerBase controller, RefreshCookie cookie, be.Application.Features.Identity.SignInResult result, bool managementEntry = false)
    {
        controller.Response.Headers.CacheControl = "private, no-store";
        if (result.Pending is { } pending)
        {
            if (pending.Ticket != null)
            {
                if (managementEntry) { cookie.Clear(controller.Response, true); cookie.ClearPending(controller.Response); }
                else cookie.ClearAll(controller.Response);
                cookie.WritePending(controller.Response, pending.Ticket, pending.View.ExpiresAt);
            }
            if (pending.View.Status == "credentials") cookie.ClearPending(controller.Response);
            return controller.StatusCode(StatusCodes.Status202Accepted, pending.View);
        }
        var sessions = result.Sessions ?? throw new InvalidOperationException("Sign-in did not produce a completion state.");
        cookie.ClearPending(controller.Response);
        if (sessions.Storefront != null) cookie.Write(controller.Response, sessions.Storefront);
        if (sessions.Management != null) cookie.Write(controller.Response, sessions.Management, management: true);
        return controller.Ok(managementEntry ? sessions.Management!.Response : sessions.Storefront!.Response);
    }
}
