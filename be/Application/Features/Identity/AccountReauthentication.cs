using System.Security.Claims;
using be.Application.Abstractions.Identity;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace be.Application.Features.Identity;
public sealed class AccountReauthentication(UserManager<User> users, ISecondFactorService factors, IRequestActor actor)
{
    public async Task<User> Verify(SecondFactorRequest request, CancellationToken ct)
    {
        var id = actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = id == null ? null : await users.FindByIdAsync(id);
        if (user == null) throw new CommerceException("AUTHENTICATION_REQUIRED", "Please sign in.", 401);
        if (await users.IsLockedOutAsync(user)) throw Invalid();
        if (!await users.CheckPasswordAsync(user, request.Password)) { await users.AccessFailedAsync(user); throw Invalid(); }
        if (user.TwoFactorEnabled && !await factors.VerifyAsync(user, request.AuthenticatorCode, request.RecoveryCode, ct)) throw Invalid();
        await users.ResetAccessFailedCountAsync(user);
        return user;
    }
    private static CommerceException Invalid() => new("REAUTHENTICATION_FAILED", "Check your password and, if enabled, a new authenticator or recovery code.");
}
