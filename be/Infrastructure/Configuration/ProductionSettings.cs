using System.Net.Mail;
using be.Infrastructure.Payments.Zahls;

namespace be.Infrastructure.Configuration;

public static class ProductionSettings
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["Uploads:Provider"] ?? "Local";
        if (!provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Uploads:Provider is unsupported; configure an implemented private-storage adapter.");
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
        var hosts = Require(configuration, "AllowedHosts").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(host => host.Contains('*') || host.Contains('<') || host.Contains('{') || Uri.CheckHostName(host) == UriHostNameType.Unknown))
            throw new InvalidOperationException("Production requires explicit valid AllowedHosts.");
        var keys = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Require(configuration, "DataProtection:KeysPath")));
        var publicRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath((environment as IWebHostEnvironment)?.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot")));
        if (publicRoot.TrimEnd(Path.DirectorySeparatorChar) == Path.GetPathRoot(publicRoot)?.TrimEnd(Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Production web root must be a dedicated public directory.");
        if (keys.Equals(publicRoot, StringComparison.OrdinalIgnoreCase) || keys.StartsWith(publicRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Data-protection keys must be outside the public web root.");
        var frontend = Require(configuration, "Authentication:FrontendBaseUrl");
        if (!Uri.TryCreate(frontend, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Production requires an HTTPS authentication frontend URL without credentials, query or fragment.");
        if (configuration.GetValue<bool>("Checkout:AllowDevelopmentShipping"))
            throw new InvalidOperationException("Development shipping cannot be enabled in Production.");
        if (configuration.GetValue<bool>("Zahls:Enabled"))
            ZahlsPaymentGateway.ValidateConfiguration(configuration.GetSection("Zahls").Get<ZahlsOptions>()!, configuration["Checkout:Currency"] ?? "LKR");
    }

    private static string Require(IConfiguration config, string key) => !string.IsNullOrWhiteSpace(config[key])
        ? config[key]! : throw new InvalidOperationException($"Production configuration requires {key}.");
}
