using be.Domain.Identity;

namespace be.Application.Abstractions.Identity;

public sealed record SecondFactorStatus(bool Enabled, int RecoveryCodesLeft, bool Mandatory);
public sealed record AuthenticatorEnrollment(string SharedKey, string AuthenticatorUri, DateTimeOffset ExpiresAt);
public interface ISecondFactorService
{
    Task<SecondFactorStatus> StatusAsync(User user, CancellationToken ct);
    Task<AuthenticatorEnrollment?> BeginAsync(User user, string password, string? code, string? recoveryCode, CancellationToken ct);
    Task<IReadOnlyList<string>?> ConfirmAsync(User user, string password, string code, CancellationToken ct);
    Task<bool> VerifyAsync(User user, string? code, string? recoveryCode, CancellationToken ct);
    Task<IReadOnlyList<string>?> RegenerateAsync(User user, string password, string code, CancellationToken ct);
    Task<bool> DisableAsync(User user, string password, string? code, string? recoveryCode, CancellationToken ct);
    Task<bool> RecoveryRequiredAsync(User user, CancellationToken ct);
    Task<DateTimeOffset> ApproveRecoveryAsync(User user, string reason, CancellationToken ct);
}
