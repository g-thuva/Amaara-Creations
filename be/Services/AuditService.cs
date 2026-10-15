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
                OldValuesJson = SafeJson(oldValues),
                NewValuesJson = SafeJson(newValues),
                CorrelationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId) ? correlationId?.ToString() : null,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public static string? SafeJson(object? value)
        {
            if (value == null) return null;
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
                    if (new[] { "password", "secret", "token", "apikey", "signingkey", "connectionstring", "cardnumber", "cvv", "securitystamp" }.Any(normalized.Contains)) obj[key] = "[redacted]";
                    else Redact(obj[key]);
                }
            else if (node is System.Text.Json.Nodes.JsonArray array)
                foreach (var child in array) Redact(child);
        }
    }
}
