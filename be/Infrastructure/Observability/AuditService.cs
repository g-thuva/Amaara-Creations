using be.Application.Abstractions.Audit;
using be.Domain.Audit;
using be.Infrastructure.Persistence;
using System.Text.Json;

namespace be.Infrastructure.Observability
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordAsync(
        string action,
        string entityType,
        string? entityId,
        object? oldValues,
        object? newValues,
        AuditActor actor,
        CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                ActorUserId = actor.UserId,
                OldValuesJson = SafeJson(oldValues),
                NewValuesJson = SafeJson(newValues),
                CorrelationId = actor.CorrelationId,
                IpAddress = actor.IpAddress,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public static string? SafeJson(object? value)
        {
            if (value == null)
                return null;
            var node = JsonSerializer.SerializeToNode(value);
            Redact(node);
            return node?.ToJsonString();
        }

        private static void Redact(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is System.Text.Json.Nodes.JsonObject obj)
                foreach (var key in obj.Select(p => p.Key).ToArray())
                {
                    var normalized = key.Replace("_", "").ToLowerInvariant();
                    if (new[]
                    {
                        "password",
                        "secret",
                        "token",
                        "apikey",
                        "signingkey",
                        "connectionstring",
                        "cardnumber",
                        "cvv",
                        "securitystamp"
                    }.Any(normalized.Contains))
                        obj[key] = "[redacted]";
                    else
                        Redact(obj[key]);
                }
            else if (node is System.Text.Json.Nodes.JsonArray array)
                foreach (var child in array)
                    Redact(child);
        }
    }
}
