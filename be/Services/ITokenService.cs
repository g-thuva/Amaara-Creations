using be.Models;

namespace be.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user, IList<string> roles);
        string GenerateRefreshToken();
    }
}

