using System.Net;
using System.Net.Http.Headers;
using be.Application.Abstractions.Storage;
using be.Domain.Common;

namespace be.Infrastructure.Storage;

// Contract for an operator-approved private scanning adapter: 204 = clean,
// 422 = rejected, every other status or network failure = unavailable. No file
// names, customer identifiers or credentials are placed in the request URL.
public sealed class HttpUploadScanner(HttpClient client, IConfiguration configuration) : IUploadScanner
{
    public async Task ScanAsync(byte[] image, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Uploads:Scanner:Enabled"))
        {
            if (configuration.GetValue<bool>("Uploads:Scanner:Required")) throw Unavailable();
            return;
        }
        if (!ValidEndpoint(configuration["Uploads:Scanner:Endpoint"], out var endpoint)) throw Unavailable();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new ByteArrayContent(image);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        if (configuration["Uploads:Scanner:ApiKey"] is { Length: > 0 } key) request.Headers.Add("X-Scan-Key", key);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.NoContent) return;
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity) throw new InvalidOperationException("The uploaded file did not pass security scanning.");
            throw Unavailable();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }
    internal static bool ValidEndpoint(string? value, out Uri? endpoint) =>
        Uri.TryCreate(value, UriKind.Absolute, out endpoint) && (endpoint.Scheme == "https" || (endpoint.Scheme == "http" && endpoint.IsLoopback)) && endpoint.UserInfo.Length == 0 && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0;
    internal static CommerceException Unavailable() => new("UPLOAD_SCAN_UNAVAILABLE", "File scanning is unavailable. Try again later; the file has not been saved.", 503);
}

internal static class UploadScanning
{
    internal static async Task Check(IUploadScanner? scanner, IConfiguration configuration, byte[] bytes, CancellationToken ct)
    {
        if (scanner != null) await scanner.ScanAsync(bytes, ct);
        else if (configuration.GetValue<bool>("Uploads:Scanner:Enabled") || configuration.GetValue<bool>("Uploads:Scanner:Required")) throw HttpUploadScanner.Unavailable();
    }
}
