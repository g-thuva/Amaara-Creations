using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Payments.Datatrans;
public sealed class DatatransWebhookVerifier(IOptions<DatatransOptions> options)
{
    public bool Verify(ReadOnlySpan<byte> rawBody, string? header, DateTimeOffset now)
    {
        var config = options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(config.HmacKey) || config.SignatureToleranceSeconds is < 30 or > 900)
            return false;
        try
        {
            var parts = header.Split(',').Select(p => p.Trim().Split('=', 2)).ToArray();
            if (parts.Any(p => p.Length != 2) || parts.Count(p => p[0] == "t") != 1 || parts.Count(p => p[0] == "s0") != 1)
                return false;
            var timestamp = parts.Single(p => p[0] == "t")[1];
            if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
                return false;
            var date = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            if (Math.Abs((now - date).TotalSeconds) > config.SignatureToleranceSeconds)
                return false;
            var signature = Convert.FromHexString(parts.Single(p => p[0] == "s0")[1]);
            var key = Convert.FromHexString(config.HmacKey);
            if (key.Length < 32 || signature.Length != 32)
                return false;
            var prefix = Encoding.UTF8.GetBytes(timestamp);
            var signed = new byte[prefix.Length + rawBody.Length];
            prefix.CopyTo(signed, 0);
            rawBody.CopyTo(signed.AsSpan(prefix.Length));
            return CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, signed), signature);
        }
        catch (Exception exception)when (exception is FormatException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }
}
