namespace be.Application.Abstractions.Storage;
public sealed record PublicMediaContent(Stream? Content, string? RedirectUrl);
public interface IPublicMediaDelivery
{
    Task<PublicMediaContent?> ReadAsync(string key, CancellationToken ct);
}
