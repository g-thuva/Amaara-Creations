using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Payments.Zahls;
public sealed class ZahlsWebhookVerifier(IOptions<ZahlsOptions> options)
{
    private static readonly Regex SignaturePattern = new("^[0-9a-f]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public bool Verify(ReadOnlySpan<byte> rawBody, string? signatureHeader)
    {
        var config = options.Value;
        if (!config.Enabled || config.Mode != "TEST" || string.IsNullOrWhiteSpace(config.WebhookSigningKey) || signatureHeader is null || !SignaturePattern.IsMatch(signatureHeader))
            return false;
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(config.WebhookSigningKey), rawBody);
        var actual = Convert.FromHexString(signatureHeader);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
