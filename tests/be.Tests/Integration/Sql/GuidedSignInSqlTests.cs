using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using be.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using be.Domain.Identity;
using be.Tests.Architecture;

namespace be.Tests;

public sealed class GuidedSignInSqlTests
{
    private static SqlConnectionStringBuilder Connection()
    {
        var value = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", value.InitialCatalog);
        value.InitialCatalog = "AmaaraPhase6Tests_Guided_" + Guid.NewGuid().ToString("N")[..10];
        return value;
    }
    private static ApplicationDbContext Database(string connection) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    private static AcceptanceApiFactory Factory(string connection, string keys, string signing) => new() { TestSqlConnection = connection, TestKeysPath = keys, TestSigningKey = signing };
    private static string Ticket(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie").Last(cookie => cookie.StartsWith("amaara_refresh_signin=")).Split(';')[0];
    private static void SetCookie(HttpClient client, string cookie) { client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", cookie); }

    [SqlCommerceFact]
    public async Task PendingEnrollmentAndProtectedKeySurviveApiRestartsWithoutAnotherPasswordOrCode()
    {
        var connection = Connection(); var keys = Path.Combine(RepositoryRoot.Path, ".runtime", "guided-mfa-keys-" + Guid.NewGuid().ToString("N")); var signing = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var database = Database(connection.ConnectionString);
        try
        {
            await database.Database.MigrateAsync();
            JsonElement fixture; string ticket; string secret;
            await using (var first = Factory(connection.ConnectionString, keys, signing))
            {
                await first.SeedCatalogueAsync(); fixture = JsonSerializer.SerializeToElement(await first.CreateBrowserFixtureAsync());
                using (var scope = first.Services.CreateScope())
                {
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); Assert.True((await users.SetTwoFactorEnabledAsync((await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!, false)).Succeeded);
                }
                using var client = first.CreateClient();
                var started = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() });
                Assert.Equal(HttpStatusCode.Accepted, started.StatusCode); ticket = Ticket(started);
                secret = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enrollment").GetProperty("sharedKey").GetString()!;
                Assert.False(await database.RefreshSessions.AnyAsync());
            }
            string staffCookies;
            await using (var second = Factory(connection.ConnectionString, keys, signing))
            {
                using var client = second.CreateClient(new() { HandleCookies = false }); SetCookie(client, ticket);
                var restored = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/sign-in"); Assert.Equal(secret, restored.GetProperty("enrollment").GetProperty("sharedKey").GetString());
                var confirmation = await client.PostAsJsonAsync("/api/v1/auth/sign-in/enrollment/confirm", new { code = TotpFixture.For(secret, DateTimeOffset.UtcNow) }); confirmation.EnsureSuccessStatusCode();
                Assert.Equal("recovery_codes", (await confirmation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString()); SetCookie(client, Ticket(confirmation));
                Assert.False(await database.RefreshSessions.AnyAsync());
                var completed = await client.PostAsJsonAsync("/api/v1/auth/sign-in/complete", new { savedRecoveryCodes = true }); completed.EnsureSuccessStatusCode();
                staffCookies = string.Join("; ", completed.Headers.GetValues("Set-Cookie").Where(cookie => cookie.StartsWith("amaara_refresh_staff=") || cookie.StartsWith("amaara_refresh_staff_assurance=")).Select(cookie => cookie.Split(';')[0]));
            }
            await using (var third = Factory(connection.ConnectionString, keys, signing))
            {
                using var client = third.CreateClient(new() { HandleCookies = false }); SetCookie(client, staffCookies);
                var refresh = await client.PostAsJsonAsync("/api/v1/auth/staff/refresh", new { }); refresh.EnsureSuccessStatusCode();
                client.DefaultRequestHeaders.Authorization = new("Bearer", (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
                Assert.Equal("authorized", (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/staff/session")).GetProperty("status").GetString());
                using var scope = third.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var user = (await users.FindByEmailAsync(fixture.GetProperty("adminEmail").GetString()!))!;
                Assert.True(user.TwoFactorEnabled); Assert.Equal(secret, await users.GetAuthenticatorKeyAsync(user)); Assert.Contains(AppRoles.Admin, await users.GetRolesAsync(user));
                var stored = await database.UserTokens.Where(value => value.UserId == user.Id && value.Name == "AuthenticatorKey").Select(value => value.Value).SingleAsync(); Assert.StartsWith("protected:v1:", stored); Assert.DoesNotContain(secret, stored!);
                Assert.NotEmpty(Directory.GetFiles(keys, "*.xml"));
            }
        }
        finally
        {
            Assert.StartsWith("AmaaraPhase6Tests_Guided_", database.Database.GetDbConnection().Database);
            await database.Database.EnsureDeletedAsync();
        }
    }

    [SqlCommerceFact]
    public async Task TwoApiInstancesConsumeOnePendingChallengeExactlyOnce()
    {
        var connection = Connection(); var keys = Path.Combine(RepositoryRoot.Path, ".runtime", "guided-mfa-keys-" + Guid.NewGuid().ToString("N")); var signing = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var database = Database(connection.ConnectionString);
        try
        {
            await database.Database.MigrateAsync();
            await using var first = Factory(connection.ConnectionString, keys, signing); await first.SeedCatalogueAsync();
            var fixture = JsonSerializer.SerializeToElement(await first.CreateBrowserFixtureAsync());
            using var one = first.CreateClient(new() { HandleCookies = false });
            var started = await one.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = fixture.GetProperty("adminEmail").GetString(), password = fixture.GetProperty("password").GetString() }); Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
            var ticket = Ticket(started); SetCookie(one, ticket);
            await using var second = Factory(connection.ConnectionString, keys, signing); using var two = second.CreateClient(new() { HandleCookies = false }); SetCookie(two, ticket);
            var code = TotpFixture.Code;
            var responses = await Task.WhenAll(one.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code }), two.PostAsJsonAsync("/api/v1/auth/sign-in/verify", new { code }));
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK); Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            Assert.Equal(2, await database.RefreshSessions.CountAsync());
        }
        finally
        {
            Assert.StartsWith("AmaaraPhase6Tests_Guided_", database.Database.GetDbConnection().Database);
            await database.Database.EnsureDeletedAsync();
        }
    }
}
