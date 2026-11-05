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
    public async Task VerifiedStaffSignInReturnsOnlyAnMfaCompleteManagementSession()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory);
        using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5179");
        var code = TotpFixture.Code;
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/sign-in", Credentials(fixture, true, code));
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, value => value.StartsWith("amaara_refresh_staff=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase) && value.Contains("path=/api/v1/auth/staff"));
        Assert.DoesNotContain(cookies, value => value.StartsWith("amaara_refresh="));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, claim => claim.Type == "session_kind" && claim.Value == "management");
        Assert.Contains(jwt.Claims, claim => claim.Type == "amr" && claim.Value == "mfa");
        Assert.False(body.TryGetProperty("refreshToken", out _));
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal("authorized", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/cart")).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True((await db.RefreshSessions.SingleAsync()).Management);
        Assert.Single(await db.AuditLogs.Where(log => log.Action == "StaffSignInSucceeded").ToListAsync());
        using var replay = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.PostAsJsonAsync("/api/v1/auth/staff/sign-in", Credentials(fixture, true, code))).StatusCode);
        (await client.PostAsJsonAsync("/api/v1/auth/staff/logout", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
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
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/sign-in", new
        {
            email = fixture.GetProperty("adminEmail").GetString(),
            password = scenario == "wrong-password" ? "Incorrect password for rejection test" : fixture.GetProperty("password").GetString(),
            authenticatorCode = scenario == "missing" ? null : scenario == "wrong-factor" ? "not-a-code" : TotpFixture.Code
        });
        Assert.Equal(scenario == "missing" ? HttpStatusCode.Accepted : scenario == "hostile-origin" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, response.StatusCode);
        if (scenario == "missing") Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
        else Assert.False(response.Headers.Contains("Set-Cookie"));
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
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/sign-in", Credentials(fixture, true, TotpFixture.Code));
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
        (await client.PostAsJsonAsync("/api/v1/auth/staff/sign-in", Credentials(fixture, true))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        Assert.Equal("enroll", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/sign-in")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
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
        (await client.PostAsJsonAsync("/api/v1/auth/staff/sign-in", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString(), recoveryCode = recovery })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        Assert.Equal("enroll", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/sign-in")).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }
}
