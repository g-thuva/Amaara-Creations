using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class AccountPrivacyAndRequestTests
{
    [Fact]
    public async Task ExportRequiresReauthenticationAndContainsOnlyOwnerDataWithoutAuthSecrets()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var a = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        var b = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient();
        await Login(client, a, "customerEmail");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/account/export", new { password = "wrong" })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/auth/account/export", new { password = a.GetProperty("password").GetString() });
        response.EnsureSuccessStatusCode();
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(a.GetProperty("customerEmail").GetString(), data.GetProperty("profile").GetProperty("email").GetString());
        Assert.DoesNotContain(b.GetProperty("customerEmail").GetString()!, json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(a.GetProperty("password").GetString()!, json);
    }

    [Fact]
    public async Task EmailChangesVerifyTheNewAddressAndRevokeEveryOldSession()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var first = factory.CreateClient(); using var second = factory.CreateClient();
        await Login(first, fixture, "customerEmail"); await Login(second, fixture, "customerEmail");
        var password = fixture.GetProperty("password").GetString();
        var newEmail = Guid.NewGuid().ToString("N") + "@example.test";
        var sent = await first.PostAsJsonAsync("/api/v1/auth/account/email-change", new { password, newEmail });
        sent.EnsureSuccessStatusCode();
        var token = factory.Email.EmailChangeToken(newEmail);
        Assert.NotNull(token);
        var before = await first.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal(fixture.GetProperty("customerEmail").GetString(), before.GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/v1/auth/account/email-change/confirm", new { password, newEmail, token = "tampered" })).StatusCode);
        (await first.PostAsJsonAsync("/api/v1/auth/account/email-change/confirm", new { password, newEmail, token })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        var old = await first.PostAsJsonAsync("/api/v1/auth/login", new { email = fixture.GetProperty("customerEmail").GetString(), password });
        Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
        var login = await first.PostAsJsonAsync("/api/v1/auth/login", new { email = newEmail, password }); login.EnsureSuccessStatusCode();
        first.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/v1/auth/account/email-change/confirm", new { password, newEmail, token })).StatusCode);
    }

    [Fact]
    public async Task RequestsAreOwnerScopedAndPrivacyReviewRequiresTheSensitiveSuperAdminPolicy()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var operations = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var a = operations.GetProperty("basic");
        var b = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var customer = factory.CreateClient(); using var stranger = factory.CreateClient(); using var admin = factory.CreateClient(); using var super = factory.CreateClient();
        await Login(customer, a, "customerEmail"); await Login(stranger, b, "customerEmail");
        var create = await customer.PostAsJsonAsync("/api/v1/account/requests/quotes", new { subject = "Real fixture request", details = "Review material and quantity", quantity = 250, customDesignId = operations.GetProperty("designId").GetInt32() });
        create.EnsureSuccessStatusCode();
        var quote = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = quote.GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync($"/api/v1/account/requests/{id}/cancel", new { })).StatusCode);
        Assert.Equal(0, (await stranger.GetFromJsonAsync<JsonElement>("/api/v1/account/requests")).GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync("/api/v1/account/requests/quotes", new { subject = "Other owner", details = "Invalid attachment", customDesignId = operations.GetProperty("designId").GetInt32() })).StatusCode);
        var deletion = await customer.PostAsJsonAsync("/api/v1/auth/account/deletion-review", new { password = a.GetProperty("password").GetString(), details = "Review deletion and retention", acknowledged = true });
        deletion.EnsureSuccessStatusCode();
        var privacy = await deletion.Content.ReadFromJsonAsync<JsonElement>();
        await Login(admin, a, "adminEmail", factor: true);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/customer-requests/quotes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/admin/customer-requests/privacy")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync($"/api/v1/admin/customer-requests/{privacy.GetProperty("id").GetString()}/review", new { status = "Closed", reply = "Not authorized" })).StatusCode);
        await Login(super, operations, "superAdminEmail", factor: true, password: a.GetProperty("password").GetString());
        (await super.PostAsJsonAsync($"/api/v1/admin/customer-requests/{privacy.GetProperty("id").GetString()}/review", new { status = "Responded", reply = "Retention review is required. No deletion has occurred.", rowVersion = privacy.GetProperty("rowVersion").GetString() })).EnsureSuccessStatusCode();
        var own = await customer.GetFromJsonAsync<JsonElement>("/api/v1/account/requests");
        Assert.Contains(own.GetProperty("items").EnumerateArray(), x => x.GetProperty("reply").GetString() == "Retention review is required. No deletion has occurred.");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Users.AnyAsync(x => x.Email == a.GetProperty("customerEmail").GetString()));
        Assert.True(await db.Orders.AnyAsync());
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "CustomerRequestReviewed"));
    }

    [Fact]
    public async Task UnapprovedRequestAndEmailFeaturesStayClosed()
    {
        await using var factory = new AcceptanceApiFactory { RequestsEnabled = false, EmailChangeEnabled = false };
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient(); await Login(client, fixture, "customerEmail");
        var password = fixture.GetProperty("password").GetString();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/account/requests/quotes", new { subject = "Review", details = "Request" })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/auth/account/deletion-review", new { password, details = "Review", acknowledged = true })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/auth/account/email-change", new { password, newEmail = "new@example.test" })).StatusCode);
    }

    private static async Task Login(HttpClient client, JsonElement fixture, string field, bool factor = false, string? password = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = fixture.GetProperty(field).GetString(), password = password ?? fixture.GetProperty("password").GetString(), authenticatorCode = factor ? TotpFixture.Code : null });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
    }
}
