using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace be.Tests;

public sealed class CustomerLifecycleTests
{
    [SqlCommerceFact]
    public async Task SqlMigrationConcurrentActionsAndRestartPreserveStatusSessionsReviewsAndHistory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_AdminLifecycle_" + Guid.NewGuid().ToString("N");
        await using var database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var signing = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var keys = Path.Combine(Path.GetTempPath(), "AmaaraAdminLifecycle", Guid.NewGuid().ToString("N"));
        try
        {
            await database.Database.MigrateAsync();
            JsonElement fixture; string id; string oldToken; string ownerToken;
            var expectedStatusChanges = 2;
            await using (var first = new AcceptanceApiFactory { TestSqlConnection = connection.ConnectionString, TestSigningKey = signing, TestKeysPath = keys })
            {
                await first.SeedCatalogueAsync(); fixture = JsonSerializer.SerializeToElement(await first.CreateOperationsFixtureAsync());
                var basic = fixture.GetProperty("basic"); using var staff = first.CreateClient(); using var customer = first.CreateClient();
                var login = await Login(customer, basic.GetProperty("customerEmail").GetString()!, basic.GetProperty("password").GetString()!, false);
                id = fixture.GetProperty("customerId").GetString()!; oldToken = login.GetProperty("token").GetString()!;
                var ownerLogin = await Login(staff, fixture.GetProperty("superAdminEmail").GetString()!, basic.GetProperty("password").GetString()!, true);
                ownerToken = ownerLogin.GetProperty("token").GetString()!;
                var current = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
                var version = current.GetProperty("customer").GetProperty("version").GetString();
                var results = await Task.WhenAll(new[] { "Removed", "Suspended" }.Select(status => staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new { status, reason = "Concurrent isolated SQL access check", version, acknowledged = true })));
                Assert.Single(results, result => result.StatusCode == HttpStatusCode.NoContent);
                Assert.Single(results, result => result.StatusCode == HttpStatusCode.Conflict);
                var winner = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
                if (winner.GetProperty("customer").GetProperty("accountStatus").GetString() != "Removed")
                {
                    Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new {
                        status = "Removed", reason = "Persist removed customer across the SQL API restart", acknowledged = true,
                        version = winner.GetProperty("customer").GetProperty("version").GetString()
                    })).StatusCode);
                    expectedStatusChanges++;
                }
            }
            // A fresh API instance uses the migrated database, not fixture memory.
            await using (var restarted = new AcceptanceApiFactory { TestSqlConnection = connection.ConnectionString, TestSigningKey = signing, TestKeysPath = keys })
            {
                using var customer = restarted.CreateClient(); customer.DefaultRequestHeaders.Authorization = new("Bearer", oldToken);
                Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/orders")).StatusCode);
                using var owner = restarted.CreateClient(); var basic = fixture.GetProperty("basic");
                owner.DefaultRequestHeaders.Authorization = new("Bearer", ownerToken);
                var current = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
                Assert.Equal("Removed", current.GetProperty("customer").GetProperty("accountStatus").GetString());
                var hidden = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?search={Uri.EscapeDataString(basic.GetProperty("customerEmail").GetString()!)}");
                Assert.Empty(hidden.GetProperty("customers").EnumerateArray());
                var removed = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?status=Removed&search={Uri.EscapeDataString(basic.GetProperty("customerEmail").GetString()!)}");
                Assert.True(Assert.Single(removed.GetProperty("customers").EnumerateArray()).GetProperty("canManageAccess").GetBoolean());
                Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new { status = "Active", reason = "Restore isolated SQL test customer", version = current.GetProperty("customer").GetProperty("version").GetString() })).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/orders")).StatusCode);
                await Login(customer, basic.GetProperty("customerEmail").GetString()!, basic.GetProperty("password").GetString()!, false);
                var product = basic.GetProperty("products")[1].GetInt32();
                var created = await customer.PostAsJsonAsync($"/api/v1/products/{product}/reviews", new { rating = 4, comment = "Persisted SQL review" });
                created.EnsureSuccessStatusCode(); var review = await created.Content.ReadFromJsonAsync<JsonElement>();
                var reviews = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/reviews?productId={product}");
                var row = reviews.GetProperty("reviews").EnumerateArray().Single();
                Assert.False(string.IsNullOrEmpty(row.GetProperty("rowVersion").GetString()));
                Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/v1/admin/reviews/{review.GetProperty("id").GetInt32()}/moderation", new { status = "Hidden", reason = "SQL moderation persistence check", rowVersion = row.GetProperty("rowVersion").GetString() })).StatusCode);
                var summary = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/products/{product}/reviews");
                Assert.Equal(0, summary.GetProperty("totalCount").GetInt32());
            }
            database.ChangeTracker.Clear();
            Assert.True(await database.Orders.AnyAsync(order => order.UserId == id));
            Assert.True(await database.Payments.AnyAsync(payment => payment.Order!.UserId == id && payment.SettledAmount > 0));
            Assert.Equal(expectedStatusChanges, await database.AuditLogs.CountAsync(a => a.Action == "CustomerAccountStatusChanged" && a.EntityId == id));
            Assert.True(await database.RefreshSessions.AnyAsync(session => session.UserId == id && session.RevocationReason == "customer-access-changed"));
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    private static async Task<JsonElement> Login(HttpClient client, string email, string password, bool staff)
    {
        var response = await client.PostAsJsonAsync(staff ? "/api/v1/auth/staff/login" : "/api/v1/auth/login",
            new { email, password, authenticatorCode = staff ? TotpFixture.Code : null });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", result.GetProperty("token").GetString());
        return result;
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Blocked")]
    [InlineData("PendingDeletion")]
    [InlineData("Removed")]
    public async Task AccessChangesRevokeRealSessionsAndPreserveBusinessHistory(string state)
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var basic = fixture.GetProperty("basic");
        var password = basic.GetProperty("password").GetString()!;
        var email = basic.GetProperty("customerEmail").GetString()!;
        using var customer = factory.CreateClient();
        using var staff = factory.CreateClient();
        var login = await Login(customer, email, password, false);
        var id = login.GetProperty("user").GetProperty("id").GetString()!;
        await Login(staff, state == "PendingDeletion" ? fixture.GetProperty("superAdminEmail").GetString()! : basic.GetProperty("adminEmail").GetString()!, password, true);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/orders")).StatusCode);
        var snapshot = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
        var version = snapshot.GetProperty("customer").GetProperty("version").GetString();
        var action = new { status = state, reason = "Isolated customer access regression", version, acknowledged = true };
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", action)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/v1/auth/login", new { email, password })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", WithStatus("Active"))).StatusCode);

        var filtered = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?status={state}&search={Uri.EscapeDataString(email)}");
        Assert.Equal(1, filtered.GetProperty("totalCount").GetInt32());
        var changed = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
        Assert.Equal(state, changed.GetProperty("customer").GetProperty("accountStatus").GetString());
        Assert.True(changed.GetProperty("customer").GetProperty("canManageAccess").GetBoolean());
        Assert.NotEmpty(changed.GetProperty("orders").EnumerateArray());
        Assert.NotEmpty(changed.GetProperty("reviews").EnumerateArray());
        if (state == "Removed")
        {
            var currentList = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?search={Uri.EscapeDataString(email)}");
            Assert.Equal(0, currentList.GetProperty("totalCount").GetInt32());
            var all = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?status=all&search={Uri.EscapeDataString(email)}");
            var row = Assert.Single(all.GetProperty("customers").EnumerateArray());
            Assert.Equal(id, row.GetProperty("id").GetString());
            Assert.Equal(changed.GetProperty("customer").GetProperty("version").GetString(), row.GetProperty("version").GetString());
            Assert.Empty(changed.GetProperty("removalRequests").EnumerateArray());
            Assert.Equal(HttpStatusCode.Conflict, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new {
                status = "Blocked", reason = "Cannot unarchive by blocking a removed account", version = row.GetProperty("version").GetString()
            })).StatusCode);
        }
        if (state == "PendingDeletion") Assert.Single(changed.GetProperty("removalRequests").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new {
            status = "Active", reason = "Restored after isolated regression", version = changed.GetProperty("customer").GetProperty("version").GetString()
        })).StatusCode);
        // Reactivation never restores an access token or cookie family revoked above.
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/v1/orders")).StatusCode);
        await Login(customer, email, password, false);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/v1/orders")).StatusCode);
        if (state == "PendingDeletion")
        {
            var requests = await customer.GetFromJsonAsync<JsonElement>("/api/v1/account/requests");
            Assert.DoesNotContain(action.reason, requests.GetRawText());
            Assert.Contains("account-removal review", requests.GetRawText());
        }
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Orders.AnyAsync(o => o.UserId == id));
        Assert.True(await db.Payments.AnyAsync(p => p.Order!.UserId == id && p.SettledAmount > 0));
        Assert.Equal(2, await db.AuditLogs.CountAsync(a => a.EntityId == id && a.Action == "CustomerAccountStatusChanged"));

        object WithStatus(string status) => new { status, action.reason, action.version, action.acknowledged };
    }

    [Fact]
    public async Task LeastPrivilegeRemovalConfirmationAndStaffBoundariesAreEnforced()
    {
        await using var factory = new AcceptanceApiFactory(); await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var basic = fixture.GetProperty("basic"); var password = basic.GetProperty("password").GetString()!;
        var id = fixture.GetProperty("customerId").GetString()!;
        using var staff = factory.CreateClient();
        await Login(staff, basic.GetProperty("adminEmail").GetString()!, password, true);
        var snapshot = await staff.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{id}/operations");
        var version = snapshot.GetProperty("customer").GetProperty("version").GetString();
        object Action(string status, bool acknowledged = false) => new { status, reason = "Controlled lifecycle test reason", version, acknowledged };
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("PendingDeletion", true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("Removed"))).StatusCode);
        using var support = factory.CreateClient();
        await Login(support, fixture.GetProperty("supportEmail").GetString()!, password, true);
        Assert.Equal(HttpStatusCode.OK, (await support.GetAsync($"/api/v1/admin/customers/{id}/operations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await support.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("Blocked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await support.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("Removed", true))).StatusCode);
        using var customer = factory.CreateClient();
        await Login(customer, basic.GetProperty("customerEmail").GetString()!, password, false);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("Blocked"))).StatusCode);
        using var owner = factory.CreateClient();
        await Login(owner, fixture.GetProperty("superAdminEmail").GetString()!, password, true);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", Action("PendingDeletion"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/v1/admin/customers/{id}/status", new { status = "Blocked", reason = " ", version })).StatusCode);
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var admin = (await manager.FindByEmailAsync(basic.GetProperty("adminEmail").GetString()!))!;
        Assert.True((await manager.AddToRoleAsync(admin, AppRoles.Customer)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/api/v1/admin/customers/{admin.Id}/status", new { status = "Blocked", reason = "Do not use customer controls for staff", version = admin.ConcurrencyStamp })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/api/v1/admin/customers/{admin.Id}/status", new { status = "Removed", reason = "Do not use customer removal for staff", version = admin.ConcurrencyStamp, acknowledged = true })).StatusCode);
        var staffDetail = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers/{admin.Id}/operations");
        Assert.False(staffDetail.GetProperty("customer").GetProperty("canManageAccess").GetBoolean());
        var staffList = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/admin/customers?search={Uri.EscapeDataString(admin.Email!)}");
        Assert.False(Assert.Single(staffList.GetProperty("customers").EnumerateArray()).GetProperty("canManageAccess").GetBoolean());
    }
}
