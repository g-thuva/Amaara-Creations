using be.Domain.Identity;

namespace be.Application.Abstractions.Identity
{
    public record RefreshTokenIssueResult(string RawToken, RefreshSession Session);
    public record RefreshTokenRotationResult(string RawToken, RefreshSession Session, User User, IList<string> Roles);
    public interface IRefreshSessionService
    {
        Task<RefreshTokenIssueResult> CreateSessionAsync(User user, SessionMetadata metadata, CancellationToken cancellationToken = default);
        Task<RefreshTokenRotationResult?> RotateAsync(string rawRefreshToken, SessionMetadata metadata, CancellationToken cancellationToken = default);
        Task RevokeAsync(string? rawRefreshToken, CancellationToken cancellationToken = default);
        Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
        string GenerateRawRefreshToken();
        string HashRefreshToken(string rawRefreshToken);
    }
}
