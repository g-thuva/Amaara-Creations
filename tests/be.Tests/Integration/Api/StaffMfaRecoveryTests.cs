using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class StaffMfaRecoveryTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static async Task<JsonElement> Operations(AcceptanceApiFactory factory) { await factory.SeedCatalogueAsync(); return JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync()); }
    private static async Task<string> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email, password, authenticatorCode = TotpFixture.Code }); response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!; client.DefaultRequestHeaders.Authorization = new("Bearer", token); return token;
    }

    [Fact]
    public async Task OnlyAnotherRecentlyMfaVerifiedOwnerCanApproveRecoveryAndNoSecretIsSent()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Operations(factory); var basic = fixture.GetProperty("basic"); var password = basic.GetProperty("password").GetString()!;
        var target = basic.GetProperty("adminEmail").GetString()!; var ownerEmail = fixture.GetProperty("superAdminEmail").GetString()!;
        using var customer = factory.CreateClient();
        var login = await customer.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = basic.GetProperty("customerEmail").GetString(), password }); login.EnsureSuccessStatusCode();
        customer.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        var request = new { email = target, reason = "Verified lost-phone request by second owner" };
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", request)).StatusCode);
        using var admin = factory.CreateClient(); await Login(admin, target, password);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", request)).StatusCode);
        using var owner = factory.CreateClient(); var token = await Login(owner, ownerEmail, password);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", new { email = ownerEmail, reason = request.reason })).StatusCode);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        using (var scope = factory.Services.CreateScope())
        {
            var user = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(ownerEmail))!;
            var old = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user, [AppRoles.SuperAdmin], parsed.Claims.Single(claim => claim.Type == "session_family").Value, DateTimeOffset.UtcNow.AddMinutes(-6), true);
            owner.DefaultRequestHeaders.Authorization = new("Bearer", old);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", request)).StatusCode);
        owner.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var approved = await owner.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", request); approved.EnsureSuccessStatusCode();
        var body = await approved.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(body.GetProperty("requiresEnrollment").GetBoolean()); Assert.False(body.TryGetProperty("sharedKey", out _));
        Assert.Single(factory.Email.SecurityNotifications);
    }

    [Fact]
    public async Task ApprovedRecoveryRevokesSessionsAndKeepsOldKeyUntilNewTotpAndAcknowledgment()
    {
        await using var factory = new AcceptanceApiFactory(); var fixture = await Operations(factory); var basic = fixture.GetProperty("basic"); var password = basic.GetProperty("password").GetString()!; var email = basic.GetProperty("adminEmail").GetString()!;
        using var target = factory.CreateClient(); await Login(target, email, password); string oldKey;
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); oldKey = (await users.GetAuthenticatorKeyAsync((await users.FindByEmailAsync(email))!))!; }
        using var owner = factory.CreateClient(); await Login(owner, fixture.GetProperty("superAdminEmail").GetString()!, password);
        (await owner.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", new { email, reason = "Verified account ownership through approved recovery procedure" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { })).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(email))!; Assert.True(user.TwoFactorEnabled); Assert.Equal(oldKey, await users.GetAuthenticatorKeyAsync(user));
        }
        target.DefaultRequestHeaders.Authorization = null;
        var start = await target.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password, authenticatorCode = TotpFixture.Code }); Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        var body = await start.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("enroll", body.GetProperty("status").GetString()); Assert.False(body.TryGetProperty("token", out _));
        var key = body.GetProperty("enrollment").GetProperty("sharedKey").GetString()!; Assert.NotEqual(oldKey, key);
        (await target.PostAsJsonAsync("/api/v1/auth/sign-in/enrollment/confirm", new { code = TotpFixture.For(key, DateTimeOffset.UtcNow) })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        (await target.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = true })).EnsureSuccessStatusCode();
        var refreshed = await target.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refreshed.EnsureSuccessStatusCode();
        target.DefaultRequestHeaders.Authorization = new("Bearer", (await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.OK, (await target.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        using var finalScope = factory.Services.CreateScope(); var logs = await finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AuditLogs.ToListAsync(); Assert.Contains(logs, log => log.Action == "StaffMfaRecoveryApproved"); Assert.Contains(logs, log => log.Action == "AuthenticatorReplaced");
        Assert.All(factory.Email.SecurityNotifications, notice => { Assert.DoesNotContain(key, notice.Body); Assert.DoesNotContain(oldKey, notice.Body); });
    }

    [Fact]
    public async Task ExpiredRecoveryApprovalCannotFallBackToTheOldAuthenticator()
    {
        var clock = new Clock(); await using var factory = new AcceptanceApiFactory { Clock = clock }; var fixture = await Operations(factory); var basic = fixture.GetProperty("basic"); var password = basic.GetProperty("password").GetString()!;
        using var owner = factory.CreateClient(); await Login(owner, fixture.GetProperty("superAdminEmail").GetString()!, password);
        (await owner.PostAsJsonAsync("/api/v1/admin/staff/mfa-recovery", new { email = basic.GetProperty("adminEmail").GetString(), reason = "Verified account recovery for expired approval test" })).EnsureSuccessStatusCode();
        clock.Now = clock.Now.AddMinutes(16);
        using var target = factory.CreateClient(); var response = await target.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = basic.GetProperty("adminEmail").GetString(), password, authenticatorCode = TotpFixture.Code });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("MFA_RECOVERY_APPROVAL_EXPIRED", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}
