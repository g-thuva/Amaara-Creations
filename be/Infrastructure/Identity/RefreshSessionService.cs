using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Identity
{
    public class RefreshSessionService : IRefreshSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        public RefreshSessionService(ApplicationDbContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public Task<RefreshTokenIssueResult> CreateSessionAsync(User user, SessionMetadata metadata, CancellationToken cancellationToken = default)
            => WithSessionLockAsync(user.Id, () => CreateSessionCoreAsync(user, metadata, cancellationToken), cancellationToken);

        private async Task<RefreshTokenIssueResult> CreateSessionCoreAsync(User user, SessionMetadata metadata, CancellationToken cancellationToken)
        {
            var rawToken = GenerateRawRefreshToken();
            var session = new RefreshSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenDays()),
                UserAgent = metadata.UserAgent,
                IpAddress = metadata.IpAddress
            };
            _context.RefreshSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
            return new RefreshTokenIssueResult(rawToken, session);
        }

        public async Task<RefreshTokenRotationResult?> RotateAsync(string rawRefreshToken, SessionMetadata metadata, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashRefreshToken(rawRefreshToken);
            var userId = await _context.RefreshSessions.AsNoTracking().Where(s => s.TokenHash == tokenHash).Select(s => s.UserId).SingleOrDefaultAsync(cancellationToken);
            if (userId == null) return null;
            return await WithSessionLockAsync(userId, () => RotateCoreAsync(tokenHash, metadata, cancellationToken), cancellationToken);
        }

        private async Task<RefreshTokenRotationResult?> RotateCoreAsync(string tokenHash, SessionMetadata metadata, CancellationToken cancellationToken)
        {
            var session = await _context.RefreshSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);
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
            if (user == null || !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user))
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
                UserAgent = metadata.UserAgent,
                IpAddress = metadata.IpAddress,
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
            var userId = await _context.RefreshSessions.AsNoTracking().Where(s => s.TokenHash == tokenHash).Select(s => s.UserId).SingleOrDefaultAsync(cancellationToken);
            if (userId == null) return;
            await WithSessionLockAsync(userId, async () =>
            {
                await RevokeCoreAsync(tokenHash, cancellationToken);
                return true;
            }, cancellationToken);
        }

        private async Task RevokeCoreAsync(string tokenHash, CancellationToken cancellationToken)
        {
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
            await WithSessionLockAsync(userId, async () =>
            {
                await RevokeAllCoreAsync(userId, cancellationToken);
                return true;
            }, cancellationToken);
        }

        private async Task RevokeAllCoreAsync(string userId, CancellationToken cancellationToken)
        {
            var sessions = await _context.RefreshSessions.Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> RevokeFamilyForUserAsync(string userId, string familyId, CancellationToken cancellationToken = default) =>
            WithSessionLockAsync(userId, async () =>
            {
                var family = await _context.RefreshSessions.Where(s => s.UserId == userId && s.FamilyId == familyId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow).ToListAsync(cancellationToken);
                foreach (var session in family) session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return family.Count > 0;
            }, cancellationToken);

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
            var familySessions = await _context.RefreshSessions.Where(s => s.FamilyId == familyId && s.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var familySession in familySessions)
            {
                familySession.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // A database-owned per-user lock coordinates rotation, logout and replay across API instances.
        // The replacement insert and predecessor link commit atomically; no external I/O occurs here.
        private async Task<T> WithSessionLockAsync<T>(string userId, Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            if (!_context.Database.IsSqlServer()) return await operation();
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var resource = "Amaara.RefreshSessions." + HashRefreshToken(userId);
            await _context.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51001, 'Session lock unavailable', 1;", cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        private int GetRefreshTokenDays()
        {
            return _configuration.GetValue<int?>("Authentication:RefreshTokenDays") ?? _configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 21;
        }
    }
}
