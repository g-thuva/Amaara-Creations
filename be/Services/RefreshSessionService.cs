using System.Security.Cryptography;
using System.Text;
using be.Data;
using be.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Services
{
    public class RefreshSessionService : IRefreshSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public RefreshSessionService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<RefreshTokenIssueResult> CreateSessionAsync(User user, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var rawToken = GenerateRawRefreshToken();
            var session = new RefreshSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenDays()),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.RefreshSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            return new RefreshTokenIssueResult(rawToken, session);
        }

        public async Task<RefreshTokenRotationResult?> RotateAsync(string rawRefreshToken, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashRefreshToken(rawRefreshToken);
            var session = await _context.RefreshSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

            if (session == null)
            {
                return null;
            }

            if (session.RevokedAt.HasValue)
            {
                await RevokeFamilyAsync(session.FamilyId, cancellationToken);
                return null;
            }

            if (session.ExpiresAt <= DateTime.UtcNow || session.User == null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return null;
            }

            var user = await _userManager.FindByIdAsync(session.UserId);
            if (user == null || !user.EmailConfirmed)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return null;
            }

            var rawReplacement = GenerateRawRefreshToken();
            var replacement = new RefreshSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(rawReplacement),
                FamilyId = session.FamilyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenDays()),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                LastUsedAt = DateTime.UtcNow
            };

            session.RevokedAt = DateTime.UtcNow;
            session.LastUsedAt = DateTime.UtcNow;
            _context.RefreshSessions.Add(replacement);
            await _context.SaveChangesAsync(cancellationToken);

            session.ReplacedBySessionId = replacement.Id;
            await _context.SaveChangesAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            return new RefreshTokenRotationResult(rawReplacement, replacement, user, roles);
        }

        public async Task RevokeAsync(string? rawRefreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return;
            }

            var tokenHash = HashRefreshToken(rawRefreshToken);
            var session = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);
            if (session == null || session.RevokedAt.HasValue)
            {
                return;
            }

            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.RefreshSessions
                .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public string GenerateRawRefreshToken()
        {
            Span<byte> bytes = stackalloc byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string HashRefreshToken(string rawRefreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken));
            return Convert.ToHexString(bytes);
        }

        private async Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken)
        {
            var familySessions = await _context.RefreshSessions
                .Where(s => s.FamilyId == familyId && s.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var familySession in familySessions)
            {
                familySession.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private int GetRefreshTokenDays()
        {
            return _configuration.GetValue<int?>("Authentication:RefreshTokenDays")
                ?? _configuration.GetValue<int?>("Jwt:RefreshTokenDays")
                ?? 21;
        }
    }
}
