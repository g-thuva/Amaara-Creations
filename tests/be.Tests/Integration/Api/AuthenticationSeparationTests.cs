using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class AuthenticationSeparationTests
{
    [Theory]
    [InlineData("/api/v1/auth")] [InlineData("/api/v1/auth/staff")]
    public async Task AnonymousStateIsNotAnAuthenticationFailure(string prefix)
    {
        await using var factory = new AcceptanceApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync(prefix + "/sign-in");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("credentials", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData(AppRoles.Admin)] [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.CatalogManager)] [InlineData(AppRoles.ProductionStaff)]
    [InlineData(AppRoles.CustomerSupport)] [InlineData(AppRoles.Marketing)] [InlineData(AppRoles.Analyst)]
    public async Task PrivilegedRolesUseSharedLoginAndReceiveOnlyManagementAccessAfterMfa(string role)
    {
        await using var factory = new AcceptanceApiFactory(); await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(role)) Assert.True((await roles.CreateAsync(new(role))).Succeeded);
            var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.True((await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user))).Succeeded);
            Assert.True((await users.AddToRolesAsync(user, [AppRoles.Customer, role])).Succeeded);
        }
        using var client = factory.CreateClient();
        var credentials = new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() };
        foreach (var alias in new[] { "login", "sign-in" })
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/" + alias, credentials);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh_signin=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
            Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
        }
        using (var verification = factory.Services.CreateScope())
            Assert.False(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.AnyAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/cart")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        var verified = await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = TotpFixture.Code });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.Contains(verified.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh_staff=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(verified.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh="));
        var auth = await verified.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/cart")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        using var completed = factory.Services.CreateScope();
        var session = await completed.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.SingleAsync(value => value.RevokedAt == null);
        Assert.True(session.Management); Assert.NotNull(session.MfaVerifiedAt);
    }
}
