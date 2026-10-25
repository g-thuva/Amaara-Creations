using be.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace be.Tests;
public sealed class StorageOptionsTests
{
    [Fact]
    public void ValidatesTierSeparationCloudFrontAndPrefixesWithoutCredentialSettings()
    {
        var options = S3MediaStorageTests.Settings();
        var validator = new StorageOptionsValidator(new S3MediaStorageTests.Environment(), new ConfigurationBuilder().Build());
        Assert.True(validator.Validate(null, options).Succeeded);
        options.S3.PrivateBucket = options.S3.PublicBucket; Assert.True(validator.Validate(null, options).Failed);
        options = S3MediaStorageTests.Settings(); options.S3.Prefixes.Artwork = "../private"; Assert.True(validator.Validate(null, options).Failed);
        options = S3MediaStorageTests.Settings(); options.S3.PublicDelivery = "CloudFront"; options.S3.PublicBaseUrl = "http://cdn.example.test"; Assert.True(validator.Validate(null, options).Failed);
    }
    [Fact]
    public void ProductionRejectsApiProxyAndUnknownProvider()
    {
        var validator = new StorageOptionsValidator(new S3MediaStorageTests.Environment { EnvironmentName = "Production" }, new ConfigurationBuilder().Build());
        var options = S3MediaStorageTests.Settings(); Assert.True(validator.Validate(null, options).Failed);
        options.S3.PublicDelivery = "CloudFront"; options.S3.PublicBaseUrl = "https://cdn.example.test";
        options.S3.ReadinessPublicKey = MediaReference.Public(MediaReference.NewKey("avatars", false, options.S3.Prefixes));
        options.S3.ReadinessPrivateKey = MediaReference.Private(MediaReference.NewKey("custom-artwork/1", true, options.S3.Prefixes));
        Assert.True(validator.Validate(null, options).Succeeded);
        options.Provider = "S33"; Assert.True(validator.Validate(null, options).Failed);
    }
}
