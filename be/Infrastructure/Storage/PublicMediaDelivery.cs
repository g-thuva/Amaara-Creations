using be.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Storage;
public sealed class PublicMediaDelivery(IMediaStorageService storage, IOptions<StorageOptions> options) : IPublicMediaDelivery
{
    public async Task<PublicMediaContent?> ReadAsync(string key, CancellationToken ct)
    {
        if (options.Value.Provider != "S3") return null;
        try { MediaReference.Parse(MediaReference.Public(key), options.Value.S3.Prefixes); }
        catch (InvalidOperationException) { return null; }
        if (options.Value.S3.PublicDelivery == "CloudFront") return new(null, options.Value.S3.PublicBaseUrl.TrimEnd('/') + "/" + key);
        return new(await storage.OpenReadAsync(MediaReference.Public(key), ct), null);
    }
}
