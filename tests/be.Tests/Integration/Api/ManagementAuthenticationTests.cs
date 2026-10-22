using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Application.Features.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class ManagementAuthenticationTests
{
    [Fact]
    public async Task ApprovedStorefrontOriginCannotExchangeOrInspectManagementCookies()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        await Login(client, fixture, true);
        client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5178");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/auth/staff/session")).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin"); client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5179");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
    }
    private static async Task<JsonElement> Fixture(AcceptanceApiFactory factory)
    {
        await factory.SeedCatalogueAsync();
        return JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
    }
    private static async Task<JsonElement> Login(HttpClient client, JsonElement fixture, bool staff, bool publicEntry = false)
    {
        var response = await client.PostAsJsonAsync(staff && !publicEntry ? "/api/v1/auth/staff/login" : "/api/v1/auth/login", new { email = fixture.GetProperty(staff ? "adminEmail" : "customerEmail").GetString(), password = fixture.GetProperty("password").GetString(), authenticatorCode = staff ? TotpFixture.Code : null });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", result.GetProperty("token").GetString());
        return result;
    }
    [Fact]
    public async Task CustomerAndManagementCookiesRefreshAndLogoutIndependently()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var customer = await Login(client, fixture, false); var staff = await Login(client, fixture, true);
        var state = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session");
        Assert.Equal("authorized", state.GetProperty("status").GetString());
        Assert.Contains("ManageCatalog", state.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));
        Assert.DoesNotContain("ManageStaff", state.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()));
        var publicRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { }); publicRefresh.EnsureSuccessStatusCode();
        Assert.Equal(customer.GetProperty("user").GetProperty("id").GetString(), (await publicRefresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetString());
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        Assert.Equal(staff.GetProperty("user").GetProperty("id").GetString(), (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetString());
        (await client.PostAsJsonAsync("/api/v1/auth/staff/logout", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
    }
    [Fact]
    public async Task StorefrontStaffMfaCannotAuthorizeManagementOrBeReplayedAsAStaffCookie()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        await Login(client, fixture, true, publicEntry: true);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/auth/staff/session")).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { }); refresh.EnsureSuccessStatusCode();
        var raw = refresh.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("amaara_refresh=")).Split(';')[0].Split('=', 2)[1];
        using var attacker = factory.CreateClient(new() { HandleCookies = false });
        attacker.DefaultRequestHeaders.Add("Cookie", "amaara_refresh_staff=" + raw);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
    }
    [Fact]
    public async Task WrongCustomerAndRolelessCredentialsNeverCreateManagementSessionsAndFailuresAreRedacted()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var email = fixture.GetProperty("customerEmail").GetString(); var password = fixture.GetProperty("password").GetString();
        var wrong = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email, password = "An incorrect password" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode); Assert.False(wrong.Headers.Contains("Set-Cookie"));
        var denied = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email, password });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); Assert.False(denied.Headers.Contains("Set-Cookie"));
        using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(email!))!;
        Assert.True((await users.RemoveFromRoleAsync(user, AppRoles.Customer)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email, password })).StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); Assert.False(await db.RefreshSessions.AnyAsync());
        var logs = await db.AuditLogs.Where(x => x.Action == "StaffSignInFailed").ToListAsync(); Assert.Equal(3, logs.Count);
        Assert.All(logs, log => { Assert.DoesNotContain(password!, log.NewValuesJson!); Assert.DoesNotContain(email!, log.NewValuesJson!); });
    }
    [Fact]
    public async Task DisabledManagementStaysClosedAndMissingFactorCannotIssueAStaffCookie()
    {
        await using var factory = new AcceptanceApiFactory { AdministrationEnabled = false }; var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.False(response.Headers.Contains("Set-Cookie"));
        await using var enabled = new AcceptanceApiFactory(); var active = await Fixture(enabled); using var other = enabled.CreateClient();
        var missing = await other.PostAsJsonAsync("/api/v1/auth/staff/login", new { email = active.GetProperty("adminEmail").GetString(), password = active.GetProperty("password").GetString() });
        Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode); Assert.False((await missing.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
    }
    [Fact]
    public async Task UnenrolledStaffReachSetupButNeverOperationalData()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.True((await users.SetTwoFactorEnabledAsync(user, false)).Succeeded);
        }
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() }); response.EnsureSuccessStatusCode();
        Assert.Equal("enroll", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/second-factor")).StatusCode);
    }
    [Fact]
    public async Task ExpiredAssuranceHasNoCapabilitiesAndHostileStaffOriginCannotSignIn()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var login = await Login(client, fixture, true); using var scope = factory.Services.CreateScope();
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("token").GetString());
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
        var old = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user, [AppRoles.SuperAdmin], jwt.Claims.Single(x => x.Type == "session_family").Value, DateTimeOffset.UtcNow.AddMinutes(-16), management: true);
        client.DefaultRequestHeaders.Authorization = new("Bearer", old);
        var state = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session"); Assert.Equal("mfa_required", state.GetProperty("status").GetString()); Assert.Empty(state.GetProperty("permissions").EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://evil.invalid");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() })).StatusCode);
    }
    [Fact]
    public async Task ExplicitNonProductionProvisioningIsIdempotentAuditedAndRevokesOldSessions()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient(); await Login(client, fixture, false);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var tool = new IdentityRoleProvisioner(services.GetRequiredService<UserManager<User>>(), services.GetRequiredService<RoleManager<IdentityRole>>(), services.GetRequiredService<IApplicationDbContext>(), services.GetRequiredService<IRefreshSessionService>(), services.GetRequiredService<IAuditService>());
        var email = fixture.GetProperty("customerEmail").GetString()!;
        await Assert.ThrowsAsync<InvalidOperationException>(() => tool.Assign(email, AppRoles.Admin, "Production", true, "Test approval", default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tool.Assign(email, AppRoles.Admin, "Development", false, "Test approval", default));
        Assert.True(await tool.Assign(email, AppRoles.Admin, "Development", true, "Disposable identity provisioning test", default));
        Assert.False(await tool.Assign(email, AppRoles.Admin, "Development", true, "Same approved target", default));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var db = services.GetRequiredService<ApplicationDbContext>(); Assert.Single(await db.AuditLogs.Where(x => x.Action == "OperatorStaffRoleAssigned").ToListAsync());
        var target = (await services.GetRequiredService<UserManager<User>>().FindByEmailAsync(email))!;
        var adminRole = (await services.GetRequiredService<RoleManager<IdentityRole>>().FindByNameAsync(AppRoles.Admin))!;
        Assert.Single(await db.UserRoles.Where(x => x.UserId == target.Id && x.RoleId == adminRole.Id).ToListAsync());
    }
}
