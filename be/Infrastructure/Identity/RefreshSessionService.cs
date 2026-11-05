using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Application.Features.Identity;
using be.Domain.Common;
using be.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Identity
{
    public class RefreshSessionService : IRefreshSessionService
    {
        private static readonly SemaphoreSlim[] InMemoryLocks = Enumerable.Range(0, 256).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
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
            var roles = await _userManager.GetRolesAsync(user);
            var privileged = roles.Any(AppRoles.AdministrativeRoles.Contains);
            var now = DateTime.UtcNow;
            var absolute = now.Add(SessionPolicy.Absolute(_configuration, privileged));
            var idle = now.Add(SessionPolicy.Idle(_configuration, privileged));
            var rawToken = GenerateRawRefreshToken();
            var session = new RefreshSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = absolute,
                FamilyCreatedAt = now, AbsoluteExpiresAt = absolute, IdleExpiresAt = idle < absolute ? idle : absolute,
                LastActivityAt = now, Management = metadata.Management, Privileged = privileged,
                SecurityStampHash = SessionPolicy.Digest(user.SecurityStamp ?? ""), RolesHash = SessionPolicy.Roles(roles),
                MfaVerifiedAt = metadata.SecondFactorAt,
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
                // A loser of an atomic rotation gets no credential. A bounded conflict
                // lets a browser observe its sibling's Set-Cookie without revoking it.
                if (session.ReplacedBySessionId.HasValue && session.RevocationReason == "rotated" && session.RevokedAt > DateTime.UtcNow.AddSeconds(-10))
                    throw new CommerceException("SESSION_REFRESH_CONFLICT", "Another session renewal is finishing. Please retry.", 409);
                await RevokeFamilyAsync(session.FamilyId, cancellationToken, "refresh-replay");
                return null;
            }

            if (!SessionPolicy.Active(session, DateTime.UtcNow) || session.User == null || session.Management != metadata.Management)
            {
                session.RevokedAt = DateTime.UtcNow;
                session.RevocationReason = session.AbsoluteExpiresAt == null ? "legacy-session" : session.AbsoluteExpiresAt <= DateTime.UtcNow ? "absolute-expired" : session.IdleExpiresAt <= DateTime.UtcNow ? "idle-expired" : "session-ineligible";
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

            var roles = await _userManager.GetRolesAsync(user);
            if (session.SecurityStampHash != SessionPolicy.Digest(user.SecurityStamp ?? "") || session.RolesHash != SessionPolicy.Roles(roles) || (session.Privileged && !user.TwoFactorEnabled))
            {
                await RevokeFamilyAsync(session.FamilyId, cancellationToken);
                return null;
            }
            var rawReplacement = GenerateRawRefreshToken();
            var replacement = new RefreshSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(rawReplacement),
                FamilyId = session.FamilyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = session.ExpiresAt,
                FamilyCreatedAt = session.FamilyCreatedAt, AbsoluteExpiresAt = session.AbsoluteExpiresAt,
                IdleExpiresAt = session.IdleExpiresAt, LastActivityAt = session.LastActivityAt,
                Management = session.Management, Privileged = session.Privileged,
                SecurityStampHash = session.SecurityStampHash, RolesHash = session.RolesHash, MfaVerifiedAt = session.MfaVerifiedAt,
                UserAgent = metadata.UserAgent,
                IpAddress = metadata.IpAddress,
                LastUsedAt = DateTime.UtcNow
            };
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "rotated";
            session.LastUsedAt = DateTime.UtcNow;
            _context.RefreshSessions.Add(replacement);
            await _context.SaveChangesAsync(cancellationToken);
            session.ReplacedBySessionId = replacement.Id;
            await _context.SaveChangesAsync(cancellationToken);
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
            if (session == null)
            {
                return;
            }

            await RevokeFamilyAsync(session.FamilyId, cancellationToken);
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

        private async Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken, string reason = "revoked")
        {
            var familySessions = await _context.RefreshSessions.Where(s => s.FamilyId == familyId && s.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var familySession in familySessions)
            {
                familySession.RevokedAt = DateTime.UtcNow;
                familySession.RevocationReason = reason;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // A database-owned per-user lock coordinates rotation, logout and replay across API instances.
        // The replacement insert and predecessor link commit atomically; no external I/O occurs here.
        private async Task<T> WithSessionLockAsync<T>(string userId, Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            if (!_context.Database.IsSqlServer())
            {
                // Test-only in-memory provider has no database-owned transactions.
                var gate = InMemoryLocks[(uint)StringComparer.Ordinal.GetHashCode(userId) % InMemoryLocks.Length];
                await gate.WaitAsync(cancellationToken);
                try { return await operation(); } finally { gate.Release(); }
            }
            // Provisioning and role changes may already own an atomic transaction.
            // Reuse it while retaining the same SQL-owned lock and ownership rules.
            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            var resource = "Amaara.RefreshSessions." + HashRefreshToken(userId);
            await _context.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51001, 'Session lock unavailable', 1;", cancellationToken);
            var result = await operation();
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return result;
        }

        public Task<bool> RecordActivityAsync(string userId, string familyId, CancellationToken cancellationToken = default) =>
            WithSessionLockAsync(userId, async () =>
            {
                var session = await _context.RefreshSessions.SingleOrDefaultAsync(s => s.UserId == userId && s.FamilyId == familyId && s.RevokedAt == null, cancellationToken);
                var now = DateTime.UtcNow;
                if (session == null || !SessionPolicy.Active(session, now)) return false;
                session.LastActivityAt = now;
                var idle = now.Add(SessionPolicy.Idle(_configuration, session.Privileged));
                session.IdleExpiresAt = idle < session.AbsoluteExpiresAt ? idle : session.AbsoluteExpiresAt;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);
    }
}
