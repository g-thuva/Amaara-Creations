using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class MfaChallengeScopeTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ChallengesStayBoundToTheirEntryAndCannotBecomeCustomerSessions(bool managementEntry)
    {
        await using var factory = new AcceptanceApiFactory(); await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient();
        var prefix = managementEntry ? "/api/v1/auth/staff" : "/api/v1/auth";
        var cookieName = managementEntry ? "amaara_refresh_staff_signin=" : "amaara_refresh_signin=";
        var started = await client.PostAsJsonAsync(prefix + "/sign-in", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() });
        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var pending = started.Headers.GetValues("Set-Cookie").Last(value => value.StartsWith(cookieName)).Split(';')[0].Split('=', 2)[1];
        using var other = factory.CreateClient(new() { HandleCookies = false });
        other.DefaultRequestHeaders.Add("Cookie", (managementEntry ? "amaara_refresh_signin=" : "amaara_refresh_staff_signin=") + pending);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.PostAsJsonAsync((managementEntry ? "/api/v1/auth" : "/api/v1/auth/staff") + "/sign-in/verify", new { code = TotpFixture.Code })).StatusCode);
        var verified = await client.PostAsJsonAsync(prefix + "/sign-in/verify", new { code = TotpFixture.Code });
        verified.EnsureSuccessStatusCode();
        var auth = await verified.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.GetProperty("token").GetString());
        foreach (var path in new[] { "cart", "wishlist", "addresses", "users/profile", "orders", "reviews/mine", "custom-designs", "account/requests" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/" + path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var session = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.SingleAsync();
        Assert.True(session.Management); Assert.NotNull(session.MfaVerifiedAt);
    }
}
