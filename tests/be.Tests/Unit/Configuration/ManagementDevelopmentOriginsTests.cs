using be.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace be.Tests;
public sealed class ManagementDevelopmentOriginsTests
{
    private static WebApplicationBuilder Builder(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.Sources.Clear();
        return builder;
    }
    [Fact]
    public void DevelopmentAllowsOnlyExplicitShopAndManagementLoopbackOriginsByDefault()
    {
        var origins = StartupSettings.GetAllowedOrigins(Builder("Development"));
        Assert.Contains("http://localhost:5179", origins); Assert.Contains("http://127.0.0.1:5179", origins);
        Assert.Contains("http://localhost:5173", origins); Assert.Equal(4, origins.Length);
    }
    [Fact]
    public void ProductionStillRequiresAnExplicitAllowlist()
    {
        Assert.Throws<InvalidOperationException>(() => StartupSettings.GetAllowedOrigins(Builder("Production")));
    }
    [Fact]
    public void ExplicitOriginsAreNeverExpanded()
    {
        var builder = Builder("Development"); builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://approved.example.test" });
        Assert.Equal(["https://approved.example.test"], StartupSettings.GetAllowedOrigins(builder));
    }
}
