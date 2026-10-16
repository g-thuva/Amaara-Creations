namespace be.Application.Abstractions.Audit;
public sealed record AuditActor(string? UserId, string? CorrelationId, string? IpAddress);
