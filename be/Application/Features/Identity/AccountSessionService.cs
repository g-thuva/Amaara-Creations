using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Identity;

public sealed record AccountSession(string Id, string Device, DateTime RefreshedAt, DateTime ExpiresAt, bool Current);
public sealed class AccountSessionService(IApplicationDbContext db, IRefreshSessionService sessions, IRequestActor actor, IAuditService audit)
{
    private string UserId => actor.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new CommerceException("AUTHENTICATION_REQUIRED", "Please sign in.", 401);
    public async Task<IReadOnlyList<AccountSession>> List(CancellationToken ct)
    {
        var id = UserId;
        var family = actor.User.FindFirstValue("session_family");
        return await db.RefreshSessions.AsNoTracking().Where(s => s.UserId == id && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt).Take(50)
            .Select(s => new AccountSession(s.FamilyId, s.UserAgent ?? "Browser session", s.LastUsedAt ?? s.CreatedAt, s.ExpiresAt, s.FamilyId == family)).ToListAsync(ct);
    }
    public async Task Revoke(string familyId, CancellationToken ct)
    {
        if (familyId.Length != 32 || !familyId.All(Uri.IsHexDigit) || !await sessions.RevokeFamilyForUserAsync(UserId, familyId, ct))
            throw new CommerceException("SESSION_NOT_FOUND", "Session not found.", 404);
        await audit.RecordAsync("AccountSessionRevoked", "User", UserId, null, new { Revoked = true }, actor.Audit, ct);
    }
}
