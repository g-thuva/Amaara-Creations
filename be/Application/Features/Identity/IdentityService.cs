using System.Security.Claims;
using be.Application.Abstractions.Email;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Audit;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace be.Application.Features.Identity;
public sealed class IdentityService(
        UserManager<User> users,
        SignInManager<User> signIn,
        ITokenService tokens,
        IRefreshSessionService sessions,
        ISecondFactorService secondFactor,
        IPendingSignInService pendingSignIn,
        IEmailService email,
        IConfiguration configuration,
        IRequestActor actor,
        IAuditService audit)
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await users.FindByEmailAsync(request.Email) != null)
            return;
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim(),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        EnsureSuccess(await users.CreateAsync(user, request.Password), "REGISTRATION_FAILED", "Registration failed.");
        await users.AddToRoleAsync(user, AppRoles.Customer);
        await SendConfirmationAsync(user, ct);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await users.FindByIdAsync(request.UserId);
        if (user == null || !(await users.ConfirmEmailAsync(user, Uri.UnescapeDataString(request.Token))).Succeeded)
            throw new CommerceException("EMAIL_CONFIRMATION_INVALID", "Invalid or expired verification link.");
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
            await SendConfirmationAsync(user, ct);
    }

    public async Task<SignInResult> SignInAsync(LoginRequest request, CancellationToken ct, bool managementEntry = false)
    {
        VerifiedLogin verified;
        try { verified = await VerifyPasswordAsync(request, ct, managementEntry); }
        catch (CommerceException failure)
        {
            await audit.RecordAsync(managementEntry ? "StaffSignInFailed" : "SignInFailed", nameof(User), null, null, new { Outcome = failure.Code }, actor.Audit, ct);
            throw;
        }
        if (verified.User.TwoFactorEnabled || verified.Roles.Any(AppRoles.AdministrativeRoles.Contains))
        {
            // Compatibility for existing clients that submit both factors at
            // once: use the same verifier/policies, without creating a pending
            // challenge that could race another complete sign-in attempt.
            var privileged = verified.Roles.Any(AppRoles.AdministrativeRoles.Contains);
            if (verified.User.TwoFactorEnabled && !await secondFactor.RecoveryRequiredAsync(verified.User, ct) && ((!string.IsNullOrWhiteSpace(request.AuthenticatorCode) && string.IsNullOrWhiteSpace(request.RecoveryCode)) || (!privileged && !string.IsNullOrWhiteSpace(request.RecoveryCode))))
            {
                if (!await secondFactor.VerifyAsync(verified.User, request.AuthenticatorCode, request.RecoveryCode, ct)) throw InvalidLogin();
                await users.ResetAccessFailedCountAsync(verified.User);
                return new(Sessions: await IssueSignInAsync(verified with { SecondFactorAt = string.IsNullOrWhiteSpace(request.AuthenticatorCode) ? null : DateTimeOffset.UtcNow }, ct, managementEntry));
            }
            var pending = await pendingSignIn.BeginSignInAsync(verified.User, verified.Roles, ct);
            // Existing API clients may supply both factors in one request. They
            // use this same transaction and cannot obtain a pending JWT.
            if (pending.View.Status == "verify" && (!string.IsNullOrWhiteSpace(request.AuthenticatorCode) || !string.IsNullOrWhiteSpace(request.RecoveryCode)))
            {
                var advanced = await AdvanceSignInAsync(pending.Ticket, "verify", request.AuthenticatorCode, request.RecoveryCode, false, ct);
                return advanced.Pending is { Ticket: null } next ? advanced with { Pending = next with { Ticket = pending.Ticket } } : advanced;
            }
            return new(Pending: pending);
        }
        if (!string.IsNullOrWhiteSpace(request.AuthenticatorCode) || !string.IsNullOrWhiteSpace(request.RecoveryCode)) throw InvalidLogin();
        await users.ResetAccessFailedCountAsync(verified.User);
        return new(Sessions: await IssueSignInAsync(verified, ct));
    }

    public async Task<SignInResult> AdvanceSignInAsync(string? ticket, string operation, string? code, string? recovery, bool savedCodes, CancellationToken ct)
    {
        var step = await pendingSignIn.AdvanceSignInAsync(ticket, operation, code, recovery, savedCodes, ct);
        if (step.ErrorCode != null) throw new CommerceException(step.ErrorCode, step.ErrorCode == "MFA_CHALLENGE_EXPIRED" ? "Your sign-in expired. Please start again." : "That code didn't work. Check your authenticator app and try the current code.", step.ErrorCode == "MFA_CHALLENGE_EXPIRED" ? 401 : 400);
        if (step.SecurityChanged)
        {
            // The MFA transaction has already rotated the stamp and revoked
            // refresh families before allowing the saved-code acknowledgment.
            var user = await users.FindByIdAsync(step.ChangedUserId ?? "");
            if (user != null) await NotifyAuthenticatorChangedAsync(user, ct);
        }
        if (step.VerifiedUser == null) return new(Pending: step);
        var current = await users.FindByIdAsync(step.VerifiedUser.Id) ?? throw InvalidLogin();
        var roles = await users.GetRolesAsync(current);
        if (step.Management != roles.Any(AppRoles.AdministrativeRoles.Contains)) throw InvalidLogin();
        if (!current.EmailConfirmed || await users.IsLockedOutAsync(current) || !current.TwoFactorEnabled || current.SecurityStamp != step.VerifiedUser.SecurityStamp) throw InvalidLogin();
        if (roles.Any(AppRoles.AdministrativeRoles.Contains) && !configuration.GetValue<bool>("Administration:Enabled")) throw new CommerceException("MANAGEMENT_DISABLED", "Staff sign-in is unavailable in this environment.", 503);
        if (!roles.Any(AppRoles.AdministrativeRoles.Contains) && !roles.Contains(AppRoles.Customer)) throw InvalidLogin();
        return new(Sessions: await IssueSignInAsync(new(current, roles, step.VerifiedAt, current.SecurityStamp), ct));
    }

    private async Task<RoutedSignIn> IssueSignInAsync(VerifiedLogin verified, CancellationToken ct, bool managementOnly = false)
    {
        if (verified.Stamp != verified.User.SecurityStamp || !verified.Roles.Order(StringComparer.Ordinal).SequenceEqual((await users.GetRolesAsync(verified.User)).Order(StringComparer.Ordinal))) throw InvalidLogin();
        var staff = verified.Roles.Any(AppRoles.AdministrativeRoles.Contains);
        if (staff && !verified.SecondFactorAt.HasValue) throw InvalidLogin();
        var storefront = managementOnly ? null : await CreateSessionAsync(verified, ct, management: false);
        AuthenticatedSession? management = null;
        try
        {
            if (staff)
            {
                management = await CreateSessionAsync(verified, ct, management: true);
                await audit.RecordAsync("StaffSignInSucceeded", nameof(User), verified.User.Id, null, new { MfaVerified = management.StaffAssurance != null, Entry = managementOnly ? "management-compatibility" : "shared" }, actor.Audit with { UserId = verified.User.Id }, ct);
            }
            return new(storefront, management);
        }
        catch
        {
            if (storefront != null) await sessions.RevokeAsync(storefront.RefreshToken, CancellationToken.None);
            if (management != null) await sessions.RevokeAsync(management.RefreshToken, CancellationToken.None);
            throw;
        }
    }

    private sealed record VerifiedLogin(User User, IList<string> Roles, DateTimeOffset? SecondFactorAt, string? Stamp);

    private async Task<VerifiedLogin> VerifyPasswordAsync(LoginRequest request, CancellationToken ct, bool management)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user == null)
            throw InvalidLogin();
        if (await users.IsLockedOutAsync(user))
            throw InvalidLogin();
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            throw InvalidLogin();
        }
        if (!user.EmailConfirmed)
            throw new CommerceException("EMAIL_CONFIRMATION_REQUIRED", "Email verification is required before signing in.", 401, new Dictionary<string, object?> { ["requiresEmailConfirmation"] = true });
        var roles = await users.GetRolesAsync(user);
        if (management && !roles.Any(AppRoles.AdministrativeRoles.Contains))
            throw new CommerceException("MANAGEMENT_ACCESS_DENIED", "This account is not authorized for management.", 403);
        if (roles.Count == 0 || roles.Any(role => role != AppRoles.Customer && !AppRoles.AdministrativeRoles.Contains(role)))
            throw new CommerceException("ACCOUNT_ACCESS_DENIED", "This account is not available for sign-in.", 403);
        if (roles.Any(AppRoles.AdministrativeRoles.Contains) && !configuration.GetValue<bool>("Administration:Enabled"))
            throw new CommerceException("MANAGEMENT_DISABLED", "Management access is disabled. Contact the operator.", 503);
        return new(user, roles, null, user.SecurityStamp);
    }

    private async Task<AuthenticatedSession> CreateSessionAsync(VerifiedLogin verified, CancellationToken ct, bool management)
    {
        var session = await sessions.CreateSessionAsync(verified.User, actor.Session, ct);
        var assurance = verified.SecondFactorAt.HasValue ? new StaffAssurance(verified.User.Id, verified.User.SecurityStamp!, session.Session.FamilyId, verified.SecondFactorAt.Value.ToUnixTimeSeconds()) : null;
        return new(CreateResponse(verified.User, verified.Roles, session.Session.FamilyId, verified.SecondFactorAt, management), session.RawToken, session.Session.ExpiresAt, assurance);
    }

    public async Task<AuthenticatedSession?> RefreshAsync(string? rawToken, CancellationToken ct, StaffAssurance? assurance = null, bool management = false)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var rotation = await sessions.RotateAsync(rawToken, actor.Session, ct);
        if (rotation == null) return null;
        if (management && (!configuration.GetValue<bool>("Administration:Enabled") || !rotation.Roles.Any(AppRoles.AdministrativeRoles.Contains)))
        {
            await sessions.RevokeAsync(rotation.RawToken, ct);
            return null;
        }
        var age = assurance == null ? -1 : DateTimeOffset.UtcNow.ToUnixTimeSeconds() - assurance.VerifiedAt;
        // The protected HttpOnly grant binds assurance to the original session and stamp.
        // Rotation preserves its original time; it can never renew privileged assurance.
        var valid = assurance != null && assurance.UserId == rotation.User.Id && assurance.SecurityStamp == rotation.User.SecurityStamp && assurance.SessionFamily == rotation.Session.FamilyId && age >= 0 && age < 900;
        var secondFactorAt = valid ? DateTimeOffset.FromUnixTimeSeconds(assurance!.VerifiedAt) : (DateTimeOffset?)null;
        return new(CreateResponse(rotation.User, rotation.Roles, rotation.Session.FamilyId, secondFactorAt, management), rotation.RawToken, rotation.Session.ExpiresAt, valid ? assurance : null);
    }

    public async Task LogoutAsync(string? rawToken, CancellationToken ct)
    {
        await sessions.RevokeAsync(rawToken, ct);
        await signIn.SignOutAsync();
    }

    public async Task LogoutAllAsync(CancellationToken ct)
    {
        var id = actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != null)
        {
            await sessions.RevokeAllForUserAsync(id, ct);
            var user = await users.FindByIdAsync(id);
            if (user != null) await users.UpdateSecurityStampAsync(user);
        }
    }

    public async Task<UserDto> CurrentUserAsync()
    {
        var user = await CurrentUser();
        return ToUserDto(user, await users.GetRolesAsync(user));
    }

    public async Task<SecondFactorStatus> SecondFactorStatusAsync(CancellationToken ct) => await secondFactor.StatusAsync(await CurrentUser(), ct);
    public async Task<AuthenticatorEnrollment> BeginSecondFactorAsync(SecondFactorRequest request, CancellationToken ct) =>
        await secondFactor.BeginAsync(await CurrentUser(), request.Password, request.AuthenticatorCode, request.RecoveryCode, ct) ?? throw InvalidSecondFactor();
    public async Task<IReadOnlyList<string>> ConfirmSecondFactorAsync(SecondFactorRequest request, CancellationToken ct)
    {
        var user = await CurrentUser();
        var codes = await secondFactor.ConfirmAsync(user, request.Password, request.AuthenticatorCode ?? "", ct) ?? throw InvalidSecondFactor();
        await NotifyAuthenticatorChangedAsync(user, ct);
        return codes;
    }

    private async Task NotifyAuthenticatorChangedAsync(User user, CancellationToken ct)
    {
        try { await email.SendTransactionAsync(user, "Your Amaara authenticator was updated", "Your authenticator setup was completed and previous sessions were revoked. If this was not you, contact your account administrator immediately.", ct); }
        catch (Exception error) when (error is not OperationCanceledException) { await audit.RecordAsync("AuthenticatorNotificationDeferred", nameof(User), user.Id, null, new { Type = error.GetType().Name }, actor.Audit, ct); }
    }
    public async Task<IReadOnlyList<string>> RegenerateRecoveryAsync(SecondFactorRequest request, CancellationToken ct) =>
        await secondFactor.RegenerateAsync(await CurrentUser(), request.Password, request.AuthenticatorCode ?? "", ct) ?? throw InvalidSecondFactor();
    public async Task DisableSecondFactorAsync(SecondFactorRequest request, CancellationToken ct)
    {
        var user = await CurrentUser();
        if (!await secondFactor.DisableAsync(user, request.Password, request.AuthenticatorCode, request.RecoveryCode, ct)) throw InvalidSecondFactor();
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not { EmailConfirmed: true })
            return;
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await email.SendPasswordResetAsync(user, FrontendUrl("/reset-password", new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = Uri.EscapeDataString(token) }), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email) ?? throw new CommerceException("PASSWORD_RESET_INVALID", "Invalid or expired password reset link.");
        EnsureSuccess(await users.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.NewPassword), "PASSWORD_RESET_INVALID", "Invalid or expired password reset link.");
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await CurrentUser();
        EnsureSuccess(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), "PASSWORD_CHANGE_FAILED", "Password change failed.");
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }

    private async Task<User> CurrentUser()
    {
        var id = actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return (id == null ? null : await users.FindByIdAsync(id)) ?? throw new CommerceException("AUTHENTICATION_REQUIRED", "User is not authenticated.", 401);
    }

    private static CommerceException InvalidLogin() => new("LOGIN_FAILED", "Invalid email or password.", 401);
    private static CommerceException InvalidSecondFactor() => new("SECOND_FACTOR_VERIFICATION_FAILED", "Unable to verify these security details. Check your password and use a new code. If setup expired, start again.");
    private static void EnsureSuccess(IdentityResult result, string code, string message)
    {
        if (!result.Succeeded)
            throw new CommerceException(code, message, details: new Dictionary<string, object?> { ["errors"] = result.Errors.Select(e => e.Description).ToArray() });
    }

    private AuthResponse CreateResponse(User user, IList<string> roles, string sessionFamily, DateTimeOffset? secondFactorAt = null, bool management = false) => new()
    {
        Token = tokens.GenerateToken(user, roles, sessionFamily, secondFactorAt, management),
        User = ToUserDto(user, roles),
        ExpiresAt = tokens.GetAccessTokenExpiry()
    };
    private static UserDto ToUserDto(User user, IList<string> roles) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email ?? string.Empty,
        Phone = user.Phone,
        Address = user.Address,
        AvatarUrl = user.AvatarUrl,
        EmailConfirmed = user.EmailConfirmed,
        Role = roles.FirstOrDefault() ?? string.Empty,
        Roles = roles.ToList()
    };
    private async Task SendConfirmationAsync(User user, CancellationToken ct)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await email.SendEmailConfirmationAsync(user, FrontendUrl("/verify-email", new Dictionary<string, string?> { ["userId"] = user.Id, ["token"] = Uri.EscapeDataString(token), ["email"] = user.Email }), ct);
    }

    private string FrontendUrl(string path, IDictionary<string, string?> query)
    {
        var origin = (configuration["Authentication:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var queryString = string.Join("&", query.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));
        return $"{origin}/#{path}?{queryString}";
    }
}
