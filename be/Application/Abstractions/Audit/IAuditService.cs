namespace be.Application.Abstractions.Audit
{
    public interface IAuditService
    {
        Task RecordAsync(
        string action,
        string entityType,
        string? entityId,
        object? oldValues,
        object? newValues,
        AuditActor actor,
        CancellationToken cancellationToken = default);
    }
}
