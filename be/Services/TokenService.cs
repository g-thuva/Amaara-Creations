using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using be.Models;
using Microsoft.IdentityModel.Tokens;

namespace be.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = GetAccessTokenExpiry();

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetAccessTokenExpiry()
        {
            var accessTokenMinutes = Convert.ToDouble(
                _configuration["Authentication:AccessTokenMinutes"] ??
                _configuration["Jwt:AccessTokenMinutes"] ??
                "15");

            return DateTime.UtcNow.AddMinutes(accessTokenMinutes);
        }
    }
}

