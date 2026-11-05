using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class PersistentSessionTests
{
    private static async Task<(JsonElement Fixture, JsonElement Login)> Login(AcceptanceApiFactory factory, HttpClient client, bool staff = false, Action<HttpResponseMessage>? capture = null)
    {
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        var response = await client.PostAsJsonAsync(staff ? "/api/v1/auth/staff/login" : "/api/v1/auth/sign-in", new
        {
            email = fixture.GetProperty(staff ? "adminEmail" : "customerEmail").GetString(),
            password = fixture.GetProperty("password").GetString(),
            authenticatorCode = staff ? TotpFixture.Code : null
        });
        response.EnsureSuccessStatusCode();
        capture?.Invoke(response);
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        return (fixture, login);
    }
    private static string Family(JsonElement login) => new JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("token").GetString()).Claims.Single(c => c.Type == "session_family").Value;

    [Theory]
    [InlineData(false, 15, 10080, 43200)]
    [InlineData(true, 10, 30, 720)]
    public async Task SessionsUseSeparateBoundedPoliciesAndRenewalNeverExtendsIdleOrAbsoluteLimits(bool staff, int access, int idle, int absolute)
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient();
        var (_, login) = await Login(factory, client, staff);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var original = await db.RefreshSessions.AsNoTracking().SingleAsync(s => s.FamilyId == Family(login));
        Assert.InRange((original.IdleExpiresAt!.Value - original.FamilyCreatedAt!.Value).TotalMinutes, idle - .01, idle + .01);
        Assert.InRange((original.AbsoluteExpiresAt!.Value - original.FamilyCreatedAt.Value).TotalMinutes, absolute - .01, absolute + .01);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.GetProperty("token").GetString());
        Assert.InRange((jwt.ValidTo - original.CreatedAt).TotalMinutes, access - .1, access + .1);
        var response = await client.PostAsJsonAsync(staff ? "/api/v1/auth/staff/refresh" : "/api/v1/auth/refresh", new { });
        response.EnsureSuccessStatusCode();
        var rows = await db.RefreshSessions.AsNoTracking().Where(s => s.FamilyId == original.FamilyId).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.Equal(original.AbsoluteExpiresAt, row.AbsoluteExpiresAt); Assert.Equal(original.IdleExpiresAt, row.IdleExpiresAt); Assert.Equal(original.FamilyCreatedAt, row.FamilyCreatedAt); });
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase) && c.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("refreshToken", out _));
    }

    [Theory]
    [InlineData(false, "idle")][InlineData(true, "idle")]
    [InlineData(false, "absolute")][InlineData(true, "absolute")]
    [InlineData(false, "revoked")][InlineData(true, "revoked")]
    [InlineData(false, "legacy")][InlineData(true, "legacy")]
    [InlineData(false, "stamp")][InlineData(true, "stamp")]
    [InlineData(false, "roles")][InlineData(true, "roles")]
    public async Task InvalidServerStateRejectsBothAccessAndRefreshRegardlessOfClientTimestamps(bool staff, string state)
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient();
        var (_, login) = await Login(factory, client, staff);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await db.RefreshSessions.Include(s => s.User).SingleAsync(s => s.FamilyId == Family(login));
            if (state == "idle") session.IdleExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            if (state == "absolute") session.AbsoluteExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            if (state == "revoked") session.RevokedAt = DateTime.UtcNow;
            if (state == "legacy") session.AbsoluteExpiresAt = null;
            if (state == "stamp") session.User!.SecurityStamp = Guid.NewGuid().ToString();
            if (state == "roles") session.RolesHash = "outdated-role-snapshot";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(staff ? "/api/v1/auth/staff/refresh" : "/api/v1/auth/refresh", new { idleExpiresAt = DateTime.UtcNow.AddYears(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/activity", new { })).StatusCode);
    }

    [Fact]
    public async Task OnlyExplicitActivityExtendsIdleTimeAndNeverBeyondTheOriginalAbsoluteLimit()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient(); var (_, login) = await Login(factory, client);
        DateTime absolute;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var session = await db.RefreshSessions.SingleAsync(s => s.FamilyId == Family(login));
            session.IdleExpiresAt = DateTime.UtcNow.AddMinutes(1); absolute = DateTime.UtcNow.AddMinutes(3); session.AbsoluteExpiresAt = absolute; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/activity", new { })).StatusCode);
        using var check = factory.Services.CreateScope();
        var current = await check.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.SingleAsync(s => s.FamilyId == Family(login));
        Assert.Equal(absolute, current.IdleExpiresAt); Assert.Equal(absolute, current.AbsoluteExpiresAt);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { }); refresh.EnsureSuccessStatusCode();
        var token = (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.True(new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo <= absolute);
    }

    [Fact]
    public async Task RotationConflictReturnsNoCredentialAndDelayedReplayRevokesTheFamily()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient(); var (_, login) = await Login(factory, client);
        string raw;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await db.RefreshSessions.Include(s => s.User).SingleAsync(s => s.FamilyId == Family(login))).User!;
            raw = (await scope.ServiceProvider.GetRequiredService<IRefreshSessionService>().CreateSessionAsync(user, new("rotation-fixture", null))).RawToken;
        }
        using var browser = factory.CreateClient(new() { HandleCookies = false }); browser.DefaultRequestHeaders.Add("Cookie", "amaara_refresh=" + Uri.EscapeDataString(raw));
        Assert.Equal(HttpStatusCode.OK, (await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        var conflict = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.False(conflict.Headers.Contains("Set-Cookie"));
        Assert.Equal("SESSION_REFRESH_CONFLICT", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var service = scope.ServiceProvider.GetRequiredService<IRefreshSessionService>();
            var old = await db.RefreshSessions.SingleAsync(s => s.TokenHash == service.HashRefreshToken(raw)); old.RevokedAt = DateTime.UtcNow.AddSeconds(-11); await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        using var verify = factory.Services.CreateScope(); var context = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hash = verify.ServiceProvider.GetRequiredService<IRefreshSessionService>().HashRefreshToken(raw);
        var family = (await context.RefreshSessions.SingleAsync(s => s.TokenHash == hash)).FamilyId;
        Assert.All(await context.RefreshSessions.Where(s => s.FamilyId == family).ToListAsync(), s => Assert.NotNull(s.RevokedAt));
    }

    [Fact]
    public async Task LogoutWithASupersededCookieRevokesItsCurrentReplacement()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient(); var (_, login) = await Login(factory, client);
        using var scope = factory.Services.CreateScope(); var sessions = scope.ServiceProvider.GetRequiredService<IRefreshSessionService>();
        var user = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.Include(s => s.User).SingleAsync(s => s.FamilyId == Family(login))).User!;
        var issue = await sessions.CreateSessionAsync(user, new("logout-race", null));
        var replacement = await sessions.RotateAsync(issue.RawToken, new("logout-race", null)); Assert.NotNull(replacement);
        await sessions.RevokeAsync(issue.RawToken);
        Assert.Null(await sessions.RotateAsync(replacement.RawToken, new("logout-race", null)));
    }

    [Theory]
    [InlineData("https://evil.invalid", "cross-site")]
    [InlineData(null, "same-site")]
    [InlineData(null, "cross-site")]
    public async Task CookieMutationsRejectUntrustedOrMissingBrowserOrigins(string? origin, string site)
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient(); await Login(factory, client);
        if (origin != null) client.DefaultRequestHeaders.Add("Origin", origin);
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", site);
        foreach (var path in new[] { "refresh", "logout", "activity" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/" + path, new { })).StatusCode);
    }
    [Fact]
    public async Task ValidPrivilegedSessionRestoresAfterFifteenMinutesButSensitiveActionsStillRequireFreshMfa()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient();
        string cookies = "";
        var (_, login) = await Login(factory, client, true, response => cookies = string.Join("; ", response.Headers.GetValues("Set-Cookie").Where(value => value.StartsWith("amaara_refresh_staff")).Select(value => value.Split(';')[0])));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.RefreshSessions.Include(value => value.User).SingleAsync(value => value.FamilyId == Family(login));
        var verifiedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-16).ToUnixTimeSeconds());
        session.MfaVerifiedAt = verifiedAt; session.FamilyCreatedAt = verifiedAt.UtcDateTime;
        await db.SaveChangesAsync();
        var cookie = scope.ServiceProvider.GetRequiredService<be.Api.Identity.RefreshCookie>();
        var request = new Microsoft.AspNetCore.Http.DefaultHttpContext(); request.Request.Headers.Cookie = cookies;
        var raw = cookie.Read(request.Request, true)!;
        var responseContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        cookie.Write(responseContext.Response, new(new(), raw, session.ExpiresAt, new(session.UserId, session.User!.SecurityStamp!, session.FamilyId, verifiedAt.ToUnixTimeSeconds())), true);
        using var returning = factory.CreateClient(new() { HandleCookies = false });
        returning.DefaultRequestHeaders.Add("Cookie", string.Join("; ", responseContext.Response.Headers.SetCookie.Select(value => value!.Split(';')[0])));
        var refresh = await returning.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>(); var token = body.GetProperty("token").GetString();
        returning.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var state = await returning.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session"); Assert.Equal("authorized", state.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await returning.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "JWT"));
        var authorization = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        Assert.False((await authorization.AuthorizeAsync(principal, null, new[] { new be.Api.Identity.StaffAuthorization(true) })).Succeeded);
    }

    [Fact]
    public async Task PasswordChangeRevokesOtherDevicesAndRequiresTheNewCredential()
    {
        await using var factory = new AcceptanceApiFactory(); using var client = factory.CreateClient(); var (fixture, _) = await Login(factory, client);
        var email = fixture.GetProperty("customerEmail").GetString(); var password = fixture.GetProperty("password").GetString();
        using var other = factory.CreateClient();
        var second = await other.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password }); second.EnsureSuccessStatusCode();
        other.DefaultRequestHeaders.Authorization = new("Bearer", (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        var next = password + " changed";
        (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = next, confirmPassword = next })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password = next })).StatusCode);
    }

}
