using be.Application.Abstractions.Storage;

namespace be.Infrastructure.Storage;

public sealed record PreparedImage(byte[] Bytes, string OriginalName, int Width, int Height);
public static class ImageUploadProcessor
{
    public static async Task<PreparedImage> PrepareAsync(MediaUpload file, IConfiguration configuration, IUploadScanner? scanner, long maximum, bool avatar, CancellationToken ct)
    {
        if (file.Length <= 0) throw new InvalidOperationException("No file was uploaded.");
        if (file.Length > maximum) throw new InvalidOperationException("File size exceeds the configured limit.");
        var original = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(original) || original.Length > 255 || original.Any(char.IsControl)) throw new InvalidOperationException("Invalid file name.");
        var extension = Path.GetExtension(original).ToLowerInvariant();
        var bytes = await ImageUploadValidator.ReadBoundedAsync(file, maximum, ct);
        var image = ImageUploadValidator.Validate(bytes, file.ContentType, extension, avatar);
        var pixels = Math.Min(configuration.GetValue<int?>("Uploads:MaxImagePixels") ?? 24_000_000, 24_000_000);
        if (image.Width <= 0 || image.Height <= 0 || (long)image.Width * image.Height > pixels) throw new InvalidOperationException("Image dimensions are invalid or too large.");
        await UploadScanning.Check(scanner, configuration, bytes, ct);
        bytes = await SafeRasterImage.NormalizeAsync(bytes, pixels, maximum, ct);
        image = ImageUploadValidator.Validate(bytes, "image/png", ".png");
        return new(bytes, original, image.Width, image.Height);
    }
}
