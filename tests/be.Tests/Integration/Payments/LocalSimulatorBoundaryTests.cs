using System.Net;
using System.Net.Http.Json;
using System.Text;
using be.Application.Abstractions.Payments;
using be.Domain.Common;
using be.Tests.Fixtures.Payments;

namespace be.Tests;

public sealed class LocalSimulatorBoundaryTests
{
    [Theory]
    [InlineData("remote.example.test")]
    [InlineData("localhost;Database=AmaaraCreationsDB")]
    [InlineData("tcp:remote.example.test,1433")]
    public void SqlFixtureRejectsRemoteOrInjectedServer(string server) =>
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.NewConnection(server, "CHF"));

    [Fact]
    public void SqlFixtureCannotUseBusinessCatalog() =>
        Assert.Throws<InvalidOperationException>(() => PaymentFixtureSafety.CheckConnection("Server=localhost;Database=AmaaraCreationsDB;Integrated Security=true"));

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
