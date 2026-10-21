using be.Application.Abstractions.Storage;
using be.Infrastructure.Storage;
using System.Buffers.Binary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace be.Tests;
public sealed class LocalMediaStorageServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"amaara-media-tests-{Guid.NewGuid():N}");
    [Fact]
    public async Task PublicMediaUsesTheConfiguredWebRootWhilePrivateArtworkStaysSeparate()
    {
        Directory.CreateDirectory(_contentRoot);
        var publicRoot = Path.Combine(_contentRoot, "configured-public");
        var environment = new TestEnvironment(_contentRoot) { WebRootPath = publicRoot };
        var service = new LocalMediaStorageService(environment, new ConfigurationBuilder().Build());
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var visible = await service.UploadAsync(CreateFile(bytes, "public.png", "image/png"), "catalog");
        var hidden = await service.UploadPrivateAsync(CreateFile(bytes, "private.png", "image/png"), "artwork");
        Assert.True(File.Exists(Path.Combine(publicRoot, visible.StorageKey)));
        Assert.False(File.Exists(Path.Combine(publicRoot, hidden.StorageKey)));
        Assert.True(await service.ExistsAsync(hidden.StorageKey));
    }

    [Fact]
    public async Task UploadAsyncBoundsActualStreamBytesBeforeWritingAnyMedia()
    {
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var file = new MediaUpload("oversize.png", "image/png", 1, () => new MemoryStream(bytes));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(maxFileSize: 40).UploadAsync(file, "products"));
        Assert.Empty(Directory.GetFiles(_contentRoot, "*", SearchOption.AllDirectories));
    }

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

    [Fact]
    public async Task RejectsStructurallyValidPngWithoutDecodablePixelData()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(CreateFile(CreatePngHeader(2, 2), "incomplete.png", "image/png"), "products"));
        Assert.Empty(Directory.GetFiles(_contentRoot, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task RemovesAncillaryMetadataAndPreservesPixels()
    {
        var source = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        using var decorated = new MemoryStream();
        decorated.Write(source[..33]);
        WriteChunk(decorated, "tEXt", System.Text.Encoding.ASCII.GetBytes("Comment\0private-location-and-script-marker"));
        decorated.Write(source[33..]);
        var service = CreateService();
        var stored = await service.UploadPrivateAsync(CreateFile(decorated.ToArray(), "metadata.png", "image/png"), "art");
        await using var stream = await service.OpenReadAsync(stored.StorageKey);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        Assert.DoesNotContain("private-location", System.Text.Encoding.ASCII.GetString(output.ToArray()));
        using var before = SkiaSharp.SKBitmap.Decode(source);
        using var after = SkiaSharp.SKBitmap.Decode(output.ToArray());
        Assert.Equal(before.GetPixel(0, 0), after.GetPixel(0, 0));
        Assert.Equal(output.Length, stored.FileSize);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(8, true)]
    public async Task AppliesExifOrientationBeforeStrippingMetadata(int orientation, bool exchangesAxes)
    {
        using var bitmap = new SkiaSharp.SKBitmap(20, 30);
        using (var canvas = new SkiaSharp.SKCanvas(bitmap)) canvas.Clear(SkiaSharp.SKColors.Red);
        using var encoded = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 95);
        var jpeg = encoded.ToArray();
        // An APP1 EXIF block with one little-endian Orientation entry.
        byte[] exif = [0xff, 0xe1, 0, 34, 69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0, 1, 0, 18, 1, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        var input = jpeg[..2].Concat(exif).Concat(jpeg[2..]).ToArray();
        var stored = await CreateService().UploadAsync(CreateFile(input, "camera.jpg", "image/jpeg"), "products");
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal(exchangesAxes ? 30 : 20, stored.Width);
        Assert.Equal(exchangesAxes ? 20 : 30, stored.Height);
        await using var stream = await CreateService().OpenReadAsync(stored.StorageKey);
        using var decoded = SkiaSharp.SKBitmap.Decode(stream);
        var pixel = decoded.GetPixel(decoded.Width / 2, decoded.Height / 2);
        Assert.True(pixel.Red > 240 && pixel.Green < 10 && pixel.Blue < 10);
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
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:MaxFileSizeBytes"] = maxFileSize.ToString(), ["Uploads:MaxImagePixels"] = maxImagePixels.ToString() }).Build();
        return new LocalMediaStorageService(new TestEnvironment(_contentRoot), configuration);
    }

    private static MediaUpload CreateFile(byte[] bytes, string fileName, string contentType) => new(fileName, contentType, bytes.Length, () => new MemoryStream(bytes, writable: false));
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
