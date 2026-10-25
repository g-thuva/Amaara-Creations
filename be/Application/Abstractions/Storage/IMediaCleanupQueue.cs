namespace be.Application.Abstractions.Storage;

/// <summary>Durable work items, independent of SQL media catalogue records.</summary>
public interface IMediaCleanupQueue
{
    Task ReserveAsync(string reference, string? publicUrl, CancellationToken ct);
    // Add inside the caller's SQL unit of work before saving the deletion.
    void EnqueueDelete(string reference);
}
