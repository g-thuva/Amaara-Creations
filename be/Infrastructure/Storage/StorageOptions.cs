using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Storage;

public sealed class StorageOptions
{
    public string Provider { get; set; } = "Local";
    public bool LegacyLocalReadsEnabled { get; set; }
    public bool AllowLocalInProduction { get; set; }
    public string[] LegacyPublicOrigins { get; set; } = [];
    public int OrphanGraceHours { get; set; } = 168;
    public S3StorageOptions S3 { get; set; } = new();
    public UploadCategoryLimits Limits { get; set; } = new();
    public static StorageOptions Read(IConfiguration configuration)
    {
        var options = configuration.GetSection("Storage").Get<StorageOptions>() ?? new();
        options.Provider = configuration["Storage:Provider"] ?? configuration["Uploads:Provider"] ?? "Local";
        return options;
    }
}
public sealed class S3StorageOptions
{
    public string Region { get; set; } = "ap-south-1";
    public string PublicBucket { get; set; } = "";
    public string PrivateBucket { get; set; } = "";
    public string PublicDelivery { get; set; } = "ApiProxy";
    public string PublicBaseUrl { get; set; } = "";
    public string ReadinessPublicKey { get; set; } = "";
    public string ReadinessPrivateKey { get; set; } = "";
    // Reserved for future authorized signed downloads; current reads always stream.
    public int PrivateDownloadExpirySeconds { get; set; } = 60;
    public StoragePrefixes Prefixes { get; set; } = new();
}
public sealed class StoragePrefixes
{
    public string Avatars { get; set; } = "avatars";
    public string Products { get; set; } = "products";
    public string Artwork { get; set; } = "custom-artwork";
    public string Proofs { get; set; } = "custom-proofs";
}
public sealed class UploadCategoryLimits
{
    public long AvatarBytes { get; set; } = 5_242_880;
    public long ProductBytes { get; set; } = 5_242_880;
    public long ArtworkBytes { get; set; } = 5_242_880;
    public long ProofBytes { get; set; } = 5_242_880;
}
public sealed class StorageOptionsValidator(IHostEnvironment environment, IConfiguration configuration) : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var errors = new List<string>();
        if (options.Provider is not ("Local" or "S3")) errors.Add("Storage:Provider must be Local or S3.");
        if (environment.IsProduction() && options.Provider == "Local" && !options.AllowLocalInProduction) errors.Add("Production requires S3; local rollback requires explicit Storage:AllowLocalInProduction and durable private/public mounts.");
        if (options.OrphanGraceHours is < 24 or > 2160) errors.Add("Storage:OrphanGraceHours must be between 24 and 2160.");
        var maximum = configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 5_242_880;
        if (new[] { options.Limits.AvatarBytes, options.Limits.ProductBytes, options.Limits.ArtworkBytes, options.Limits.ProofBytes }.Any(limit => limit <= 0 || limit > Math.Min(maximum, 5_242_880)))
            errors.Add("Storage:Limits must be positive and cannot exceed the existing 5 MiB endpoint/global upload limit.");
        if (options.Provider == "S3")
        {
            var s3 = options.S3;
            static bool Bucket(string value) => Regex.IsMatch(value, "^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$") && !value.Contains("..") && !System.Net.IPAddress.TryParse(value, out _);
            if (!Bucket(s3.PublicBucket) || !Bucket(s3.PrivateBucket) || s3.PublicBucket == s3.PrivateBucket) errors.Add("S3 requires two distinct, valid public/private bucket names.");
            if (!Regex.IsMatch(s3.Region, "^[a-z]{2}(-[a-z]+)+-[0-9]+$")) errors.Add("Storage:S3:Region must be an AWS region identifier.");
            if (s3.PublicDelivery is not ("ApiProxy" or "CloudFront")) errors.Add("Storage:S3:PublicDelivery must be ApiProxy or CloudFront.");
            if (environment.IsProduction() && s3.PublicDelivery != "CloudFront") errors.Add("Production S3 public delivery requires CloudFront with OAC.");
            if (s3.PublicDelivery == "CloudFront" && (!Uri.TryCreate(s3.PublicBaseUrl, UriKind.Absolute, out var url) || url.Scheme != "https" || url.IsLoopback || url.UserInfo.Length != 0 || url.Query.Length != 0 || url.Fragment.Length != 0 || url.AbsolutePath != "/" || url.Host.Contains("s3", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Storage:S3:PublicBaseUrl must be an HTTPS CDN origin without path, credentials, query or fragment.");
            var prefixes = new[] { s3.Prefixes.Avatars, s3.Prefixes.Products, s3.Prefixes.Artwork, s3.Prefixes.Proofs };
            if (prefixes.Any(prefix => !Regex.IsMatch(prefix, "^[a-z][a-z0-9-]{0,47}$")) || prefixes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4) errors.Add("S3 prefixes must be distinct single lowercase path segments (max 48 characters).");
            if (s3.PrivateDownloadExpirySeconds is < 15 or > 300) errors.Add("Private download expiry must be between 15 and 300 seconds.");
            if (environment.IsProduction() && (string.IsNullOrWhiteSpace(s3.ReadinessPublicKey) || string.IsNullOrWhiteSpace(s3.ReadinessPrivateKey))) errors.Add("Production S3 readiness requires existing public/private canary references.");
            foreach (var reference in new[] { s3.ReadinessPublicKey, s3.ReadinessPrivateKey }.Where(value => value.Length > 0))
                try { MediaReference.Parse(reference, s3.Prefixes); } catch (InvalidOperationException) { errors.Add("Invalid S3 readiness media reference."); }
            if (s3.ReadinessPublicKey.Length > 0 && !s3.ReadinessPublicKey.StartsWith("s3:public/", StringComparison.Ordinal) || s3.ReadinessPrivateKey.Length > 0 && !s3.ReadinessPrivateKey.StartsWith("s3:private/", StringComparison.Ordinal)) errors.Add("Readiness canaries must match their public/private tiers.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
