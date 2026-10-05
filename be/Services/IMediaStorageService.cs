namespace be.Services
{
    public record StoredMedia(
        string StorageKey,
        string PublicUrl,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        int Width,
        int Height,
        string StorageProvider);

    public interface IMediaStorageService
    {
        Task<StoredMedia> UploadAsync(IFormFile file, string area, CancellationToken cancellationToken = default);
        Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
        string GetPublicUrl(string storageKey);
        Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
    }
}
