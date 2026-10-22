using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class SecondFactorJourneyTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task EnrollmentRequiresPasswordAndConfirmationThenRevokesExistingSessions()
    {
        var clock = new Clock();
        await using var factory = new AcceptanceApiFactory { Clock = clock };
        var (client, email, password) = await Customer(factory);
        using (client)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/second-factor/enrollment", new { password = "incorrect" })).StatusCode);
            var enrollment = await Post(client, "enrollment", new { password });
            var key = enrollment.GetProperty("sharedKey").GetString()!;
            Assert.StartsWith("otpauth://", enrollment.GetProperty("authenticatorUri").GetString());
            var confirmed = await Post(client, "confirm", new { password, authenticatorCode = TotpFixture.For(key, clock.Now) });
            Assert.Equal(10, confirmed.GetProperty("recoveryCodes").GetArrayLength());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            var pending = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.Accepted, pending.StatusCode); Assert.False((await pending.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("token", out _));
            // The setup code is consumed too, so it cannot immediately sign in.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode = TotpFixture.For(key, clock.Now) })).StatusCode);
            clock.Now = clock.Now.AddSeconds(30);
            var signedIn = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode = TotpFixture.For(key, clock.Now) });
            signedIn.EnsureSuccessStatusCode();
            var jwt = (await signedIn.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
            var status = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/second-factor");
            Assert.True(status.GetProperty("enabled").GetBoolean());
            Assert.True(signedIn.Headers.CacheControl!.NoStore);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            var values = await db.Set<IdentityUserToken<string>>().Where(x => x.UserId == user.Id && x.LoginProvider == "Amaara.SecondFactor.v1").Select(x => x.Value).ToListAsync();
            Assert.All(values, value => Assert.DoesNotContain(key, value!));
            Assert.All(values, value => Assert.DoesNotContain(confirmed.GetProperty("recoveryCodes")[0].GetString()!, value!));
        }
    }

    [Fact]
    public async Task RecoveryIsSingleUseAndConcurrentTotpReplayHasOneWinner()
    {
        var clock = new Clock();
        await using var factory = new AcceptanceApiFactory { Clock = clock };
        var (client, email, password) = await Customer(factory);
        using (client)
        {
            var key = (await Post(client, "enrollment", new { password })).GetProperty("sharedKey").GetString()!;
            var codes = (await Post(client, "confirm", new { password, authenticatorCode = TotpFixture.For(key, clock.Now) })).GetProperty("recoveryCodes");
            var recoveryCode = codes[0].GetString();
            (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, recoveryCode })).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, recoveryCode })).StatusCode);
            clock.Now = clock.Now.AddSeconds(30);
            var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode = TotpFixture.For(key, clock.Now) })));
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task ExpiredEnrollmentCannotEnableSecondFactor()
    {
        var clock = new Clock();
        await using var factory = new AcceptanceApiFactory { Clock = clock };
        var (client, _, password) = await Customer(factory);
        using (client)
        {
            var key = (await Post(client, "enrollment", new { password })).GetProperty("sharedKey").GetString()!;
            clock.Now = clock.Now.AddMinutes(11);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/second-factor/confirm", new { password, authenticatorCode = TotpFixture.For(key, clock.Now) })).StatusCode);
            Assert.False((await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/second-factor")).GetProperty("enabled").GetBoolean());
        }
    }

    [Fact]
    public async Task StaffRecoveryCannotGrantStaffCapabilitiesOrDisableMandatoryFactor()
    {
        var clock = new Clock();
        await using var factory = new AcceptanceApiFactory { Clock = clock };
        var (client, email, password) = await Customer(factory);
        using (client)
        {
            var key = (await Post(client, "enrollment", new { password })).GetProperty("sharedKey").GetString()!;
            var codes = (await Post(client, "confirm", new { password, authenticatorCode = TotpFixture.For(key, clock.Now) })).GetProperty("recoveryCodes");
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, AppRoles.Admin)).Succeeded);
            }
            var recovery = await client.PostAsJsonAsync("/api/v1/auth/staff/login", new { email, password, recoveryCode = codes[0].GetString() });
            recovery.EnsureSuccessStatusCode();
            var state = await recovery.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("enroll", state.GetProperty("status").GetString()); Assert.False(state.TryGetProperty("token", out _));
            client.DefaultRequestHeaders.Authorization = null;
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/second-factor/disable", new { password, recoveryCode = codes[1].GetString() })).StatusCode);
            using var verificationScope = factory.Services.CreateScope(); Assert.True((await verificationScope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(email))!.TwoFactorEnabled);
        }
    }

    [Fact]
    public async Task SessionListScopesToOwnerAndDeviceRevocationTakesEffectImmediately()
    {
        await using var factory = new AcceptanceApiFactory();
        var (client, email, password) = await Customer(factory);
        using (client)
        using (var otherDevice = factory.CreateClient())
        {
            var response = await otherDevice.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            response.EnsureSuccessStatusCode();
            otherDevice.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
            var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/sessions");
            Assert.Equal(2, list.GetArrayLength());
            Assert.Single(list.EnumerateArray(), x => x.GetProperty("current").GetBoolean());
            var otherSession = list.EnumerateArray().Single(x => !x.GetProperty("current").GetBoolean()).GetProperty("id").GetString();
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/auth/sessions/" + otherSession)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            var (stranger, _, _) = await Customer(factory, seed: false);
            using (stranger)
            {
                var strangerSession = (await stranger.GetFromJsonAsync<JsonElement>("/api/v1/auth/sessions"))[0].GetProperty("id").GetString();
                Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/v1/auth/sessions/" + strangerSession)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await stranger.GetAsync("/api/v1/auth/me")).StatusCode);
            }
        }
    }

    private static async Task<(HttpClient, string, string)> Customer(AcceptanceApiFactory factory, bool seed = true)
    {
        if (seed) await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        var client = factory.CreateClient();
        var email = fixture.GetProperty("customerEmail").GetString()!;
        var password = fixture.GetProperty("password").GetString()!;
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        return (client, email, password);
    }
    private static async Task<JsonElement> Post(HttpClient client, string route, object request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/second-factor/" + route, request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
