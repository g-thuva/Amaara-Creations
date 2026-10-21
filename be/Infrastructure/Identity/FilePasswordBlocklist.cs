using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace be.Infrastructure.Identity;
public enum PasswordBlocklistResult { Clear, Blocked, Unavailable }

// A versioned, private offline corpus. Only a two-byte SHA-256 bucket is read;
// passwords and their hashes are never sent to an external service.
public sealed class FilePasswordBlocklist(IConfiguration configuration)
{
    public PasswordBlocklistResult Check(string password)
    {
        var directory = configuration["Authentication:PasswordBlocklistPath"];
        if (string.IsNullOrWhiteSpace(directory)) return configuration.GetValue<bool>("Authentication:RequirePasswordBlocklist") ? PasswordBlocklistResult.Unavailable : PasswordBlocklistResult.Clear;
        try
        {
            var root = Path.GetFullPath(directory);
            var manifestPath = Path.Combine(root, "manifest.json");
            if (new FileInfo(manifestPath).Length > 16_000_000) return PasswordBlocklistResult.Unavailable;
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (manifest.RootElement.GetProperty("format").GetInt32() != 1 || manifest.RootElement.GetProperty("normalization").GetString() != "NFC" || manifest.RootElement.GetProperty("algorithm").GetString() != "SHA256") return PasswordBlocklistResult.Unavailable;
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormC)));
            var prefix = Convert.ToHexString(hash.AsSpan(0, 2)).ToLowerInvariant();
            var buckets = manifest.RootElement.GetProperty("buckets");
            if (!buckets.EnumerateObject().Any()) return PasswordBlocklistResult.Unavailable;
            if (!buckets.TryGetProperty(prefix, out var bucket)) return PasswordBlocklistResult.Clear;
            var count = bucket.GetProperty("count").GetInt32();
            if (count is < 1 or > 1_000_000) return PasswordBlocklistResult.Unavailable;
            using var stream = new FileStream(Path.Combine(root, prefix[..2], prefix[2..] + ".bin"), FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length != (long)count * 32 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(stream), Convert.FromHexString(bucket.GetProperty("sha256").GetString()!))) return PasswordBlocklistResult.Unavailable;
            Span<byte> candidate = stackalloc byte[32];
            var low = 0; var high = count - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                stream.Position = middle * 32L; stream.ReadExactly(candidate);
                var comparison = candidate.SequenceCompareTo(hash);
                if (comparison == 0) return PasswordBlocklistResult.Blocked;
                if (comparison < 0) low = middle + 1; else high = middle - 1;
            }
            return PasswordBlocklistResult.Clear;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException) { return PasswordBlocklistResult.Unavailable; }
    }
}
