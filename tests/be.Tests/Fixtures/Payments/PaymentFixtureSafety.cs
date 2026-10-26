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
    public static string NewConnection(string server, string currency)
    {
        if (!Regex.IsMatch(server, @"^(localhost|127\.0\.0\.1|\.|\(localdb\)\\MSSQLLocalDB)(\\[A-Za-z0-9_]+)?$", RegexOptions.CultureInvariant) || currency is not ("LKR" or "CHF"))
            throw new InvalidOperationException("Choose a loopback SQL Server and explicit LKR or CHF payment fixture currency.");
        return new SqlConnectionStringBuilder { DataSource = server, InitialCatalog = Prefix + currency + "_" + Guid.NewGuid().ToString("N"), IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, ConnectTimeout = 10 }.ConnectionString;
    }

    public static void CheckConnection(string connection)
    {
        var value = new SqlConnectionStringBuilder(connection);
        if (!Regex.IsMatch(value.InitialCatalog, "^" + Prefix + "(LKR|CHF)_[a-f0-9]{32}$", RegexOptions.CultureInvariant) || !value.IntegratedSecurity || value.AttachDBFilename.Length != 0)
            throw new InvalidOperationException("Only a newly named disposable payment fixture catalog is allowed.");
        _ = NewConnection(value.DataSource, value.InitialCatalog.Contains("_CHF_") ? "CHF" : "LKR");
    }

}
