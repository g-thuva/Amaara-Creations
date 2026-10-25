using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using be.Domain.CustomDesigns;
using be.Infrastructure.Persistence;
using be.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;
public sealed class S3MediaApiTests
{
    private static MultipartFormDataContent Image()
    {
        var form = new MultipartFormDataContent(); var image = new ByteArrayContent(S3MediaStorageTests.Png);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png"); form.Add(image, "file", "safe.png"); return form;
    }
    private static async Task Login(HttpClient client, string email, string password, bool staff = false)
    {
        var response = await client.PostAsJsonAsync(staff ? "/api/v1/auth/staff/login" : "/api/v1/auth/login", new { email, password, authenticatorCode = staff ? TotpFixture.Code : null }); response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new("Bearer", json.GetProperty("token").GetString());
    }
    [Fact]
    public async Task RealApiKeepsOwnershipStaffAssuranceProfileAndProofFlowsWithMockedS3()
    {
        using var s3 = new S3MediaStorageTests.FakeS3();
        await using var factory = new AcceptanceApiFactory
        {
            ExtraConfiguration = new Dictionary<string, string?> { ["Storage:Provider"] = "S3", ["Storage:S3:PublicBucket"] = "amaara-test-public", ["Storage:S3:PrivateBucket"] = "amaara-test-private" },
            ExtraServices = services => { services.RemoveAll<IAmazonS3>(); services.AddSingleton<IAmazonS3>(s3); }
        };
        await factory.SeedCatalogueAsync(); var operations = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync()); var basic = operations.GetProperty("basic"); var password = basic.GetProperty("password").GetString()!;
        var second = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var customer = factory.CreateClient(); using var other = factory.CreateClient(); using var admin = factory.CreateClient(); using var marketing = factory.CreateClient(); using var anonymous = factory.CreateClient();
        await Login(customer, basic.GetProperty("customerEmail").GetString()!, password);
        await Login(other, second.GetProperty("customerEmail").GetString()!, second.GetProperty("password").GetString()!);
        await Login(admin, basic.GetProperty("adminEmail").GetString()!, password, true);
        await Login(marketing, operations.GetProperty("marketingEmail").GetString()!, password, true);
        int designId; int productId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var email = basic.GetProperty("customerEmail").GetString(); var user = await db.Users.SingleAsync(user => user.Email == email);
            productId = await db.Products.Select(product => product.Id).FirstAsync();
            var design = new CustomDesign { UserId = user.Id, Name = "S3 ownership fixture", Width = 10, Height = 10 }; db.CustomDesigns.Add(design); await db.SaveChangesAsync(); designId = design.Id;
        }
        using var artwork = Image(); var uploaded = await customer.PostAsync($"/api/v1/custom-designs/{designId}/artwork", artwork); uploaded.EnsureSuccessStatusCode(); var asset = await uploaded.Content.ReadFromJsonAsync<JsonElement>(); var url = asset.GetProperty("downloadUrl").GetString()!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        var reads = s3.Reads; Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode); Assert.Equal(reads, s3.Reads);
        Assert.Equal(HttpStatusCode.NotFound, (await marketing.GetAsync(url)).StatusCode);
        var own = await customer.GetAsync(url); own.EnsureSuccessStatusCode(); Assert.True(own.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
        using var forbiddenProof = Image(); Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsync($"/api/v1/admin/custom-designs/{designId}/proofs", forbiddenProof)).StatusCode);
        using var proof = Image(); var proofResponse = await admin.PostAsync($"/api/v1/admin/custom-designs/{operations.GetProperty("designId").GetInt32()}/proofs", proof); proofResponse.EnsureSuccessStatusCode();
        var proofJson = await proofResponse.Content.ReadFromJsonAsync<JsonElement>(); var proofUrl = proofJson.GetProperty("downloadUrl").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync(proofUrl)).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(proofUrl)).StatusCode);
        using var avatar = Image(); var avatarResponse = await customer.PostAsync("/api/v1/upload/avatar", avatar); avatarResponse.EnsureSuccessStatusCode(); var avatarJson = await avatarResponse.Content.ReadFromJsonAsync<JsonElement>(); var avatarUrl = avatarJson.GetProperty("fileUrl").GetString()!;
        (await customer.PostAsJsonAsync("/api/v1/users/profile/avatar", new { avatarUrl })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(avatarUrl)).StatusCode);
        var profile = await customer.GetFromJsonAsync<JsonElement>("/api/v1/users/profile"); Assert.Equal(avatarUrl, profile.GetProperty("avatarUrl").GetString());
        using var galleryImage = Image();
        var galleryResponse = await admin.PostAsync($"/api/v1/admin/products/{productId}/media", galleryImage); galleryResponse.EnsureSuccessStatusCode();
        var gallery = await galleryResponse.Content.ReadFromJsonAsync<JsonElement>(); var productUrl = gallery.GetProperty("url").GetString()!;
        Assert.Equal("s3", gallery.GetProperty("storageProvider").GetString());
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(productUrl)).StatusCode);
        Assert.Contains(productUrl, await anonymous.GetStringAsync($"/api/v1/products/{productId}"));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var item in await db.MediaCleanupWork.ToListAsync()) item.NotBeforeUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync(); var count = s3.Objects.Count;
            await new MediaCleanupWorker(scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<MediaCleanupWorker>.Instance).ProcessAsync(scope.ServiceProvider, default);
            Assert.Equal(count, s3.Objects.Count); Assert.Empty(await db.MediaCleanupWork.ToListAsync());
        }
    }
}
