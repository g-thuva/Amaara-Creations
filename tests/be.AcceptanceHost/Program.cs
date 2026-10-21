using be.Tests;

Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_BE", AppContext.BaseDirectory);
if (args.Contains("--smtp"))
{
    var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    Environment.ExitCode = await ExternalSmtpAcceptance.RunAsync(repository, args.Contains("--outbox-only"));
    return;
}
await using var factory = new AcceptanceApiFactory();
using var api = factory.CreateClient(new() { HandleCookies = false, AllowAutoRedirect = false });
await factory.SeedCatalogueAsync();
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5198");
var app = builder.Build();
app.MapGet("/__fixture/health", () => Results.Ok(new { testOnly = true }));
app.MapGet("/__fixture/authenticator-code", () => Results.Ok(new { code = TotpFixture.Code }));
app.MapPost("/__fixture/new", () => factory.CreateBrowserFixtureAsync());
app.MapPost("/__fixture/operations", () => factory.CreateOperationsFixtureAsync());
app.MapPost("/__fixture/refund", (RefundFixtureRequest request) => factory.CreateBrowserFixtureAsync(request.Outcome));
app.MapPost("/__fixture/confirmation", (ConfirmationRequest request) =>
    factory.Email.Confirmation(request.Email) is { } url ? Results.Ok(new { url }) : Results.NotFound());
app.MapPost("/__fixture/email-change", (ConfirmationRequest request) =>
    factory.Email.EmailChangeToken(request.Email) is { } token ? Results.Ok(new { token }) : Results.NotFound());
app.MapGet("/__fixture/image/{name}", () => Results.Bytes(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="), "image/png"));
app.Map("/{**path}", async context =>
{
    // This loopback bridge forwards real requests into WebApplicationFactory's actual app.
    // It is a separate executable; these fixture routes cannot appear in be's deployment.
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
    request.Content = new StreamContent(context.Request.Body);
    foreach (var header in context.Request.Headers)
        if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) && !request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    using var response = await api.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers.Concat(response.Content.Headers)) context.Response.Headers[header.Key] = header.Value.ToArray();
    context.Response.Headers.Remove("transfer-encoding");
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});
await app.RunAsync();
internal sealed record ConfirmationRequest(string Email);
internal sealed record RefundFixtureRequest(string Outcome);
