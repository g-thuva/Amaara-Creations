using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;

namespace be.Tests;
public sealed class FilePasswordBlocklistTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AmaaraBlocklist-" + Guid.NewGuid().ToString("N"));
    [Fact]
    public void ChecksNormalizedWholePasswordsAndFailsClosedOnMissingOrCorruptedBuckets()
    {
        var password = "a public example cafe\u0301 passphrase";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormC)));
        var prefix = Convert.ToHexString(hash[..2]).ToLowerInvariant();
        Directory.CreateDirectory(Path.Combine(root, prefix[..2]));
        var file = Path.Combine(root, prefix[..2], prefix[2..] + ".bin");
        File.WriteAllBytes(file, hash);
        File.WriteAllText(Path.Combine(root, "manifest.json"), JsonSerializer.Serialize(new { format = 1, algorithm = "SHA256", normalization = "NFC", buckets = new Dictionary<string, object> { [prefix] = new { count = 1, sha256 = Convert.ToHexString(SHA256.HashData(hash)) } } }));
        var checker = new FilePasswordBlocklist(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:PasswordBlocklistPath"] = root }).Build());
        Assert.Equal(PasswordBlocklistResult.Blocked, checker.Check(password));
        Assert.Equal(PasswordBlocklistResult.Blocked, checker.Check(password.Normalize(NormalizationForm.FormC)));
        Assert.Equal(PasswordBlocklistResult.Clear, checker.Check("a completely different test passphrase"));
        File.WriteAllBytes(file, new byte[32]);
        Assert.Equal(PasswordBlocklistResult.Unavailable, checker.Check(password));
        File.Delete(file);
        Assert.Equal(PasswordBlocklistResult.Unavailable, checker.Check(password));
    }
    [Fact]
    public async Task RequiredUnavailableCorpusRejectsNewPasswordsWithoutChangingExistingHashes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:RequirePasswordBlocklist"] = "true" }).Build();
        var result = await new PassphraseValidator(configuration).ValidateAsync(null!, new be.Domain.Identity.User(), "Otherwise valid new password 8!");
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "PasswordCheckUnavailable");
    }
    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
