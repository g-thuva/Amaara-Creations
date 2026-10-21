using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using be.Api.Identity;
using be.Domain.Identity;
using be.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace be.Tests;

public sealed class RedesignSecurityTests
{
    private static async Task<string> Login(HttpClient client, string email, string password, string? authenticatorCode = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task StaffNeedsVerifiedSecondFactorAndRefreshPreservesItsOriginalAssuranceTime()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient();
        var email = fixture.GetProperty("adminEmail").GetString()!;
        var password = fixture.GetProperty("password").GetString()!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", await Login(client, email, password));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", await Login(client, email, password, TotpFixture.Code));
        var originalToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization.Parameter);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        refresh.EnsureSuccessStatusCode();
        var token = (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var renewedToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(originalToken.Claims.Single(c => c.Type == "mfa_time").Value, renewedToken.Claims.Single(c => c.Type == "mfa_time").Value);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }

    [Fact]
    public async Task WrongSecondFactorAccumulatesAccountLockoutEvenWithTheCorrectPassword()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient();
        var email = fixture.GetProperty("adminEmail").GetString();
        var password = fixture.GetProperty("password").GetString();
        var wrongCode = TotpFixture.Code == "000000" ? "111111" : "000000";
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode = wrongCode })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, authenticatorCode = TotpFixture.Code })).StatusCode);
    }

    [Fact]
    public async Task PrivateProofEndpointDoesNotBypassStaffMfaThroughAnOwnerRoute()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateOperationsFixtureAsync());
        var basic = fixture.GetProperty("basic");
        using var client = factory.CreateClient();
        var email = basic.GetProperty("adminEmail").GetString()!;
        var password = basic.GetProperty("password").GetString()!;
        var designId = fixture.GetProperty("designId").GetInt32();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await Login(client, email, password, TotpFixture.Code));
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", "private-proof.png");
        var uploaded = await client.PostAsync($"/api/v1/admin/custom-designs/{designId}/proofs", form);
        uploaded.EnsureSuccessStatusCode();
        var proof = await uploaded.Content.ReadFromJsonAsync<JsonElement>();
        var url = proof.GetProperty("downloadUrl").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", await Login(client, email, password));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task LogoutImmediatelyRevokesAccessAndForgedSignatureCannotAuthenticate()
    {
        await using var factory = new AcceptanceApiFactory();
        await factory.SeedCatalogueAsync();
        var fixture = JsonSerializer.SerializeToElement(await factory.CreateBrowserFixtureAsync());
        using var client = factory.CreateClient();
        var token = await Login(client, fixture.GetProperty("customerEmail").GetString()!, fixture.GetProperty("password").GetString()!);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var parts = token.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new("Bearer", string.Join('.', parts));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/capabilities")).StatusCode);
    }

    [Fact]
    public async Task CookieAuthRejectsUntrustedOriginsIncludingSimpleCrossSitePosts()
    {
        await using var factory = new AcceptanceApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "https://attacker.example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/auth/logout", new StringContent(""))).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/auth/refresh", new StringContent(""))).StatusCode);
    }

    [Theory]
    [InlineData(false, 1, false, false)]
    [InlineData(true, 901, false, false)]
    [InlineData(true, 301, true, false)]
    [InlineData(true, 299, true, true)]
    [InlineData(true, -100, false, false)]
    public async Task StaffPolicyDefaultsClosedAndUsesBoundedAssurance(bool enabled, int age, bool sensitive, bool allowed)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Administration:Enabled"] = enabled.ToString() }).Build();
        var requirement = new StaffAuthorization(sensitive);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("amr", "mfa"), new Claim("mfa_time", DateTimeOffset.UtcNow.AddSeconds(-age).ToUnixTimeSeconds().ToString()) }, "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);
        await new StaffAuthorizationHandler(configuration).HandleAsync(context);
        Assert.Equal(allowed, context.HasSucceeded);
    }

    [Theory]
    [InlineData("short Password1!", true)]
    [InlineData("four calm words for printing", true)]
    [InlineData("passwordpassword", false)]
    [InlineData("123456789012345", false)]
    [InlineData("short", false)]
    public async Task NewPasswordRulesUseWholePasswordBlocklistWithoutComposition(string password, bool accepted)
    {
        var validator = new PassphraseValidator(new ConfigurationBuilder().Build());
        var result = await validator.ValidateAsync(null!, new User(), password);
        Assert.Equal(accepted, result.Succeeded);
    }
}
