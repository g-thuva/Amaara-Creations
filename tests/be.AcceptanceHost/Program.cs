using be.Tests;

Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_BE", AppContext.BaseDirectory);
if (args.Contains("--smtp"))
{
    var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    Environment.ExitCode = await ExternalSmtpAcceptance.RunAsync(repository, args.Contains("--outbox-only"));
    return;
}
var storefrontOrigin = Environment.GetEnvironmentVariable("AMAARA_E2E_STOREFRONT_ORIGIN") ?? "http://127.0.0.1:5178";
var managementOrigin = Environment.GetEnvironmentVariable("AMAARA_E2E_MANAGEMENT_ORIGIN") ?? "http://127.0.0.1:5179";
await using var factory = new AcceptanceApiFactory
{
    ExtraConfiguration = new Dictionary<string, string?>
    {
        ["Cors:AllowedOrigins:0"] = storefrontOrigin,
        ["Cors:AllowedOrigins:1"] = managementOrigin,
        ["Administration:AllowedOrigins:0"] = managementOrigin
    }
};
using var api = factory.CreateClient(new() { HandleCookies = false, AllowAutoRedirect = false });
await factory.SeedCatalogueAsync();
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5198");
var app = builder.Build();
app.MapGet("/__fixture/health", () => Results.Ok(new { testOnly = true }));
app.MapGet("/__fixture/authenticator-code", () => Results.Ok(new { code = TotpFixture.Code }));
app.MapPost("/__fixture/new", () => factory.CreateBrowserFixtureAsync());
// Session time/revocation controls exist only in this isolated test executable.
app.MapPost("/__fixture/session", async (SessionFixtureRequest request) =>
{
    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<be.Infrastructure.Persistence.ApplicationDbContext>();
    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Users, user => user.Email == request.Email);
    var sessions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.RefreshSessions.Where(session => session.UserId == user.Id && session.Management == request.Management && session.RevokedAt == null));
    if (request.Action == "expire-access")
    {
        var session = sessions.Single();
        var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<be.Domain.Identity.User>>();
        var tokens = scope.ServiceProvider.GetRequiredService<be.Application.Abstractions.Identity.ITokenService>();
        var token = tokens.GenerateToken(user, await manager.GetRolesAsync(user), session.FamilyId, session.MfaVerifiedAt, session.Management);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        var key = scope.ServiceProvider.GetRequiredService<IConfiguration>()["Jwt:Key"]!;
        var expired = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(parsed.Issuer, parsed.Audiences.Single(), parsed.Claims.Where(claim => claim.Type is not ("exp" or "aud" or "iss" or "nbf" or "iat")), expires: DateTime.UtcNow.AddMinutes(-2), signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        return Results.Ok(new { token = handler.WriteToken(expired) });
    }
    foreach (var session in sessions)
    {
        if (request.Action == "expire-idle") session.IdleExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        else if (request.Action == "expire-absolute") session.AbsoluteExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        else if (request.Action == "revoke") session.RevokedAt = DateTime.UtcNow;
        else return Results.BadRequest();
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { changed = sessions.Count });
});
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

internal sealed record SessionFixtureRequest(string Email, string Action, bool Management = false);
