using be.Domain.Identity;

namespace be.Application.Abstractions.Identity
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles);
        DateTime GetAccessTokenExpiry();
    }
}
