using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;

namespace be.Application.Features.Audit;
public sealed class AuditQueries(IApplicationDbContext db)
{
    public async Task<object?> Audit(string? action, string? entityType, string? entityId, int page = 1, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking()
            .Where(a => (action == null || a.Action == action) && (entityType == null || a.EntityType == entityType) && (entityId == null || a.EntityId == entityId));
        return new
        {
            items = await query.OrderByDescending(a => a.Id)
            .Skip((Math.Max(1, page) - 1) * 50)
            .Take(50)
            .Select(a => new { a.Id, a.ActorUserId, a.Action, a.EntityType, a.EntityId, a.CreatedAt, a.CorrelationId, a.OldValuesJson, a.NewValuesJson })
            .ToListAsync(ct),
            totalCount = await query.CountAsync(ct)
        };
    }
}
