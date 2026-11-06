using System.Net;
using System.Net.Http.Json;
using System.Text;
using be.Application.Abstractions.Payments;
using be.Domain.Common;
using be.Tests.Fixtures.Payments;
using Microsoft.Data.SqlClient;

namespace be.Tests;

public sealed class LocalSimulatorBoundaryTests
{
    [Theory]
    [InlineData("remote.example.test")]
    [InlineData("localhost;Database=AmaaraCreationsDB")]
    [InlineData("tcp:remote.example.test,1433")]
    [InlineData("localhost,0")]
    [InlineData("localhost,65536")]
    [InlineData("127.0.0.1,1433;Database=master")]
    public void SqlFixtureRejectsRemoteOrInjectedServer(string server) =>
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.NewConnection(server, "CHF"));

    [Fact]
    public void SqlFixtureCannotUseBusinessCatalog() =>
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.CheckConnection("Server=localhost;Database=AmaaraCreationsDB;Integrated Security=true"));

    [Theory]
    [InlineData("localhost,1433")]
    [InlineData("127.0.0.1,51433")]
    public void SqlFixturePreservesExplicitCiAuthenticationForDisposableCatalogs(string server)
    {
        var template = new SqlConnectionStringBuilder
        {
            DataSource = server, InitialCatalog = "AmaaraPhase6Tests_CI_Fixture", UserID = "fixture-ci",
            Password = Guid.NewGuid().ToString("N"), Encrypt = false, TrustServerCertificate = true
        };
        var connection = PaymentFixtureSafety.NewConnection(server, "CHF", template.ConnectionString);
        PaymentFixtureSafety.CheckConnection(connection);
        var actual = new SqlConnectionStringBuilder(connection);
        Assert.Equal(server, actual.DataSource);
        Assert.Equal(template.UserID, actual.UserID);
        Assert.Equal(template.Password, actual.Password);
        Assert.False(actual.IntegratedSecurity);
        Assert.StartsWith(PaymentFixtureSafety.Prefix + "CHF_", actual.InitialCatalog);
        Assert.NotEqual(template.InitialCatalog, actual.InitialCatalog);
    }

    [Fact]
    public void SqlFixtureRejectsBusinessOrMismatchedConnectionTemplates()
    {
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.NewConnection("localhost", "CHF", "Server=localhost;Database=AmaaraCreationsDB;Integrated Security=true"));
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.NewConnection("localhost", "CHF", "Server=remote.example.test;Database=AmaaraPhase6Tests_CI_Fixture;Integrated Security=true"));
    }

    [Fact]
    public async Task NormalApplicationNeverExposesSimulatorRoutes()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/simulator/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/simulator/start?port=5173")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/simulator/api/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/simulator/api/events", new { })).StatusCode);
        Assert.DoesNotContain(typeof(be.Infrastructure.Persistence.ApplicationDbContext).Assembly.GetReferencedAssemblies(), name => name.Name!.Contains("Tests") || name.Name!.Contains("Simulator"));
    }

    [Fact]
    public void SimulatedWebhookRejectsMissingSignatureAndAlteredPayload()
    {
        var parser = new SignedFixtureWebhookParser();
        var state = new GatewayStatus("123456789012", "test", "reference", "LKR", 100, 100, "payment", "SIMULATED_success", GatewayState.Paid, "SIMULATED") { Mode = PaymentFixtureSafety.Mode, InvoiceTest = true };
        var payload = parser.Payload(Guid.NewGuid(), state); var signature = parser.Sign(payload);
        Assert.Equal(state, parser.ReadVerified(payload, signature));
        Assert.Throws<CommerceException>(() => parser.ReadVerified(payload, ""));
        Assert.Throws<CommerceException>(() => parser.ReadVerified(payload.Concat(Encoding.UTF8.GetBytes(" ")).ToArray(), signature));
        Assert.Throws<CommerceException>(() => new SignedFixtureWebhookParser().ReadVerified(payload, signature));
    }
}
