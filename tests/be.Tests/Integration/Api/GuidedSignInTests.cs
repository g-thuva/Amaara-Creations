using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class GuidedSignInTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static async Task<JsonElement> Fixture(AcceptanceApiFactory factory)
    {
        await factory.SeedCatalogueAsync(); return JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
    }
    private static Task<HttpResponseMessage> Start(HttpClient client, JsonElement fixture, bool staff = true) => client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = fixture.GetProperty(staff ? "adminEmail" : "customerEmail").GetString(), password = fixture.GetProperty("password").GetString() });
    private static async Task AssertNoSessions(AcceptanceApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.AnyAsync());
    }
    private static async Task Unenroll(AcceptanceApiFactory factory, JsonElement fixture)
    {
        using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Assert.True((await users.SetTwoFactorEnabledAsync((await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!, false)).Succeeded);
    }

    [Fact]
    public async Task DefaultCustomerCompletesWithoutAnyMfaOrStaffSession()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var response = await Start(client, fixture, false); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(body.TryGetProperty("token", out _)); Assert.False(body.TryGetProperty("enrollment", out _));
        Assert.DoesNotContain(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh_staff="));
    }

    [Theory]
    [InlineData("unknown-role")]
    [InlineData("unconfirmed")]
    public async Task InvalidAccountStateCannotSilentlyBecomeACustomer(string change)
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            if (change == "unknown-role")
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                Assert.True((await roles.CreateAsync(new IdentityRole("Unrecognized"))).Succeeded);
                Assert.True((await users.AddToRoleAsync(user, "Unrecognized")).Succeeded);
            }
            else { user.EmailConfirmed = false; Assert.True((await users.UpdateAsync(user)).Succeeded); }
        }
        var response = await Start(client, fixture);
        Assert.Equal(change == "unknown-role" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
        await AssertNoSessions(factory);
    }

    [Fact]
    public async Task ReturningStaffGetsOnlyAnOpaquePendingCookieUntilValidCodeAndCannotReplayTheChallenge()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        var response = await Start(client, fixture); Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("verify", body.GetProperty("status").GetString()); Assert.False(body.TryGetProperty("token", out _));
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("amaara_refresh_signin=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.True(response.Headers.CacheControl!.NoStore); await AssertNoSessions(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = "invalid" })).StatusCode);
        await AssertNoSessions(factory);
        var verified = await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = TotpFixture.Code }); verified.EnsureSuccessStatusCode();
        Assert.True((await verified.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = TotpFixture.Code })).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }

    [Theory]
    [InlineData("legacy-valid")]
    [InlineData("legacy-invalid")]
    [InlineData("ciphertext-invalid")]
    public async Task ExistingKeyFormatsArePreservedOrFailClosedWithoutResettingEnrollment(string format)
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(value => value.Email == fixture.GetProperty("adminEmail").GetString());
            var token = await db.UserTokens.SingleAsync(value => value.UserId == user.Id && value.Name == "AuthenticatorKey");
            token.Value = format == "legacy-valid" ? TotpFixture.Secret : format == "legacy-invalid" ? "invalid-base32!" : "protected:v1:invalid-ciphertext";
            await db.SaveChangesAsync();
        }
        var started = await Start(client, fixture); Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var view = await started.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("verify", view.GetProperty("status").GetString()); Assert.Equal(JsonValueKind.Null, view.GetProperty("enrollment").ValueKind);
        var result = await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = TotpFixture.Code });
        Assert.Equal(format == "legacy-valid" ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result.StatusCode);
        using var verification = factory.Services.CreateScope(); var users = verification.ServiceProvider.GetRequiredService<UserManager<User>>();
        var current = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
        Assert.True(current.TwoFactorEnabled);
        if (format == "legacy-valid")
        {
            Assert.Equal(TotpFixture.Secret, await users.GetAuthenticatorKeyAsync(current));
            var stored = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().UserTokens.SingleAsync(value => value.UserId == current.Id && value.Name == "AuthenticatorKey");
            Assert.StartsWith("protected:v1:", stored.Value);
        }
        else { await AssertNoSessions(factory); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode); }
    }

    [Fact]
    public async Task EnrollmentReusesQrAcrossRefreshAndRequiresVerifiedCodeAndSavedRecoveryAcknowledgment()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); await Unenroll(factory, fixture); using var client = factory.CreateClient();
        var started = await Start(client, fixture); Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var body = await started.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("enroll", body.GetProperty("status").GetString());
        var enrollment = body.GetProperty("enrollment"); var secret = enrollment.GetProperty("sharedKey").GetString()!;
        var uri = enrollment.GetProperty("authenticatorUri").GetString()!;
        Assert.Contains(Uri.EscapeDataString("Amaara Creations:" + fixture.GetProperty("adminEmail").GetString()), uri);
        Assert.Contains("issuer=Amaara%20Creations", uri);
        var restored = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/sign-in"); Assert.Equal(secret, restored.GetProperty("enrollment").GetProperty("sharedKey").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/enrollment/confirm", new { code = "invalid" })).StatusCode);
        using (var scope = factory.Services.CreateScope()) Assert.False((await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!.TwoFactorEnabled);
        await AssertNoSessions(factory);
        var confirmed = await client.PostAsJsonAsync("/api/v1/auth/sign-in/enrollment/confirm", new { code = TotpFixture.For(secret, DateTimeOffset.UtcNow) }); confirmed.EnsureSuccessStatusCode();
        var codes = await confirmed.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("recovery_codes", codes.GetProperty("status").GetString()); Assert.Equal(10, codes.GetProperty("recoveryCodes").GetArrayLength()); Assert.False(codes.TryGetProperty("token", out _));
        await AssertNoSessions(factory);
        var restoredCodes = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/sign-in"); Assert.Equal(codes.GetProperty("recoveryCodes").GetRawText(), restoredCodes.GetProperty("recoveryCodes").GetRawText());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = false })).StatusCode);
        await AssertNoSessions(factory);
        (await client.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = true })).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.True(user.TwoFactorEnabled); Assert.Equal(secret, await users.GetAuthenticatorKeyAsync(user));
            var values = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().UserTokens.Where(token => token.UserId == user.Id).Select(token => token.Value).ToListAsync();
            Assert.All(values, value => Assert.DoesNotContain(secret, value!));
            Assert.All(values, value => Assert.DoesNotContain(codes.GetProperty("recoveryCodes")[0].GetString()!, value!));
        }
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("stamp")]
    [InlineData("role")]
    [InlineData("cancel")]
    [InlineData("password")]
    [InlineData("logout")]
    public async Task AbandonedChangedOrExpiredChallengesCannotComplete(string change)
    {
        var clock = new Clock(); await using var factory = new AcceptanceApiFactory { Clock = clock }; var fixture = await Fixture(factory); using var client = factory.CreateClient();
        (await Start(client, fixture)).EnsureSuccessStatusCode();
        if (change == "expired") clock.Now = clock.Now.AddMinutes(11);
        if (change == "cancel") (await client.PostAsJsonAsync("/api/v1/auth/sign-in/cancel", new { })).EnsureSuccessStatusCode();
        if (change == "logout")
        {
            var pendingCookie = (await Start(client, fixture)).Headers.GetValues("Set-Cookie").Last(value => value.StartsWith("amaara_refresh_signin=")).Split(';')[0];
            (await client.PostAsJsonAsync("/api/v1/auth/logout", new { })).EnsureSuccessStatusCode();
            using var replay = factory.CreateClient(new() { HandleCookies = false }); replay.DefaultRequestHeaders.Add("Cookie", pendingCookie);
            Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/v1/auth/sign-in")).StatusCode);
        }
        if (change is "stamp" or "role" or "password")
        {
            using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            if (change == "stamp") Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
            else if (change == "role") Assert.True((await users.RemoveFromRoleAsync(user, AppRoles.Admin)).Succeeded);
            else
            {
                var key = await users.GetAuthenticatorKeyAsync(user);
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = user.Email, token = Uri.EscapeDataString(token), newPassword = "Another long disposable test password 95!", confirmPassword = "Another long disposable test password 95!" });
                reset.EnsureSuccessStatusCode();
                await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Entry(user).ReloadAsync();
                Assert.True(user.TwoFactorEnabled); Assert.Equal(key, await users.GetAuthenticatorKeyAsync(user));
            }
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = TotpFixture.Code })).StatusCode);
        await AssertNoSessions(factory);
    }

    [Fact]
    public async Task ConcurrentChallengeConsumptionHasExactlyOneWinner()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        (await Start(client, fixture)).EnsureSuccessStatusCode(); var code = TotpFixture.Code;
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code })));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK); Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false, "unenrolled")]
    [InlineData(true, "unenrolled")]
    [InlineData(false, "missing")]
    [InlineData(true, "missing")]
    [InlineData(false, "expired")]
    [InlineData(true, "expired")]
    public async Task LegacyLimitedOrExpiredStaffRefreshCannotMintAnOrdinaryJwt(bool management, string state)
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
        if (state == "unenrolled") Assert.True((await users.SetTwoFactorEnabledAsync(user, false)).Succeeded);
        var issued = await scope.ServiceProvider.GetRequiredService<be.Application.Abstractions.Identity.IRefreshSessionService>().CreateSessionAsync(user, new("Legacy device", null));
        var assurance = state == "missing" ? null : new be.Application.Features.Identity.StaffAssurance(user.Id, user.SecurityStamp!, issued.Session.FamilyId, DateTimeOffset.UtcNow.AddMinutes(state == "expired" ? -16 : 0).ToUnixTimeSeconds());
        var response = await scope.ServiceProvider.GetRequiredService<be.Application.Features.Identity.IdentityService>().RefreshAsync(issued.RawToken, default, assurance, management);
        Assert.Null(response);
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.AnyAsync(value => value.UserId == user.Id && value.RevokedAt == null));
    }

    [Fact]
    public async Task FiveBadCodesPersistLockoutAndNoSessions()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient(); (await Start(client, fixture)).EnsureSuccessStatusCode();
        for (var attempt = 0; attempt < 5; attempt++) Assert.False((await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code = "invalid" })).IsSuccessStatusCode);
        using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Assert.True(await users.IsLockedOutAsync((await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!)); await AssertNoSessions(factory);
    }

    [Fact]
    public async Task RecoveryKeepsOldKeyUntilReplacementIsVerifiedAndThenRevokesOldSessions()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Fixture(factory); using var client = factory.CreateClient();
        string oldKey; string recovery;
        long oldSessionId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            oldKey = (await users.GetAuthenticatorKeyAsync(user))!;
            oldSessionId = (await scope.ServiceProvider.GetRequiredService<be.Application.Abstractions.Identity.IRefreshSessionService>().CreateSessionAsync(user, new("Prior device", null))).Session.Id;
            var codes = await scope.ServiceProvider.GetRequiredService<be.Application.Abstractions.Identity.ISecondFactorService>().RegenerateAsync(user, fixture.GetProperty("password").GetString()!, TotpFixture.Code, default);
            recovery = codes![0];
        }
        (await Start(client, fixture)).EnsureSuccessStatusCode();
        var repair = await client.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { recoveryCode = recovery }); repair.EnsureSuccessStatusCode();
        var body = await repair.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("enroll", body.GetProperty("status").GetString()); Assert.False(body.TryGetProperty("token", out _));
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.Equal(oldKey, await users.GetAuthenticatorKeyAsync(user)); Assert.True(user.TwoFactorEnabled);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        var key = body.GetProperty("enrollment").GetProperty("sharedKey").GetString()!; Assert.NotEqual(oldKey, key);
        (await client.PostAsJsonAsync("/api/v1/auth/sign-in/enrollment/confirm", new { code = TotpFixture.For(key, DateTimeOffset.UtcNow) })).EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope()) Assert.NotNull((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshSessions.SingleAsync(session => session.Id == oldSessionId)).RevokedAt);
        (await client.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = true })).EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
            Assert.Equal(key, await users.GetAuthenticatorKeyAsync(user));
            Assert.Contains(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AuditLogs.ToListAsync(), log => log.Action == "AuthenticatorRecoveryVerified");
        }
        using var replay = factory.CreateClient(); (await Start(replay, fixture)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await replay.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { recoveryCode = recovery })).StatusCode);
    }
}
