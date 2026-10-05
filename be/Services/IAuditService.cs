namespace be.Services
{
    public interface IAuditService
    {
        Task RecordAsync(string action, string entityType, string? entityId, object? oldValues, object? newValues, HttpContext httpContext, CancellationToken cancellationToken = default);
    }
}
