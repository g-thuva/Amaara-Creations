namespace be.Application.Abstractions.Storage;
public sealed record MediaUpload(string FileName, string ContentType, long Length, Func<Stream> OpenReadStream)
{
    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await using var source = OpenReadStream();
        await source.CopyToAsync(target, cancellationToken);
    }
}
