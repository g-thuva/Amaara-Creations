using be.Domain.Identity;

namespace be.Application.Abstractions.Identity
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles, string? sessionFamily = null, DateTimeOffset? secondFactorAt = null);
        DateTime GetAccessTokenExpiry();
    }
}
