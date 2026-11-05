using be.Domain.Identity;

namespace be.Application.Abstractions.Identity
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles, string? sessionFamily = null, DateTimeOffset? secondFactorAt = null, bool management = false, DateTime? sessionExpiresAt = null);
        DateTime GetAccessTokenExpiry(bool privileged = false, DateTime? sessionExpiresAt = null);
    }
}
