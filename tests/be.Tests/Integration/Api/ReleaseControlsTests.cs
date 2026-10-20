using System.Net;
using System.Net.Http.Json;
using be.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class ReleaseControlsTests
{
    [Theory]
    [InlineData("/api/v1/checkout/session")]
    [InlineData("/api/v1/checkout/orders/1/retry")]
    [InlineData("/api/v1/admin/promotions")]
    public async Task CutoverPauseBlocksNewWritesWithoutCallingProviders(string path)
    {
        await using var original = new CommerceApiFactory();
        await using var paused = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<ReleaseControls>(options => { options.AcceptNewPayments = false; options.AllowAdminChanges = false; })));
        using var client = paused.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        using var response = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("300", Assert.Single(response.Headers.GetValues("Retry-After")));
        Assert.Equal(0, original.Gateway.Initializations);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task UploadPauseStillAllowsAuthenticationAndSignedCallbackVerification()
    {
        await using var original = new CommerceApiFactory();
        await using var paused = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<ReleaseControls>(options => { options.AcceptUploads = false; options.AllowAdminChanges = false; options.AcceptNewPayments = false; })));
        using var client = paused.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1, 2, 3]), "file", "test.png");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync("/api/v1/upload/avatar", content)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/payments/datatrans/webhook", new { })).StatusCode);
    }
}
