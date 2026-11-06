using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class StoreConfigurationTests
{
    [Fact]
    public async Task SupportedOwnerSettingsReachPublicConsumersAndRejectUnsafeOrUnusedKeys()
    {
        await using var factory = new AcceptanceApiFactory(); await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        using var owner = factory.CreateClient(); using var customer = factory.CreateClient();
        var response = await owner.PostAsJsonAsync("/api/v1/auth/staff/login", new { email = fixture.GetProperty("superAdminEmail").GetString(), password = fixture.GetProperty("basic").GetProperty("password").GetString(), authenticatorCode = TotpFixture.Code });
        response.EnsureSuccessStatusCode(); owner.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        var definitions = await owner.GetFromJsonAsync<JsonElement>("/api/v1/admin/settings/catalogue");
        Assert.Contains(definitions.EnumerateArray(), item => item.GetProperty("key").GetString() == "contact.email");
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/admin/settings/catalogue")).StatusCode);
        foreach (var (key, value) in new[] { ("Jwt:Key", "unsafe-secret"), ("checkout.tax", "99"), ("contact.email", "invalid-email"), ("announcement.url", "javascript:alert(1)"), ("announcement.url", "//unsafe.test"), ("social.instagram", "https://name:password@example.test") })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/v1/admin/settings/{Uri.EscapeDataString(key)}", new { key, value, isPublic = true })).StatusCode);
        var saved = await owner.PutAsJsonAsync("/api/v1/admin/settings/contact.email", new { key = "contact.email", value = "shop@example.test", isPublic = true });
        saved.EnsureSuccessStatusCode(); var setting = await saved.Content.ReadFromJsonAsync<JsonElement>();
        var publicSettings = await customer.GetStringAsync("/api/v1/content/settings");
        Assert.Contains("shop@example.test", publicSettings);
        Assert.DoesNotContain("unsafe-secret", publicSettings);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync("/api/v1/admin/settings/contact.email", new { key = "contact.email", value = "stale@example.test", isPublic = true, expectedUpdatedAt = DateTime.UtcNow.AddDays(-1) })).StatusCode);
        var hidden = await owner.PutAsJsonAsync("/api/v1/admin/settings/contact.email", new { key = "contact.email", value = "shop@example.test", isPublic = false, expectedUpdatedAt = setting.GetProperty("updatedAt").GetDateTime() });
        hidden.EnsureSuccessStatusCode();
        Assert.DoesNotContain("shop@example.test", await customer.GetStringAsync("/api/v1/content/settings"));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.AuditLogs.CountAsync(a => a.EntityType == "SiteSetting" && a.EntityId == "contact.email"));
        Assert.DoesNotContain(await db.SiteSettings.ToListAsync(), item => item.Key.Contains("Jwt"));
    }
}
