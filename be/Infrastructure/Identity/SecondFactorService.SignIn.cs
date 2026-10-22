using System.Security.Cryptography;
using System.Text.Json;
using be.Application.Abstractions.Identity;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace be.Infrastructure.Identity;

public sealed partial class SecondFactorService
{
    private const string SignInProvider = "Amaara.SignIn.v1";
    private const string SignInName = "Pending";
    private sealed record Ticket(string UserId, string Nonce, long Expires);
    private sealed record Pending(string NonceHash, string Stamp, string Roles, long Expires, string Stage, int Attempts = 0, long? VerifiedAt = null, IReadOnlyList<string>? Codes = null);
    private IDataProtector TicketProtector => protection.CreateProtector("Amaara.SignInTicket.v1");
    private IDataProtector PendingProtector => protection.CreateProtector("Amaara.SignInState.v1");
    private static string RoleSet(IEnumerable<string> roles) => string.Join('|', roles.Order(StringComparer.Ordinal));
    private static CommerceException Expired() => new("MFA_CHALLENGE_EXPIRED", "Your sign-in expired or changed. Please enter your email and password again.", 401);

    public Task<SignInStep> BeginSignInAsync(User user, IEnumerable<string> verifiedRoles, CancellationToken ct)
    {
        var verifiedStamp = user.SecurityStamp;
        return Locked(user, async () =>
        {
            if (user.SecurityStamp != verifiedStamp || !user.EmailConfirmed || await users.IsLockedOutAsync(user)) throw Expired();
            var roles = await users.GetRolesAsync(user);
            if (RoleSet(roles) != RoleSet(verifiedRoles)) throw Expired();
            var recoveryRequired = await RecoveryRequiredAsync(user, ct);
            if (recoveryRequired)
            {
                var approval = await Read<RecoveryApproval>(user, "RecoveryApproval");
                if (approval == null || approval.Stamp != user.SecurityStamp || approval.Expires <= clock.GetUtcNow().ToUnixTimeSeconds())
                    throw new CommerceException("MFA_RECOVERY_APPROVAL_EXPIRED", "Your recovery approval expired or changed. Ask another owner to approve recovery again.");
            }
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expiry = clock.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds();
            var state = new Pending(Hash(nonce), user.SecurityStamp!, RoleSet(roles), expiry, user.TwoFactorEnabled && !recoveryRequired ? "verify" : "enroll");
            AuthenticatorEnrollment? enrollment = null;
            if (state.Stage == "enroll")
            {
                enrollment = await BeginCore(user, ct);
                state = state with { Expires = Math.Min(expiry, enrollment.ExpiresAt.ToUnixTimeSeconds()) };
            }
            await SavePending(user, state);
            return new SignInStep(View(state, enrollment), ProtectTicket(new(user.Id, nonce, state.Expires)));
        }, ct);
    }

    public async Task<SignInStep> AdvanceSignInAsync(string? protectedTicket, string operation, string? code, string? recoveryCode, bool savedCodes, CancellationToken ct)
    {
        Ticket ticket;
        try { ticket = JsonSerializer.Deserialize<Ticket>(TicketProtector.Unprotect(protectedTicket ?? "")) ?? throw Expired(); }
        catch (Exception error) when (error is CryptographicException or JsonException or ArgumentException) { throw Expired(); }
        if (ticket.Expires <= clock.GetUtcNow().ToUnixTimeSeconds()) throw Expired();
        var user = await users.FindByIdAsync(ticket.UserId) ?? throw Expired();
        return await Locked(user, async () =>
        {
            var state = await LoadPending(user);
            var roles = await users.GetRolesAsync(user);
            if (state == null || state.Expires <= clock.GetUtcNow().ToUnixTimeSeconds() || state.Stamp != user.SecurityStamp || state.Roles != RoleSet(roles) ||
                !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(state.NonceHash), Convert.FromHexString(Hash(ticket.Nonce))) ||
                !user.EmailConfirmed || await users.IsLockedOutAsync(user)) throw Expired();
            if (!roles.Any(AppRoles.AdministrativeRoles.Contains) && !roles.Contains(AppRoles.Customer)) throw Expired();

            if (operation == "cancel")
            {
                Ensure(await users.RemoveAuthenticationTokenAsync(user, SignInProvider, SignInName));
                return new SignInStep(new("credentials", clock.GetUtcNow()));
            }
            if (operation == "status") return new SignInStep(await PendingView(user, state));

            if (operation == "verify" && state.Stage == "verify")
            {
                var stamp = user.SecurityStamp;
                if (!await VerifyCore(user, code, recoveryCode)) return await FailedCode(user, state);
                if (user.SecurityStamp != stamp) throw Expired();
                if (!string.IsNullOrWhiteSpace(recoveryCode))
                {
                    if (!roles.Any(AppRoles.AdministrativeRoles.Contains))
                    {
                        Ensure(await users.ResetAccessFailedCountAsync(user));
                        Ensure(await users.RemoveAuthenticationTokenAsync(user, SignInProvider, SignInName));
                        return new SignInStep(new("authenticated", clock.GetUtcNow()), VerifiedUser: user);
                    }
                    // Possession of a one-use recovery code authorizes repair,
                    // not operations. Keep the active authenticator until its
                    // replacement's first TOTP is verified.
                    var enrollment = await BeginCore(user, ct);
                    state = state with { Stage = "enroll", Attempts = 0, Expires = Math.Min(state.Expires, enrollment.ExpiresAt.ToUnixTimeSeconds()) };
                    await SavePending(user, state);
                    await Record("AuthenticatorRecoveryVerified", user, ct);
                    return new SignInStep(View(state, enrollment));
                }
                Ensure(await users.ResetAccessFailedCountAsync(user));
                return await CompletePending(user, state, roles, clock.GetUtcNow());
            }

            if (operation == "confirm" && state.Stage == "enroll")
            {
                var codes = await ConfirmCore(user, code ?? "", ct);
                if (codes == null) return await FailedCode(user, state);
                state = state with { Stamp = user.SecurityStamp!, Stage = "recovery_codes", Codes = codes, Attempts = 0, VerifiedAt = clock.GetUtcNow().ToUnixTimeSeconds(), Expires = clock.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds() };
                await SavePending(user, state);
                return new SignInStep(View(state), ProtectTicket(ticket with { Expires = state.Expires }), SecurityChanged: true, ChangedUserId: user.Id);
            }
            if (operation == "complete" && state.Stage == "recovery_codes" && savedCodes && state.VerifiedAt.HasValue)
                return await CompletePending(user, state, roles, DateTimeOffset.FromUnixTimeSeconds(state.VerifiedAt.Value));
            throw new CommerceException("MFA_STEP_INVALID", "Complete the current security step before continuing.");
        }, ct);
    }

    private async Task<SignInStep> CompletePending(User user, Pending state, IList<string> roles, DateTimeOffset verifiedAt)
    {
        if (!user.TwoFactorEnabled || state.Stamp != user.SecurityStamp) throw Expired();
        Ensure(await users.RemoveAuthenticationTokenAsync(user, SignInProvider, SignInName));
        return new SignInStep(new("authenticated", clock.GetUtcNow()), VerifiedUser: user, VerifiedAt: verifiedAt, Management: roles.Any(AppRoles.AdministrativeRoles.Contains));
    }

    private async Task<SignInStep> FailedCode(User user, Pending state)
    {
        await Record("SignInMfaRejected", user, default);
        state = state with { Attempts = state.Attempts + 1 };
        if (state.Attempts >= 5) Ensure(await users.RemoveAuthenticationTokenAsync(user, SignInProvider, SignInName));
        else await SavePending(user, state);
        // Return the error until the enclosing transaction commits the failed
        // attempt/lockout. The HTTP layer throws only after that commit.
        return new SignInStep(View(state), ErrorCode: state.Attempts >= 5 ? "MFA_CHALLENGE_EXPIRED" : "MFA_CODE_INVALID");
    }

    private async Task<SignInView> PendingView(User user, Pending state)
    {
        AuthenticatorEnrollment? enrollment = null;
        if (state.Stage == "enroll")
        {
            var pending = await Read<Enrollment>(user, "Enrollment");
            if (pending == null || pending.Stamp != user.SecurityStamp || pending.Expires <= clock.GetUtcNow().ToUnixTimeSeconds()) throw Expired();
            // BeginCore reuses this verified existing attempt; it cannot create a
            // new QR here because the existing record was checked above.
            enrollment = await BeginCore(user, default);
        }
        return View(state, enrollment);
    }
    private static SignInView View(Pending state, AuthenticatorEnrollment? enrollment = null) => new(state.Stage, DateTimeOffset.FromUnixTimeSeconds(state.Expires), enrollment, state.Stage == "recovery_codes" ? state.Codes : null, state.Roles.Split('|').Any(AppRoles.AdministrativeRoles.Contains));
    private string ProtectTicket(Ticket ticket) => TicketProtector.Protect(JsonSerializer.Serialize(ticket));
    private async Task SavePending(User user, Pending state) => Ensure(await users.SetAuthenticationTokenAsync(user, SignInProvider, SignInName, PendingProtector.Protect(JsonSerializer.Serialize(state))));
    private async Task<Pending?> LoadPending(User user)
    {
        var value = await users.GetAuthenticationTokenAsync(user, SignInProvider, SignInName);
        if (value == null) return null;
        try { return JsonSerializer.Deserialize<Pending>(PendingProtector.Unprotect(value)); }
        catch (Exception error) when (error is CryptographicException or JsonException) { return null; }
    }
}
