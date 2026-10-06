using System.Buffers.Binary;
using be.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace be.Tests;

public sealed class LocalMediaStorageServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"amaara-media-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task UploadAsyncStoresValidPngWithPortableMetadata()
    {
        var service = CreateService();
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var file = CreateFile(bytes, "sample.png", "image/png");

        var result = await service.UploadAsync(file, "products");

        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.StartsWith("uploads/products/", result.StorageKey);
        Assert.DoesNotContain(":\\", result.StorageKey);
        Assert.True(await service.ExistsAsync(result.StorageKey));
    }

    [Fact]
    public async Task UploadPrivateAsyncStoresOutsideWebRootWithoutPublicUrl()
    {
        var service = CreateService();
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var file = CreateFile(bytes, "artwork.png", "image/png");

        var result = await service.UploadPrivateAsync(file, "custom-artwork/42");

        Assert.StartsWith("private/custom-artwork-42/", result.StorageKey);
        Assert.Equal(string.Empty, result.PublicUrl);
        Assert.True(await service.ExistsAsync(result.StorageKey));
        Assert.False(File.Exists(Path.Combine(_contentRoot, "wwwroot", result.StorageKey.Replace('/', Path.DirectorySeparatorChar))));
        await using var stream = await service.OpenReadAsync(result.StorageKey);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task UploadAsyncRejectsMismatchedExtensionAndContentType()
    {
        var service = CreateService();
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var file = CreateFile(bytes, "sample.jpg", "image/jpeg");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(file, "products"));

        Assert.Contains("must match", exception.Message);
    }

    [Fact]
    public async Task UploadAsyncRejectsCorruptedPng()
    {
        var service = CreateService();
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        bytes[bytes.Length - 5] ^= 0xFF;
        var file = CreateFile(bytes, "corrupt.png", "image/png");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(file, "products"));

        Assert.Contains("corrupted", exception.Message);
    }

    [Fact]
    public async Task UploadAsyncRejectsExcessivePixelDimensions()
    {
        var service = CreateService(maxImagePixels: 100);
        var file = CreateFile(CreatePngHeader(20, 20), "large.png", "image/png");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(file, "products"));

        Assert.Contains("dimensions", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private LocalMediaStorageService CreateService(long maxFileSize = 5 * 1024 * 1024, int maxImagePixels = 24_000_000)
    {
        Directory.CreateDirectory(_contentRoot);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Uploads:MaxFileSizeBytes"] = maxFileSize.ToString(),
            ["Uploads:MaxImagePixels"] = maxImagePixels.ToString()
        }).Build();
        return new LocalMediaStorageService(new TestEnvironment(_contentRoot), configuration);
    }

    private static FormFile CreateFile(byte[] bytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        using var stream = new MemoryStream();
        stream.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(stream, "IHDR", header);
        WriteChunk(stream, "IEND", Array.Empty<byte>());
        return stream.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crcInput = typeBytes.Concat(data).ToArray();
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, ComputeCrc(crcInput));
        stream.Write(crc);
    }

    private static uint ComputeCrc(IEnumerable<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "be.Tests";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
