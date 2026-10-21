using System.Security.Cryptography;

namespace be.Tests;

// Disposable test-only credentials; not referenced by the application assembly.
public static class TotpFixture
{
    public const string Secret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";
    public static string Code
    {
        get
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var bits = 0; var value = 0; var bytes = new List<byte>();
            foreach (var character in Secret)
            {
                value = (value << 5) | alphabet.IndexOf(character);
                bits += 5;
                if (bits >= 8) { bits -= 8; bytes.Add((byte)(value >> bits)); }
            }
            var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            if (BitConverter.IsLittleEndian) Array.Reverse(counter);
            var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
            var offset = hash[^1] & 15;
            var number = ((hash[offset] & 127) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            return (number % 1000000).ToString("D6");
        }
    }
}
