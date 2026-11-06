using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace be.Tests.Fixtures.Payments;

public static class PaymentFixtureSafety
{
    public const string Origin = "http://127.0.0.1:5198";
    public const string Storefront = "http://127.0.0.1:5178/Amaara-Creations";
    public const string Management = "http://127.0.0.1:5179/Amaara-Creations";
    public const string Prefix = "AmaaraPhase6Tests_PaymentContracts_";
    public const string Provider = "ContractTestGateway";
    public const string Mode = "TEST";
    public static string NewConnection(string server, string currency, string? connectionTemplate = null)
    {
        var match = Regex.Match(server, @"^(localhost|127\.0\.0\.1|\.|\(localdb\)\\MSSQLLocalDB)(\\[A-Za-z0-9_]+)?(?:,(?<port>[0-9]{1,5}))?$", RegexOptions.CultureInvariant);
        if (!match.Success || match.Groups["port"].Success && (!int.TryParse(match.Groups["port"].Value, out var port) || port is < 1 or > 65535) || currency is not ("LKR" or "CHF"))
            throw new InvalidOperationException("Choose a loopback SQL Server and explicit LKR or CHF payment fixture currency.");
        var connection = connectionTemplate == null
            ? new SqlConnectionStringBuilder { DataSource = server, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true }
            : new SqlConnectionStringBuilder(connectionTemplate);
        if (!string.Equals(connection.DataSource, server, StringComparison.OrdinalIgnoreCase) || connection.AttachDBFilename.Length != 0
            || connectionTemplate != null && !connection.InitialCatalog.StartsWith("AmaaraPhase6Tests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Payment fixtures require an explicit disposable loopback SQL connection.");
        connection.InitialCatalog = Prefix + currency + "_" + Guid.NewGuid().ToString("N");
        connection.ConnectTimeout = 10;
        return connection.ConnectionString;
    }

    public static void CheckConnection(string connection)
    {
        var value = new SqlConnectionStringBuilder(connection);
        if (!Regex.IsMatch(value.InitialCatalog, "^" + Prefix + "(LKR|CHF)_[a-f0-9]{32}$", RegexOptions.CultureInvariant) || value.AttachDBFilename.Length != 0
            || !value.IntegratedSecurity && (string.IsNullOrWhiteSpace(value.UserID) || string.IsNullOrEmpty(value.Password)))
            throw new InvalidOperationException("Only a newly named disposable payment fixture catalog is allowed.");
        _ = NewConnection(value.DataSource, value.InitialCatalog.Contains("_CHF_") ? "CHF" : "LKR");
    }

}
