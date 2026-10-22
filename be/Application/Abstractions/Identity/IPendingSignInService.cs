using be.Domain.Identity;

namespace be.Application.Abstractions.Identity;

public sealed record SignInView(string Status, DateTimeOffset ExpiresAt, AuthenticatorEnrollment? Enrollment = null, IReadOnlyList<string>? RecoveryCodes = null, bool Mandatory = false);
public sealed record SignInStep(SignInView View, string? Ticket = null, User? VerifiedUser = null, DateTimeOffset? VerifiedAt = null, bool Management = false, string? ErrorCode = null, bool SecurityChanged = false, string? ChangedUserId = null);
public interface IPendingSignInService
{
    Task<SignInStep> BeginSignInAsync(User user, IEnumerable<string> verifiedRoles, CancellationToken ct);
    Task<SignInStep> AdvanceSignInAsync(string? ticket, string operation, string? code, string? recoveryCode, bool savedCodes, CancellationToken ct);
}
