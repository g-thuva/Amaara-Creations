using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace be.Infrastructure.Identity;

// Existing unprefixed Identity keys remain readable. New/replaced keys are
// protected without changing their value or introducing another key table.
public sealed class ProtectedIdentityUserStore(ApplicationDbContext context, IdentityErrorDescriber errors, IDataProtectionProvider protection)
    : UserStore<User, IdentityRole, ApplicationDbContext, string>(context, errors)
{
    private const string Prefix = "protected:v1:";
    private readonly IDataProtector protector = protection.CreateProtector("Amaara.AuthenticatorKey.v1");
    private (string UserId, string Key)? verification;

    public override Task SetAuthenticatorKeyAsync(User user, string key, CancellationToken cancellationToken) =>
        base.SetAuthenticatorKeyAsync(user, Prefix + protector.Protect(key), cancellationToken);

    public override async Task<string?> GetAuthenticatorKeyAsync(User user, CancellationToken cancellationToken)
    {
        if (verification is { } pending && pending.UserId == user.Id) return pending.Key;
        var key = await base.GetAuthenticatorKeyAsync(user, cancellationToken);
        if (key?.StartsWith(Prefix, StringComparison.Ordinal) != true) return key;
        try { return protector.Unprotect(key[Prefix.Length..]); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }

    // Only this scoped store is overridden, only during native provider
    // validation. The pending key never replaces the persisted active key.
    internal async Task<bool> VerifyPendingAsync(UserManager<User> manager, User user, string key, string code)
    {
        var previous = verification;
        verification = (user.Id, key);
        try { return await manager.VerifyTwoFactorTokenAsync(user, manager.Options.Tokens.AuthenticatorTokenProvider, code); }
        finally { verification = previous; }
    }

    internal async Task ProtectExistingKeyAsync(User user)
    {
        var key = await base.GetAuthenticatorKeyAsync(user, default);
        if (!string.IsNullOrEmpty(key) && !key.StartsWith(Prefix, StringComparison.Ordinal)) await SetAuthenticatorKeyAsync(user, key, default);
    }
}
