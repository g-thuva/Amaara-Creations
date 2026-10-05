using System.Security.Claims;
using System.Text.Json;
using be.Data;
using be.Infrastructure;
using be.Models;

namespace be.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordAsync(string action, string entityType, string? entityId, object? oldValues, object? newValues, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                ActorUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                OldValuesJson = oldValues == null ? null : JsonSerializer.Serialize(oldValues),
                NewValuesJson = newValues == null ? null : JsonSerializer.Serialize(newValues),
                CorrelationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId) ? correlationId?.ToString() : null,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
