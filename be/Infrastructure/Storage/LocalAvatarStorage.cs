using be.Application.Abstractions.Storage;
using be.Domain.Common;

namespace be.Infrastructure.Storage;
public sealed class LocalAvatarStorage(IWebHostEnvironment environment, IConfiguration configuration) : IAvatarStorage
{
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] ContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    public async Task<StoredAvatar> SaveAsync(MediaUpload file, string userId, CancellationToken cancellationToken)
    {
        var maximum = configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 5 * 1024 * 1024;
        if (file.Length == 0)
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "No file uploaded");
        if (file.Length > maximum)
            throw new CommerceException("AVATAR_UPLOAD_INVALID", $"File size exceeds maximum allowed size of {maximum / 1024 / 1024}MB");
        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (!Extensions.Contains(extension))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", $"Invalid file type. Allowed types: {string.Join(", ", Extensions)}");
        if (!ContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "Invalid image content type");
        var directory = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(directory);
        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var path = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "Invalid upload owner.");
        await using var target = new FileStream(path, FileMode.CreateNew);
        await file.CopyToAsync(target, cancellationToken);
        return new StoredAvatar($"/uploads/avatars/{fileName}", fileName);
    }
}
