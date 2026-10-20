using be.Application.Abstractions.Storage;

namespace be.Infrastructure.Storage
{
    public class LocalMediaStorageService : IMediaStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize;
        private readonly int _maxImagePixels;
        public LocalMediaStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _maxFileSize = configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 5 * 1024 * 1024;
            _maxImagePixels = configuration.GetValue<int?>("Uploads:MaxImagePixels") ?? 24_000_000;
        }

        public async Task<StoredMedia> UploadAsync(MediaUpload file, string area, CancellationToken cancellationToken = default)
        {
            return await UploadCoreAsync(file, area, false, cancellationToken);
        }

        public async Task<StoredMedia> UploadPrivateAsync(MediaUpload file, string area, CancellationToken cancellationToken = default)
        {
            return await UploadCoreAsync(file, area, true, cancellationToken);
        }

        private async Task<StoredMedia> UploadCoreAsync(MediaUpload file, string area, bool isPrivate, CancellationToken cancellationToken)
        {
            if (file.Length <= 0)
            {
                throw new InvalidOperationException("No file was uploaded.");
            }

            if (file.Length > _maxFileSize)
            {
                throw new InvalidOperationException($"File size exceeds the configured limit of {_maxFileSize / 1024 / 1024}MB.");
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var bytes = await ImageUploadValidator.ReadBoundedAsync(file, _maxFileSize, cancellationToken);
            var imageInfo = ImageUploadValidator.Validate(bytes, file.ContentType, extension);
            if (imageInfo.Width <= 0 || imageInfo.Height <= 0 || (long)imageInfo.Width * imageInfo.Height > _maxImagePixels)
            {
                throw new InvalidOperationException("Image dimensions are invalid or too large.");
            }

            var safeArea = string.Join('-', area.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(safeArea))
            {
                safeArea = "catalog";
            }

            var fileName = $"{Guid.NewGuid():N}{imageInfo.Extension}";
            var relativeStorageKey = isPrivate ? $"private/{safeArea}/{DateTime.UtcNow:yyyy/MM}/{fileName}" : $"uploads/{safeArea}/{DateTime.UtcNow:yyyy/MM}/{fileName}";
            var physicalPath = GetPhysicalPath(relativeStorageKey);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            await using (var target = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await target.WriteAsync(bytes, cancellationToken);
            }

            return new StoredMedia(relativeStorageKey, isPrivate ? string.Empty : GetPublicUrl(relativeStorageKey), originalFileName, imageInfo.ContentType, bytes.Length, imageInfo.Width, imageInfo.Height, "local");
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var path = GetPhysicalPath(storageKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.CompletedTask;
        }

        public string GetPublicUrl(string storageKey)
        {
            if (storageKey.Replace('\\', '/').StartsWith("private/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Private media does not have a public URL.");
            }

            return "/" + storageKey.Replace('\\', '/').TrimStart('/');
        }

        public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(GetPhysicalPath(storageKey)));
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Stream stream = new FileStream(GetPhysicalPath(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult(stream);
        }

        private string GetPhysicalPath(string storageKey)
        {
            var normalized = storageKey.Replace('\\', '/').TrimStart('/');
            if (normalized.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid storage key.");
            }

            var isPrivate = normalized.StartsWith("private/", StringComparison.OrdinalIgnoreCase);
            var relative = isPrivate ? normalized["private/".Length..] : normalized;
            var root = isPrivate ? Path.Combine(_environment.ContentRootPath, "App_Data", "private-media") : _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid storage key.");
            }

            return fullPath;
        }

    }
}
