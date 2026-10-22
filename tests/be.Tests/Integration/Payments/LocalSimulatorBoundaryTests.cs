using System.Net;
using System.Net.Http.Json;
using System.Text;
using be.Application.Abstractions.Payments;
using be.Domain.Common;
using be.Tests.Simulation;

namespace be.Tests;

public sealed class LocalSimulatorBoundaryTests
{
    [Theory]
    [InlineData("Production", "1", true)]
    [InlineData("Staging", "1", true)]
    [InlineData("Development", null, true)]
    [InlineData("Development", "1", false)]
    public void NonDevelopmentOrMissingOptInCannotStart(string environment, string? enabled, bool argument) => Assert.Throws<InvalidOperationException>(() => SimulatorSafety.RequireDevelopment(environment, enabled, argument));

    [Theory]
    [InlineData("remote.example.test")]
    [InlineData("localhost;Database=AmaaraCreationsDB")]
    [InlineData("tcp:remote.example.test,1433")]
    public void SqlTargetCannotBeAnExistingBusinessDatabaseOrRemoteServer(string server) => Assert.Throws<InvalidOperationException>(() => SimulatorSafety.NewConnection(server, "LKR"));

    [Fact]
    public void OnlyLoopbackHostCanReachSimulator()
    {
        Assert.True(SimulatorSafety.LocalRequest("Development", IPAddress.Loopback, "127.0.0.1:5208"));
        Assert.False(SimulatorSafety.LocalRequest("Production", IPAddress.Loopback, "127.0.0.1:5208"));
        Assert.False(SimulatorSafety.LocalRequest("Development", IPAddress.Parse("192.0.2.1"), "127.0.0.1:5208"));
        Assert.False(SimulatorSafety.LocalRequest("Development", IPAddress.Loopback, "evil.example.test:5208"));
    }

    [Fact]
    public async Task NormalApplicationNeverExposesSimulatorRoutes()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/simulator/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/simulator/api/events", new { })).StatusCode);
        Assert.DoesNotContain(typeof(be.Infrastructure.Persistence.ApplicationDbContext).Assembly.GetReferencedAssemblies(), name => name.Name!.Contains("Tests") || name.Name!.Contains("Simulator"));
    }

    [Fact]
    public void SimulatedWebhookRejectsMissingSignatureAndAlteredPayload()
    {
        var parser = new SimulatorWebhookParser();
        var state = new GatewayStatus("123456789012", "test", "reference", "LKR", 100, 100, "payment", "SIMULATED_success", GatewayState.Paid, "SIMULATED") { Mode = SimulatorSafety.Mode, InvoiceTest = true };
        var payload = parser.Payload(Guid.NewGuid(), state); var signature = parser.Sign(payload);
        Assert.Equal(state, parser.ReadVerified(payload, signature));
        Assert.Throws<CommerceException>(() => parser.ReadVerified(payload, ""));
        Assert.Throws<CommerceException>(() => parser.ReadVerified(payload.Concat(Encoding.UTF8.GetBytes(" ")).ToArray(), signature));
        Assert.Throws<CommerceException>(() => new SimulatorWebhookParser().ReadVerified(payload, signature));
    }
}
