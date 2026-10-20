using be.Application.Abstractions.Storage;
using be.Domain.Common;
using be.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace be.Tests;

public sealed class AvatarUploadSecurityTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amaara-avatar-tests-" + Guid.NewGuid().ToString("N"));
    private static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    private static byte[] Gif => Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");

    [Theory]
    [InlineData("script.png", "image/png")]
    [InlineData("script.jpg", "image/jpeg")]
    [InlineData("script.gif", "image/gif")]
    [InlineData("script.webp", "image/webp")]
    public async Task RejectsExecutableContentDisguisedAsAnImage(string name, string mime)
    {
        await Reject(System.Text.Encoding.UTF8.GetBytes("<script>alert('avatar')</script>"), name, mime);
    }

    [Fact]
    public async Task RejectsMismatchedTypeCorruptionAndTruncatedGif()
    {
        await Reject(Png, "avatar.jpg", "image/jpeg");
        var corrupt = Png; corrupt[^5] ^= 0xff;
        await Reject(corrupt, "avatar.png", "image/png");
        await Reject(Gif[..^1], "avatar.gif", "image/gif");
    }

    [Fact]
    public async Task EnforcesActualBytesAndDecodedCanvasDimensions()
    {
        var service = Service(maximumBytes: 40, maximumPixels: 1);
        await Assert.ThrowsAsync<CommerceException>(() => service.SaveAsync(new("avatar.png", "image/png", 1, () => new MemoryStream(Png)), "owner", default));
        var large = Gif; large[6] = 100; large[8] = 100;
        await Assert.ThrowsAsync<CommerceException>(() => service.SaveAsync(Upload(large, "avatar.gif", "image/gif"), "owner", default));
        AssertNoFiles();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StoresValidatedOriginalBytesUnderRandomizedNames(bool gif)
    {
        var bytes = gif ? Gif : Png;
        var service = Service();
        var result = await service.SaveAsync(Upload(bytes, gif ? "avatar.gif" : "avatar.png", gif ? "image/gif" : "image/png"), "owner", default);
        var second = await service.SaveAsync(Upload(bytes, gif ? "avatar.gif" : "avatar.png", gif ? "image/gif" : "image/png"), "owner", default);
        Assert.NotEqual(result.FileName, second.FileName);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(root, "wwwroot", "uploads", "avatars", result.FileName)));
    }

    private async Task Reject(byte[] data, string name, string mime)
    {
        await Assert.ThrowsAsync<CommerceException>(() => Service().SaveAsync(Upload(data, name, mime), "owner", default));
        AssertNoFiles();
    }
    private void AssertNoFiles() => Assert.Empty(Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories) : []);
    private static MediaUpload Upload(byte[] data, string name, string mime) => new(name, mime, data.Length, () => new MemoryStream(data));
    private LocalAvatarStorage Service(int maximumBytes = 4096, int maximumPixels = 24_000_000) => new(new Environment(root), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Uploads:MaxFileSizeBytes"] = maximumBytes.ToString(), ["Uploads:MaxImagePixels"] = maximumPixels.ToString()
    }).Build());
    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
    private sealed class Environment(string path) : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = path;
        public string WebRootPath { get; set; } = Path.Combine(path, "wwwroot");
        public string ApplicationName { get; set; } = "be.Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
