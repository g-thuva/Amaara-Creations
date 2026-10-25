using be.Application.Abstractions.Storage;
using be.Domain.Common;

namespace be.Infrastructure.Storage;
public sealed class LocalAvatarStorage(IWebHostEnvironment environment, IConfiguration configuration, IUploadScanner? scanner = null) : IAvatarStorage
{
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] ContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    public async Task<StoredAvatar> SaveAsync(MediaUpload file, string userId, CancellationToken cancellationToken)
    {
        var maximum = Math.Min(configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 5 * 1024 * 1024, StorageOptions.Read(configuration).Limits.AvatarBytes);
        if (file.Length == 0)
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "No file uploaded");
        if (file.Length > maximum)
            throw new CommerceException("AVATAR_UPLOAD_INVALID", $"File size exceeds maximum allowed size of {maximum / 1024 / 1024}MB");
        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (!Extensions.Contains(extension))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", $"Invalid file type. Allowed types: {string.Join(", ", Extensions)}");
        if (!ContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "Invalid image content type");
        byte[] bytes;
        try
        {
            bytes = await ImageUploadValidator.ReadBoundedAsync(file, maximum, cancellationToken);
            var image = ImageUploadValidator.Validate(bytes, file.ContentType, extension, allowGif: true);
            var maximumPixels = configuration.GetValue<int?>("Uploads:MaxImagePixels") ?? 24_000_000;
            if (image.Width <= 0 || image.Height <= 0 || (long)image.Width * image.Height > maximumPixels)
                throw new InvalidOperationException("Image dimensions are invalid or too large.");
            await UploadScanning.Check(scanner, configuration, bytes, cancellationToken);
            bytes = await SafeRasterImage.NormalizeAsync(bytes, maximumPixels, maximum, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new CommerceException("AVATAR_UPLOAD_INVALID", exception.Message);
        }
        var directory = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "avatars");
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}.png";
        var path = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new CommerceException("AVATAR_UPLOAD_INVALID", "Invalid upload owner.");
        await using var target = new FileStream(path, FileMode.CreateNew);
        await target.WriteAsync(bytes, cancellationToken);
        return new StoredAvatar($"/uploads/avatars/{fileName}", fileName);
    }
}
