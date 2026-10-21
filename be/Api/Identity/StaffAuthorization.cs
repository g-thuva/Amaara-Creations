using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace be.Api.Identity;

// Every staff policy shares this requirement, including non-/admin legacy actions.
public sealed record StaffAuthorization(bool Sensitive) : IAuthorizationRequirement;

public sealed class StaffAuthorizationHandler(IConfiguration configuration) : AuthorizationHandler<StaffAuthorization>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StaffAuthorization requirement)
    {
        var seconds = requirement.Sensitive ? 300 : 900;
        if (configuration.GetValue<bool>("Administration:Enabled") &&
            context.User.HasClaim("amr", "mfa") &&
            long.TryParse(context.User.FindFirst("mfa_time")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var authenticatedAt))
        {
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - authenticatedAt;
            if (age >= 0 && age < seconds) context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
