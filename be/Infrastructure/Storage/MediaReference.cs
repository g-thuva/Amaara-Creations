using System.Text.RegularExpressions;

namespace be.Infrastructure.Storage;

/// <summary>Durable provider/tier discriminator; bucket names never come from a caller.</summary>
public static class MediaReference
{
    public const string PublicRoute = "/api/v1/media/public/";
    public static string Public(string key) => "s3:public/" + key;
    public static string Private(string key) => "s3:private/" + key;
    public static (bool IsPrivate, string Key) Parse(string reference, StoragePrefixes prefixes)
    {
        var isPrivate = reference.StartsWith("s3:private/", StringComparison.Ordinal);
        var prefix = isPrivate ? "s3:private/" : "s3:public/";
        if (!reference.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid media storage reference.");
        var key = reference[prefix.Length..];
        var pattern = isPrivate
            ? $"(?:{Regex.Escape(prefixes.Artwork)}|{Regex.Escape(prefixes.Proofs)})/[1-9][0-9]*/[0-9]{{4}}/(?:0[1-9]|1[0-2])/[a-f0-9]{{32}}\\.png"
            : $"(?:{Regex.Escape(prefixes.Avatars)}/[a-f0-9]{{32}}\\.png|{Regex.Escape(prefixes.Products)}/[0-9]{{4}}/(?:0[1-9]|1[0-2])/[a-f0-9]{{32}}\\.png)";
        if (key.Length > 450 || !Regex.IsMatch(key, "\\A" + pattern + "\\z", RegexOptions.CultureInvariant)) throw new InvalidOperationException("Invalid media storage reference.");
        return (isPrivate, key);
    }
    public static string NewKey(string area, bool isPrivate, StoragePrefixes prefixes, string? identifier = null)
    {
        var id = identifier ?? Guid.NewGuid().ToString("N");
        string key;
        if (!isPrivate && area == "avatars") key = $"{prefixes.Avatars}/{id}.png";
        else if (!isPrivate && area is "products" or "catalog") key = $"{prefixes.Products}/{DateTime.UtcNow:yyyy/MM}/{id}.png";
        else
        {
            var match = Regex.Match(area, "\\A(custom-artwork|custom-proofs)/([1-9][0-9]*)\\z");
            if (!isPrivate || !match.Success) throw new InvalidOperationException("Unsupported media purpose.");
            key = $"{(match.Groups[1].Value == "custom-artwork" ? prefixes.Artwork : prefixes.Proofs)}/{match.Groups[2].Value}/{DateTime.UtcNow:yyyy/MM}/{id}.png";
        }
        Parse(isPrivate ? Private(key) : Public(key), prefixes);
        return key;
    }
}
