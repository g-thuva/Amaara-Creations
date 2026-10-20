using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace be.Tests;

public sealed class AuthenticationRateLimitTests
{
    [Fact]
    public async Task RefreshHasAnIndependentBoundedBudgetWithoutWeakeningLoginProtection()
    {
        await using var original = new CommerceApiFactory();
        await using var limited = original.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:RateLimitPermitLimit"] = "1",
                ["Authentication:RefreshRateLimitPermitLimit"] = "3"
            })));
        using var client = limited.CreateClient();
        var credentials = new { email = "missing@example.test", password = "InvalidPassword1!" };
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/login", credentials)).StatusCode);
        for (var attempt = 0; attempt < 3; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/login", credentials)).StatusCode);
    }
}
