using System.Net;
using Microsoft.Data.SqlClient;

namespace be.Tests;

public sealed class HealthSemanticsTests
{
    [SqlCommerceFact]
    public async Task DatabaseOutageFailsReadinessWithoutFailingLivenessOrExposingDiagnostics()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        var database = "AmaaraPhase6Tests_Health_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = database;
        await using var factory = new CommerceApiFactory(connection.ConnectionString);
        factory.Gateway.Available = false; // Optional payment availability is not database readiness.
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        connection.InitialCatalog = "master";
        await using var master = new SqlConnection(connection.ConnectionString);
        await master.OpenAsync();
        try
        {
            // The name is generated here, never supplied by configuration or a business database.
            await using (var offline = new SqlCommand($"ALTER DATABASE [{database}] SET OFFLINE WITH ROLLBACK IMMEDIATE", master))
                await offline.ExecuteNonQueryAsync();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
            using var readiness = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
            Assert.Equal("{\"status\":\"not_ready\"}", await readiness.Content.ReadAsStringAsync());
        }
        finally
        {
            await using var online = new SqlCommand($"ALTER DATABASE [{database}] SET ONLINE", master);
            await online.ExecuteNonQueryAsync();
        }
        // SqlClient briefly blocks reconnects after a login failure; recovery is eventual.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            using var response = await client.GetAsync("/health/ready");
            if (response.StatusCode == HttpStatusCode.OK) break;
            Assert.True(DateTime.UtcNow < deadline, "Readiness did not recover after the disposable database returned online.");
            await Task.Delay(500);
        }
    }
}
