namespace be.Domain.Catalog;

public sealed class MediaCleanupWork
{
    public long Id { get; set; }
    public string StorageKey { get; set; } = "";
    public string? PublicUrl { get; set; }
    public DateTime NotBeforeUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
}
