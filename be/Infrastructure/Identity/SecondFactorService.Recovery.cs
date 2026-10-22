using be.Domain.Common;
using be.Domain.Identity;

namespace be.Infrastructure.Identity;

public sealed partial class SecondFactorService
{
    public async Task<bool> RecoveryRequiredAsync(User user, CancellationToken ct) =>
        await users.GetAuthenticationTokenAsync(user, Provider, "RecoveryApproval") != null;

    public Task<DateTimeOffset> ApproveRecoveryAsync(User user, string reason, CancellationToken ct) => Locked(user, async () =>
    {
        if (!user.EmailConfirmed || !user.TwoFactorEnabled || !(await users.GetRolesAsync(user)).Any(AppRoles.AdministrativeRoles.Contains))
            throw new CommerceException("STAFF_RECOVERY_INVALID", "Choose a verified staff account with an existing authenticator.", 409);
        var expires = clock.GetUtcNow().AddMinutes(15);
        Ensure(await users.UpdateSecurityStampAsync(user));
        await sessions.RevokeAllForUserAsync(user.Id, ct);
        Ensure(await users.RemoveAuthenticationTokenAsync(user, SignInProvider, SignInName));
        await Write(user, "RecoveryApproval", new RecoveryApproval(user.SecurityStamp!, expires.ToUnixTimeSeconds()));
        await audit.RecordAsync("StaffMfaRecoveryApproved", nameof(User), user.Id, null, new { Reason = reason, ExpiresAt = expires }, actor.Audit, ct);
        return expires;
    }, ct);
}
