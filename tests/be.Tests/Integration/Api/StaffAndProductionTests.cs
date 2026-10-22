using be.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class StaffAndProductionTests
{
    private static async Task Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(email.StartsWith("acceptance-customer-", StringComparison.OrdinalIgnoreCase) ? "/api/v1/auth/login" : "/api/v1/auth/staff/login", new { email, password, authenticatorCode = email.StartsWith("acceptance-customer-", StringComparison.OrdinalIgnoreCase) ? null : TotpFixture.Code });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.GetProperty("token").GetString());
    }

    [Fact]
    public async Task DemotionTakesEffectForPreviouslyIssuedStaffJwtAndRevokesRefreshSessions()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var basic = fixture.GetProperty("basic");
        var password = basic.GetProperty("password").GetString()!;
        using var super = factory.CreateClient();
        using var marketing = factory.CreateClient();
        await Login(super, fixture.GetProperty("superAdminEmail").GetString()!, password);
        var email = fixture.GetProperty("marketingEmail").GetString()!;
        await Login(marketing, email, password);
        Assert.Equal(HttpStatusCode.OK, (await marketing.GetAsync("/api/v1/admin/promotions")).StatusCode);
        var demote = await super.PutAsJsonAsync("/api/v1/admin/staff", new { email, roles = Array.Empty<string>(), reason = "Deterministic demotion fixture" });
        demote.EnsureSuccessStatusCode();
        // Demotion revokes the session family, so its access token is now unauthenticated.
        Assert.Equal(HttpStatusCode.Unauthorized, (await marketing.GetAsync("/api/v1/admin/promotions")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        Assert.False(await db.RefreshSessions.AnyAsync(s => s.UserId == userId && s.RevokedAt == null));
        Assert.Single(await db.AuditLogs.Where(a => a.Action == "StaffRolesChanged").ToListAsync());
        var self = await super.PutAsJsonAsync("/api/v1/admin/staff", new { email = fixture.GetProperty("superAdminEmail").GetString(), roles = Array.Empty<string>(), reason = "Invalid self demotion fixture" });
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
    }

    [Fact]
    public async Task ProofRevisionApprovalGatesProductionAndEveryTransitionIsAudited()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var basic = fixture.GetProperty("basic");
        var password = basic.GetProperty("password").GetString()!;
        var designId = fixture.GetProperty("designId").GetInt32();
        using var admin = factory.CreateClient();
        using var customer = factory.CreateClient();
        await Login(admin, basic.GetProperty("adminEmail").GetString()!, password);
        await Login(customer, basic.GetProperty("customerEmail").GetString()!, password);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/admin/custom-designs/{designId}/production-status", new { status = "Queued", note = "Should be blocked before proof" })).StatusCode);
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", "test-proof.png");
        form.Add(new StringContent("Test-only proof for approval"), "customerVisibleMessage");
        var proofResponse = await admin.PostAsync($"/api/v1/admin/custom-designs/{designId}/proofs", form);
        proofResponse.EnsureSuccessStatusCode();
        var proof = await proofResponse.Content.ReadFromJsonAsync<JsonElement>();
        var revision = proof.GetProperty("revisionNumber").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync($"/api/v1/custom-designs/{designId}/proofs/approve", new { revisionNumber = revision + 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync($"/api/v1/custom-designs/{designId}/proofs/approve", new { revisionNumber = revision })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/admin/custom-designs/{designId}/production-status", new { status = "Ready", note = "Invalid shortcut fixture" })).StatusCode);
        foreach (var status in new[]
        {
            "Queued",
            "Printing",
            "Finishing",
            "QualityCheck",
            "Ready"
        }

        )
            Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/custom-designs/{designId}/production-status", new { status, note = "Test production step" })).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(5, await db.AuditLogs.CountAsync(a => a.Action == "CustomProductionStatusChanged"));
        Assert.Single(await db.AuditLogs.Where(a => a.Action == "ProofApprovedByCustomer").ToListAsync());
        var orderId = basic.GetProperty("orderId").GetInt32();
        foreach (var status in new[]
        {
            "Packed",
            "Shipped",
            "Delivered"
        }

        )
        {
            var shipped = await admin.PutAsJsonAsync($"/api/v1/admin/shipping/orders/{orderId}/shipment", new { status, courier = status == "Delivered" ? null : "TEST LOCAL CARRIER", trackingNumber = status == "Delivered" ? null : "TEST-0001", trackingUrl = status == "Delivered" ? null : "https://tracking.example.test/TEST-0001", notes = "Fixture event" });
            shipped.EnsureSuccessStatusCode();
        }

        var order = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}");
        Assert.Equal("Completed", order.GetProperty("status").GetString());
        Assert.Equal("TEST-0001", order.GetProperty("shipments")[0].GetProperty("trackingNumber").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v1/admin/shipping/orders/{orderId}/shipment", new { status = "Delivered", trackingNumber = "TEST-CORRECTED" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/shipping/orders/{orderId}/shipment", new { status = "Delivered", trackingNumber = "TEST-CORRECTED", notes = "TEST tracking correction" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/v1/admin/custom-designs/{designId}/archive", new { status = "Archived", note = "Fulfilled test job retained", rowVersion = "" })).StatusCode);
        var queue = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/custom-designs?designStatus=Archived");
        Assert.Equal(designId, queue.GetProperty("items")[0].GetProperty("id").GetInt32());
    }
}
