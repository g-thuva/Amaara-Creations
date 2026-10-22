using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace be.Tests.Simulation;

public static class SimulatorSafety
{
    public const string Origin = "http://127.0.0.1:5208";
    public const string Storefront = "http://127.0.0.1:5188/Amaara-Creations";
    public const string Management = "http://127.0.0.1:5189/Amaara-Creations";
    public const string Prefix = "AmaaraPhase6Tests_LocalPayments_";
    public const string Provider = "LocalSimulator";
    public const string Mode = "SIMULATED";

    public static void RequireDevelopment(string? environment, string? optIn, bool argument)
    {
        if (environment != "Development" || optIn != "1" || !argument)
            throw new InvalidOperationException("Local payments require Development, AMAARA_LOCAL_PAYMENT_SIMULATOR=1 and --local-payments. This executable cannot run in production.");
    }

    public static string NewConnection(string server, string currency)
    {
        if (!Regex.IsMatch(server, @"^(localhost|127\.0\.0\.1|\.|\(localdb\)\\MSSQLLocalDB)(\\[A-Za-z0-9_]+)?$", RegexOptions.CultureInvariant) || currency is not ("LKR" or "CHF"))
            throw new InvalidOperationException("Choose a loopback SQL Server and explicit LKR or CHF simulator currency.");
        return new SqlConnectionStringBuilder { DataSource = server, InitialCatalog = Prefix + currency + "_" + Guid.NewGuid().ToString("N"), IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, ConnectTimeout = 10 }.ConnectionString;
    }

    public static void CheckConnection(string connection)
    {
        var value = new SqlConnectionStringBuilder(connection);
        if (!Regex.IsMatch(value.InitialCatalog, "^" + Prefix + "(LKR|CHF)_[a-f0-9]{32}$", RegexOptions.CultureInvariant) || !value.IntegratedSecurity || value.AttachDBFilename.Length != 0)
            throw new InvalidOperationException("Only a newly named disposable simulator catalog is allowed.");
        _ = NewConnection(value.DataSource, value.InitialCatalog.Contains("_CHF_") ? "CHF" : "LKR");
    }

    public static bool LocalRequest(string environment, IPAddress? remote, string host) => environment == "Development" && remote != null && IPAddress.IsLoopback(remote) && host == "127.0.0.1:5208";
}
