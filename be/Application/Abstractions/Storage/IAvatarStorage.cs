namespace be.Application.Abstractions.Storage;
public sealed record StoredAvatar(string FileUrl, string FileName);
public interface IAvatarStorage
{
    Task<StoredAvatar> SaveAsync(MediaUpload file, string userId, CancellationToken cancellationToken);
}
