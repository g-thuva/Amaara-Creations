using System.Collections.Concurrent;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using be.Application.Abstractions.Storage;
using be.Domain.Common;
using be.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using be.Api.Controllers;

namespace be.Tests;

public sealed class S3MediaStorageTests
{
    internal static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    internal static MediaUpload File(byte[]? bytes = null, string name = "image.png", string mime = "image/png") => new(name, mime, (bytes ?? Png).Length, () => new MemoryStream(bytes ?? Png));
    internal static StorageOptions Settings() => new() { Provider = "S3", S3 = new() { PublicBucket = "amaara-test-public", PrivateBucket = "amaara-test-private" } };
    internal static S3MediaStorageService Storage(FakeS3 client, Reservation? journal = null, StorageOptions? settings = null, IUploadScanner? scanner = null)
    {
        var config = new ConfigurationBuilder().Build();
        return new(client, Options.Create(settings ?? Settings()), config, scanner ?? new Scanner(), journal ?? new(), new LocalMediaStorageService(new Environment(), config));
    }
    [Fact]
    public async Task AvatarUsesRandomPiiFreeKeyAndStablePublicUrl()
    {
        using var s3 = new FakeS3(); var journal = new Reservation();
        var avatar = await new S3AvatarStorage(Storage(s3, journal)).SaveAsync(File(), "private-user-id@example.test", default);
        Assert.StartsWith(MediaReference.PublicRoute + "avatars/", avatar.FileUrl);
        Assert.DoesNotContain("private-user", avatar.FileUrl);
        Assert.Single(journal.Keys);
        var stored = Assert.Single(s3.Objects);
        Assert.Equal("amaara-test-public", stored.Key.Bucket);
        using var image = SkiaSharp.SKBitmap.Decode(stored.Value); Assert.NotNull(image);
        Assert.Equal("image/png", s3.LastPut!.ContentType);
        Assert.Equal(ServerSideEncryptionMethod.AES256, s3.LastPut.ServerSideEncryptionMethod);
        Assert.Equal("*", s3.LastPut.IfNoneMatch);
        Assert.Equal("public, max-age=31536000, immutable", s3.LastPut.Headers.CacheControl);
        Assert.NotNull(s3.LastPut.ChecksumSHA256);
    }
    [Theory]
    [InlineData("custom-artwork/42")]
    [InlineData("custom-proofs/42")]
    public async Task PrivateUploadStaysPrivateAndStreamsWithOwnedResponseLifetime(string area)
    {
        using var s3 = new FakeS3(); var service = Storage(s3);
        var saved = await service.UploadPrivateAsync(File(), area);
        Assert.StartsWith("s3:private/" + area, saved.StorageKey); Assert.Empty(saved.PublicUrl);
        Assert.Equal("private, no-store", s3.LastPut!.Headers.CacheControl);
        Assert.Throws<InvalidOperationException>(() => service.GetPublicUrl(saved.StorageKey));
        Assert.True(await service.ExistsAsync(saved.StorageKey));
        await using (var stream = await service.OpenReadAsync(saved.StorageKey)) { Assert.False(stream.CanSeek); Assert.True(stream.ReadByte() >= 0); }
        Assert.True(s3.LastRead!.Disposed);
        await service.DeleteAsync(saved.StorageKey); await service.DeleteAsync(saved.StorageKey);
        Assert.False(await service.ExistsAsync(saved.StorageKey));
    }
    [Fact]
    public async Task RetriedImportVerifiesExistingBytesRatherThanOverwriting()
    {
        using var s3 = new FakeS3(); var service = Storage(s3);
        var key = MediaReference.NewKey("products", false, Settings().S3.Prefixes);
        var first = await service.ImportAsync(File(), "products", false, key, default);
        var second = await service.ImportAsync(File(), "products", false, key, default);
        Assert.Equal(first, second); Assert.Single(s3.Objects);
        s3.Objects[("amaara-test-public", key)] = [1, 2, 3];
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ImportAsync(File(), "products", false, key, default));
    }
    [Fact]
    public async Task ConcurrentUploadsUseDifferentImmutableObjects()
    {
        using var s3 = new FakeS3(); var service = Storage(s3);
        var saved = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => service.UploadAsync(File(), "products")));
        Assert.Equal(12, saved.Select(item => item.StorageKey).Distinct().Count()); Assert.Equal(12, s3.Objects.Count);
    }
    [Theory]
    [InlineData("s3:public/../private/anything.png")]
    [InlineData("s3:public/custom-artwork/1/2026/10/11111111111111111111111111111111.png")]
    [InlineData("s3:private/avatars/11111111111111111111111111111111.png")]
    [InlineData("s3://other-bucket/secret.png")]
    [InlineData("s3:public/products/2026/10/%2e%2e.png")]
    public async Task ArbitraryReferencesNeverReachTheS3Client(string reference)
    {
        using var s3 = new FakeS3();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Storage(s3).OpenReadAsync(reference)); Assert.Equal(0, s3.Reads);
    }
    [Fact]
    public async Task MalformedUnsupportedOversizeAndUnscannedFilesNeverReachS3()
    {
        using var s3 = new FakeS3(); var queue = new Reservation(); var storage = Storage(s3, queue);
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.UploadAsync(File([1, 2, 3]), "products"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.UploadAsync(File(Png, "active.svg", "image/svg+xml"), "products"));
        var settings = Settings(); settings.Limits.ArtworkBytes = 40;
        var small = Storage(s3, queue, settings);
        await Assert.ThrowsAsync<InvalidOperationException>(() => small.UploadPrivateAsync(new("large.png", "image/png", 1, () => new MemoryStream(Png)), "custom-artwork/1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Storage(s3, queue, scanner: new Scanner(true)).UploadAsync(File(), "products"));
        Assert.Empty(s3.Objects); Assert.Empty(queue.Keys);
    }
    [Fact]
    public async Task MissingAndNetworkFailureRemainDistinctAndDoNotFallBackToDisk()
    {
        using var s3 = new FakeS3(); var storage = Storage(s3);
        var key = MediaReference.Public(MediaReference.NewKey("products", false, Settings().S3.Prefixes));
        Assert.Equal(404, (await Assert.ThrowsAsync<CommerceException>(() => storage.OpenReadAsync(key))).Status);
        s3.Failure = true;
        var failure = await Assert.ThrowsAsync<CommerceException>(() => storage.UploadAsync(File(), "products"));
        Assert.Equal(503, failure.Status); Assert.DoesNotContain("secret-provider-detail", failure.Message);
        Assert.Equal(503, (await Assert.ThrowsAsync<CommerceException>(() => storage.OpenReadAsync("uploads/legacy.png"))).Status);
    }
    [Fact]
    public async Task PublicControllerOnlyProxiesPublicTierAndCloudFrontUsesTheConfiguredOrigin()
    {
        using var s3 = new FakeS3(); var settings = Settings(); var storage = Storage(s3);
        var saved = await storage.UploadAsync(File(), "products"); var key = saved.StorageKey["s3:public/".Length..];
        var controller = new PublicMediaController(new PublicMediaDelivery(storage, Options.Create(settings))) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<FileStreamResult>(await controller.Get(key, default));
        Assert.IsType<NotFoundResult>(await controller.Get("../custom-artwork/1/file.png", default));
        settings.S3.PublicDelivery = "CloudFront"; settings.S3.PublicBaseUrl = "https://cdn.example.test";
        Assert.Equal("https://cdn.example.test/" + key, Assert.IsType<RedirectResult>(await controller.Get(key, default)).Url);
    }

    internal sealed class Reservation : IMediaCleanupQueue
    {
        public ConcurrentBag<string> Keys { get; } = [];
        public Task ReserveAsync(string reference, string? url, CancellationToken ct) { Keys.Add(reference); return Task.CompletedTask; }
        public void EnqueueDelete(string reference) => Keys.Add(reference);
    }
    internal sealed class Scanner(bool reject = false) : IUploadScanner
    {
        public Task ScanAsync(byte[] image, CancellationToken ct) => reject ? Task.FromException(new InvalidOperationException("Rejected test file")) : Task.CompletedTask;
    }
    internal sealed class Environment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "amaara-s3-unit-no-writes");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
    internal sealed class TrackingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    internal sealed class FakeS3() : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { RegionEndpoint = RegionEndpoint.APSouth1 })
    {
        public ConcurrentDictionary<(string Bucket, string Key), byte[]> Objects { get; } = new();
        public PutObjectRequest? LastPut { get; private set; }
        public TrackingStream? LastRead { get; private set; }
        public int Reads { get; private set; }
        public bool Failure { get; set; }
        public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken ct = default)
        {
            if (Failure) throw new AmazonS3Exception("secret-provider-detail") { StatusCode = HttpStatusCode.ServiceUnavailable };
            LastPut = request;
            using var output = new MemoryStream(); await request.InputStream.CopyToAsync(output, ct);
            if (!Objects.TryAdd((request.BucketName, request.Key), output.ToArray())) throw new AmazonS3Exception("Exists") { StatusCode = HttpStatusCode.PreconditionFailed };
            return new();
        }
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken ct = default)
        {
            Reads++;
            if (!Objects.TryGetValue((request.BucketName, request.Key), out var bytes)) throw new AmazonS3Exception("Missing") { StatusCode = HttpStatusCode.NotFound };
            LastRead = new TrackingStream(bytes);
            return Task.FromResult(new GetObjectResponse { ResponseStream = LastRead, ContentLength = bytes.Length });
        }
        public override Task<GetObjectMetadataResponse> GetObjectMetadataAsync(GetObjectMetadataRequest request, CancellationToken ct = default) => Objects.TryGetValue((request.BucketName, request.Key), out var bytes) ? Task.FromResult(new GetObjectMetadataResponse { ContentLength = bytes.Length }) : throw new AmazonS3Exception("Missing") { StatusCode = HttpStatusCode.NotFound };
        public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken ct = default) { Objects.TryRemove((request.BucketName, request.Key), out _); return Task.FromResult(new DeleteObjectResponse()); }
    }
}
