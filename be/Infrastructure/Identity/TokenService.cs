using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Application.Features.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace be.Infrastructure.Identity
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, IList<string> roles, string? sessionFamily = null, DateTimeOffset? secondFactorAt = null, bool management = false, DateTime? sessionExpiresAt = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };
            if (sessionFamily != null) claims.Add(new Claim("session_family", sessionFamily));
            claims.Add(new Claim("session_kind", management ? "management" : "storefront"));
            if (secondFactorAt.HasValue)
            {
                claims.Add(new Claim("amr", "mfa"));
                claims.Add(new Claim("mfa_time", secondFactorAt.Value.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = GetAccessTokenExpiry(roles.Any(AppRoles.AdministrativeRoles.Contains), sessionExpiresAt);
            var token = new JwtSecurityToken(issuer: _configuration["Jwt:Issuer"], audience: _configuration["Jwt:Audience"], claims: claims, expires: expiresAt, signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetAccessTokenExpiry(bool privileged = false, DateTime? sessionExpiresAt = null)
        {
            var accessTokenMinutes = SessionPolicy.AccessMinutes(_configuration, privileged);
            var expiry = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
            return sessionExpiresAt.HasValue && sessionExpiresAt < expiry ? DateTime.SpecifyKind(sessionExpiresAt.Value, DateTimeKind.Utc) : expiry;
        }
    }
}
