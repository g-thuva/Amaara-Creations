using be.Application.Abstractions.Storage;
using be.Domain.Common;

namespace be.Infrastructure.Storage;
public sealed class S3AvatarStorage(S3MediaStorageService storage) : IAvatarStorage
{
    public async Task<StoredAvatar> SaveAsync(MediaUpload file, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await storage.UploadAsync(file, "avatars", cancellationToken);
            return new(result.PublicUrl, result.StorageKey.Split('/')[^1]);
        }
        catch (InvalidOperationException ex) { throw new CommerceException("AVATAR_UPLOAD_INVALID", ex.Message); }
    }
}
