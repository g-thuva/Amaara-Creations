using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using be.Application.Features.Identity;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class SingleSignInTests
{
    private static async Task<JsonElement> Fixture(AcceptanceApiFactory factory)
    {
        await factory.SeedCatalogueAsync();
        return JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
    }

    private static object Credentials(JsonElement fixture, bool staff, string? code = null) => new
    {
        email = fixture.GetProperty(staff ? "adminEmail" : "customerEmail").GetString(),
        password = fixture.GetProperty("password").GetString(),
        authenticatorCode = code
    };

    [Fact]
    public async Task OneVerifiedFactorCreatesIndependentSessionsButReturnsOnlyAStorefrontToken()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory);
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5178");
        var usedCode = TotpFixture.Code;
        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", Credentials(fixture, true, usedCode));
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, value => value.StartsWith("amaara_refresh_staff=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase) && value.Contains("path=/api/v1/auth/staff"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var storefrontToken = body.GetProperty("token").GetString()!;
        var handler = new JwtSecurityTokenHandler(); var storefront = handler.ReadJwtToken(storefrontToken);
        Assert.Contains(storefront.Claims, claim => claim.Type == "session_kind" && claim.Value == "storefront");
        Assert.False(body.TryGetProperty("management", out _));
        Assert.False(body.TryGetProperty("refreshToken", out _));
        client.DefaultRequestHeaders.Authorization = new("Bearer", storefrontToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin"); client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5179");
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        var managementToken = (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        var management = handler.ReadJwtToken(managementToken);
        Assert.Contains(management.Claims, claim => claim.Type == "session_kind" && claim.Value == "management");
        Assert.NotEqual(storefront.Claims.Single(c => c.Type == "session_family").Value, management.Claims.Single(c => c.Type == "session_family").Value);
        Assert.Equal(storefront.Claims.Single(c => c.Type == "mfa_time").Value, management.Claims.Single(c => c.Type == "mfa_time").Value);
        client.DefaultRequestHeaders.Authorization = new("Bearer", managementToken);
        var state = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session");
        Assert.Equal("authorized", state.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.AuditLogs.Where(log => log.Action == "StaffSignInSucceeded").ToListAsync());
        // A consumed authenticator is not verified again during the handoff.
        using var replay = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.PostAsJsonAsync("/api/v1/auth/sign-in", Credentials(fixture, true, usedCode))).StatusCode);
        (await client.PostAsJsonAsync("/api/v1/auth/staff/logout", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-password")]
    [InlineData("wrong-factor")]
    [InlineData("hostile-origin")]
    public async Task InvalidOrHostileSignInCannotIssueEitherSession(string scenario)
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory);
        using var client = factory.CreateClient();
        if (scenario == "hostile-origin") client.DefaultRequestHeaders.Add("Origin", "https://evil.invalid");
        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new
        {
            email = fixture.GetProperty("adminEmail").GetString(),
            password = scenario == "wrong-password" ? "Incorrect password for rejection test" : fixture.GetProperty("password").GetString(),
            authenticatorCode = scenario == "missing" ? null : scenario == "wrong-factor" ? "not-a-code" : TotpFixture.Code
        });
        Assert.Equal(scenario == "hostile-origin" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.AnyAsync());
    }

    [Fact]
    public async Task CustomerGetsOnlyAShoppingSessionRegardlessOfPostedRole()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = fixture.GetProperty("customerEmail").GetString(), password = fixture.GetProperty("password").GetString(), role = "SuperAdmin", management = true });
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh_staff="));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain("Admin", body.GetProperty("user").GetProperty("roles").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
    }

    [Fact]
    public async Task DisabledManagementRejectsStaffWithoutIssuingSessionsButAllowsCustomers()
    {
        await using var factory = new AcceptanceApiFactory { AdministrationEnabled = false }; var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", Credentials(fixture, true, TotpFixture.Code));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.False(response.Headers.Contains("Set-Cookie"));
        using (var scope = factory.Services.CreateScope()) Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.AnyAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/sign-in", Credentials(fixture, false))).StatusCode);
    }

    [Fact]
    public async Task UnenrolledStaffRestoreSetupWithoutRepeatingPasswordAndCannotUseOperations()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.True((await users.SetTwoFactorEnabledAsync(user, false)).Succeeded);
        }
        (await client.PostAsJsonAsync("/api/v1/auth/sign-in", Credentials(fixture, true))).EnsureSuccessStatusCode();
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal("enrollment_required", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }

    [Fact]
    public async Task RecoverySignInPreparesAccountRepairButDoesNotGrantOperationalAssurance()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        string recovery;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            var codes = await scope.ServiceProvider.GetRequiredService<ISecondFactorService>().RegenerateAsync(user, fixture.GetProperty("password").GetString()!, TotpFixture.Code, default);
            recovery = Assert.IsAssignableFrom<IReadOnlyList<string>>(codes)[0];
        }
        (await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString(), recoveryCode = recovery })).EnsureSuccessStatusCode();
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal("mfa_required", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }
}
