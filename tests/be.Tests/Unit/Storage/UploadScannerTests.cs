using System.Net;
using be.Domain.Common;
using be.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace be.Tests;
public sealed class UploadScannerTests
{
    [Theory]
    [InlineData(204, true)]
    [InlineData(200, false)]
    [InlineData(302, false)]
    [InlineData(422, false)]
    [InlineData(500, false)]
    public async Task OnlyExplicitCleanResponsesPermitStorage(int status, bool clean)
    {
        var handler = new Handler((HttpStatusCode)status);
        var scanner = new HttpUploadScanner(new HttpClient(handler), Config(true));
        if (clean) await scanner.ScanAsync([1, 2, 3], default);
        else if (status == 422) await Assert.ThrowsAsync<InvalidOperationException>(() => scanner.ScanAsync([1, 2, 3], default));
        else Assert.Equal(503, (await Assert.ThrowsAsync<CommerceException>(() => scanner.ScanAsync([1, 2, 3], default))).Status);
        Assert.Equal(new byte[] { 1, 2, 3 }, handler.Bytes);
        Assert.Equal("application/octet-stream", handler.ContentType);
    }
    [Fact]
    public async Task DisabledOptionalScannerHasNoNetworkSideEffectAndRequiredScannerFailsClosed()
    {
        var handler = new Handler(HttpStatusCode.NoContent);
        await new HttpUploadScanner(new HttpClient(handler), Config(false)).ScanAsync([1], default);
        Assert.Null(handler.Bytes);
        var required = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Scanner:Required"] = "true" }).Build();
        await Assert.ThrowsAsync<CommerceException>(() => new HttpUploadScanner(new HttpClient(handler), required).ScanAsync([1], default));
        Assert.Null(handler.Bytes);
    }
    [Fact]
    public async Task ScannerNetworkFailureFailsClosed()
    {
        await Assert.ThrowsAsync<CommerceException>(() => new HttpUploadScanner(new HttpClient(new Handler(null)), Config(true)).ScanAsync([1], default));
    }
    private static IConfiguration Config(bool enabled) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Scanner:Enabled"] = enabled.ToString(), ["Uploads:Scanner:Endpoint"] = "https://scanner.example.test/scan" }).Build();
    private sealed class Handler(HttpStatusCode? status) : HttpMessageHandler
    {
        public byte[]? Bytes;
        public string? ContentType;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Bytes = await request.Content!.ReadAsByteArrayAsync(ct); ContentType = request.Content.Headers.ContentType!.MediaType;
            if (status == null) throw new HttpRequestException("Test scanner outage");
            return new HttpResponseMessage(status.Value);
        }
    }
}
