using be.Domain.Common;
using System.Net.Mail;
using be.Infrastructure.Payments.Zahls;

namespace be.Infrastructure.Configuration;

public static class ProductionSettings
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        // Reject retired configuration instead of silently selecting a fallback.
        if (configuration.GetValue<bool>("LocalPaymentSimulator:Enabled") ||
            string.Equals(configuration["Payments:Provider"], "LocalSimulator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("LocalSimulator is unavailable in the business API. The runtime simulator has been removed; configure genuine Zahls TEST payments.");
        if (configuration.GetValue<bool>("Uploads:Scanner:Required") && !configuration.GetValue<bool>("Uploads:Scanner:Enabled"))
            throw new InvalidOperationException("Required upload scanning must be enabled and configured.");
        if (configuration.GetValue<bool>("Uploads:Scanner:Enabled") && !be.Infrastructure.Storage.HttpUploadScanner.ValidEndpoint(configuration["Uploads:Scanner:Endpoint"], out _))
            throw new InvalidOperationException("Upload scanning requires an approved HTTPS endpoint without URL credentials or query parameters.");
        var storage = be.Infrastructure.Storage.StorageOptions.Read(configuration);
        var storageValidation = new be.Infrastructure.Storage.StorageOptionsValidator(environment, configuration).Validate(null, storage);
        if (storageValidation.Failed) throw new InvalidOperationException(string.Join(" ", storageValidation.Failures));
        if (configuration.GetValue<bool?>("Email:Enabled") != false && configuration["Email:Provider"] is not (null or "SMTP" or "DevelopmentCapture"))
            throw new InvalidOperationException("Email:Provider is unsupported; configure an implemented email adapter.");
        if (configuration.GetValue<bool?>("Email:Enabled") == true && configuration["Email:Provider"] != "DevelopmentCapture")
        {
            foreach (var key in new[] { "Host", "Username", "Password", "FromEmail" })
                Require(configuration, "Email:" + key);
            if (!MailAddress.TryCreate(configuration["Email:FromEmail"], out _) || configuration.GetValue<int?>("Email:Port") is not (>= 1 and <= 65535))
                throw new InvalidOperationException("Enabled email requires a valid sender and SMTP port.");
        }
        if (!environment.IsProduction()) return;
        if (configuration.GetValue<bool>("Administration:Enabled"))
        {
            var origins = StartupSettings.GetManagementOrigins(configuration, environment);
            if (origins.Length == 0 || origins.Any(value => !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != "/" || uri.GetLeftPart(UriPartial.Authority) != value))
                throw new InvalidOperationException("Enabled production management requires exact approved HTTPS Administration:AllowedOrigins.");
        }
        var hosts = Require(configuration, "AllowedHosts").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(host => host.Contains('*') || host.Contains('<') || host.Contains('{') || Uri.CheckHostName(host) == UriHostNameType.Unknown))
            throw new InvalidOperationException("Production requires explicit valid AllowedHosts.");
        var keys = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Require(configuration, "DataProtection:KeysPath")));
        var publicRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath((environment as IWebHostEnvironment)?.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot")));
        if (publicRoot.TrimEnd(Path.DirectorySeparatorChar) == Path.GetPathRoot(publicRoot)?.TrimEnd(Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Production web root must be a dedicated public directory.");
        var contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.ContentRootPath));
        if (configuration.GetValue<bool>("Authentication:RequirePasswordBlocklist") && string.IsNullOrWhiteSpace(configuration["Authentication:PasswordBlocklistPath"]))
            throw new InvalidOperationException("Required offline password blocklist is not configured.");
        if (configuration["Authentication:PasswordBlocklistPath"] is { Length: > 0 } corpus)
        {
            var corpusRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(corpus));
            if (corpusRoot.Equals(publicRoot, StringComparison.OrdinalIgnoreCase) || corpusRoot.StartsWith(publicRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Password blocklist storage must remain outside the public root.");
        }
        var privateMedia = Path.GetFullPath(Path.Combine(contentRoot, "App_Data", "private-media"));
        if (contentRoot.Equals(publicRoot, StringComparison.OrdinalIgnoreCase) || contentRoot.StartsWith(publicRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || privateMedia.Equals(publicRoot, StringComparison.OrdinalIgnoreCase) || privateMedia.StartsWith(publicRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production web root cannot contain application configuration or private media.");
        if (keys.Equals(publicRoot, StringComparison.OrdinalIgnoreCase) || keys.StartsWith(publicRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Data-protection keys must be outside the public web root.");
        var frontend = Require(configuration, "Authentication:FrontendBaseUrl");
        if (!Uri.TryCreate(frontend, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Production requires an HTTPS authentication frontend URL without credentials, query or fragment.");
        if (configuration.GetValue<bool>("Checkout:AllowDevelopmentShipping"))
            throw new InvalidOperationException("Development shipping cannot be enabled in Production.");
        if (configuration.GetValue<bool>("Zahls:Enabled"))
            ZahlsPaymentGateway.ValidateConfiguration(configuration.GetSection("Zahls").Get<ZahlsOptions>()!, configuration["Checkout:Currency"] ?? Money.DefaultCurrency);
    }

    private static string Require(IConfiguration config, string key) => !string.IsNullOrWhiteSpace(config[key])
        ? config[key]! : throw new InvalidOperationException($"Production configuration requires {key}.");
}
