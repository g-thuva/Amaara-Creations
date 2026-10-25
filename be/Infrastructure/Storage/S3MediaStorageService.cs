using System.Net;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using be.Application.Abstractions.Storage;
using be.Domain.Common;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Storage;

public sealed class S3MediaStorageService(IAmazonS3 client, IOptions<StorageOptions> options, IConfiguration configuration,
    IUploadScanner scanner, IMediaCleanupQueue cleanup, LocalMediaStorageService local) : IMediaStorageService
{
    private StorageOptions Settings => options.Value;
    public Task<StoredMedia> UploadAsync(MediaUpload file, string area, CancellationToken cancellationToken = default) => Upload(file, area, false, null, cancellationToken);
    public Task<StoredMedia> UploadPrivateAsync(MediaUpload file, string area, CancellationToken cancellationToken = default) => Upload(file, area, true, null, cancellationToken);

    // CLI-only import entry; still passes the same decoder/scanner and key validation.
    public Task<StoredMedia> ImportAsync(MediaUpload file, string area, bool isPrivate, string deterministicKey, CancellationToken ct) => Upload(file, area, isPrivate, deterministicKey, ct);
    private async Task<StoredMedia> Upload(MediaUpload file, string area, bool isPrivate, string? importKey, CancellationToken ct)
    {
        var maximum = area == "avatars" ? Settings.Limits.AvatarBytes : !isPrivate ? Settings.Limits.ProductBytes : area.StartsWith("custom-proofs/", StringComparison.Ordinal) ? Settings.Limits.ProofBytes : Settings.Limits.ArtworkBytes;
        var prepared = await ImageUploadProcessor.PrepareAsync(file, configuration, scanner, maximum, area == "avatars", ct);
        var key = importKey ?? MediaReference.NewKey(area, isPrivate, Settings.S3.Prefixes);
        var reference = isPrivate ? MediaReference.Private(key) : MediaReference.Public(key);
        MediaReference.Parse(reference, Settings.S3.Prefixes);
        var publicUrl = isPrivate ? "" : GetPublicUrl(reference);
        // A durable reservation survives process termination or an ambiguous network result.
        await cleanup.ReserveAsync(reference, isPrivate ? null : publicUrl, ct);
        using var stream = new MemoryStream(prepared.Bytes, writable: false);
        var request = new PutObjectRequest
        {
            BucketName = isPrivate ? Settings.S3.PrivateBucket : Settings.S3.PublicBucket,
            Key = key, InputStream = stream, AutoCloseStream = false,
            ContentType = "image/png", ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            ChecksumSHA256 = Convert.ToBase64String(SHA256.HashData(prepared.Bytes)),
            IfNoneMatch = "*",
        };
        request.Headers.CacheControl = isPrivate ? "private, no-store" : "public, max-age=31536000, immutable";
        request.Headers.ContentDisposition = isPrivate ? "attachment; filename=\"image.png\"" : "inline";
        request.Metadata["sha256"] = Convert.ToHexString(SHA256.HashData(prepared.Bytes)).ToLowerInvariant();
        try { await client.PutObjectAsync(request, ct); }
        catch (AmazonS3Exception ex) when (importKey != null && ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            // Resume only if the already-present object is byte-for-byte identical.
            await VerifyAsync(reference, prepared.Bytes.LongLength, request.Metadata["sha256"], ct);
        }
        catch (Exception ex) when (IsUnavailable(ex, ct)) { throw Unavailable(); }
        return new StoredMedia(reference, publicUrl, prepared.OriginalName, "image/png", prepared.Bytes.LongLength, prepared.Width, prepared.Height, "s3", request.Metadata["sha256"]);
    }

    public string GetPublicUrl(string storageKey)
    {
        var (isPrivate, key) = MediaReference.Parse(storageKey, Settings.S3.Prefixes);
        if (isPrivate) throw new InvalidOperationException("Private media does not have a public URL.");
        // SQL stores a deployment-independent URL; the endpoint selects proxy/CDN at read time.
        return MediaReference.PublicRoute + key;
    }
    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (Legacy(storageKey)) { await local.DeleteAsync(storageKey, cancellationToken); return; }
        var (isPrivate, key) = MediaReference.Parse(storageKey, Settings.S3.Prefixes);
        try { await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = Bucket(isPrivate), Key = key }, cancellationToken); }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken)) { throw Unavailable(); }
    }
    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (Legacy(storageKey)) return await local.ExistsAsync(storageKey, cancellationToken);
        var (isPrivate, key) = MediaReference.Parse(storageKey, Settings.S3.Prefixes);
        try { await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = Bucket(isPrivate), Key = key }, cancellationToken); return true; }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return false; }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken)) { throw Unavailable(); }
    }
    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (Legacy(storageKey)) return await local.OpenReadAsync(storageKey, cancellationToken);
        var (isPrivate, key) = MediaReference.Parse(storageKey, Settings.S3.Prefixes);
        try
        {
            var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = Bucket(isPrivate), Key = key }, cancellationToken);
            return new S3ResponseStream(response);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) { throw new CommerceException("MEDIA_NOT_FOUND", "Media not found.", 404); }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken)) { throw Unavailable(); }
    }
    public async Task VerifyAsync(string reference, long length, string sha256, CancellationToken ct)
    {
        // GET and hash actual remote bytes, rather than trusting a multipart ETag or metadata alone.
        await using var stream = await OpenReadAsync(reference, ct);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920]; long read = 0; int count;
        while ((count = await stream.ReadAsync(buffer, ct)) > 0)
        {
            read += count;
            if (read > length) throw new InvalidOperationException("Remote media integrity check failed.");
            hash.AppendData(buffer, 0, count);
        }
        if (read != length || !Convert.ToHexString(hash.GetHashAndReset()).Equals(sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Remote media integrity check failed.");
    }
    private bool Legacy(string reference)
    {
        if (reference.StartsWith("s3:", StringComparison.Ordinal)) return false;
        if (!Settings.LegacyLocalReadsEnabled) throw new CommerceException("MEDIA_LEGACY_DISABLED", "This media has not been migrated.", 503);
        return true;
    }
    private string Bucket(bool isPrivate) => isPrivate ? Settings.S3.PrivateBucket : Settings.S3.PublicBucket;
    private static bool IsUnavailable(Exception ex, CancellationToken ct) => !ct.IsCancellationRequested && ex is Amazon.Runtime.AmazonServiceException or Amazon.Runtime.AmazonClientException or HttpRequestException or IOException or OperationCanceledException;
    private static CommerceException Unavailable() => new("MEDIA_STORAGE_UNAVAILABLE", "Media storage is unavailable. Try again later.", 503);
}

internal sealed class S3ResponseStream(GetObjectResponse response) : Stream
{
    private Stream Inner => response.ResponseStream;
    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => response.ContentLength;
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => Inner.ReadAsync(buffer, ct);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => Inner.ReadAsync(buffer, offset, count, ct);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) response.Dispose(); base.Dispose(disposing); }
    public override ValueTask DisposeAsync() { response.Dispose(); GC.SuppressFinalize(this); return ValueTask.CompletedTask; }
}
