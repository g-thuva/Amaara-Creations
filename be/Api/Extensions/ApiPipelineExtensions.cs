using be.Api.Middleware;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using be.Infrastructure.Storage;

namespace be.Api.Extensions;
public static class ApiPipelineExtensions
{
    public static void UseAmaaraPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ReleaseTelemetryMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<CommerceExceptionMiddleware>();
        app.UseStatusCodePages();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
            app.UseHsts();
        app.UseHttpsRedirection();
        var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var storage = StorageOptions.Read(app.Configuration);
        var localReads = storage.Provider == "Local" || storage.LegacyLocalReadsEnabled;
        if (localReads) Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
        // Upload storage and static reads must use the same configured root,
        // including hosts whose initial provider contains build-time web assets.
        if (localReads)
        {
            var provider = new PhysicalFileProvider(webRoot);
            app.Environment.WebRootPath = webRoot;
            app.Environment.WebRootFileProvider = provider;
            app.Lifetime.ApplicationStopped.Register(provider.Dispose);
        }
        if (storage.Provider == "Local") Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "private-media"));
        // Enable static file serving for uploaded images
        if (localReads) app.UseStaticFiles();
        // Enable CORS (must be before UseAuthentication and UseAuthorization)
        app.UseCors("AllowReactApp");
        app.UseMiddleware<AuthOriginMiddleware>();
        // Authentication & Authorization (order matters!)
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseMiddleware<ReleaseControlsMiddleware>();
        app.MapGet("/health", () => Results.Ok(new { status = "ok", environment = app.Environment.EnvironmentName, timestamp = DateTimeOffset.UtcNow }));
        app.MapControllers();
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/health/ready", async (ApplicationDbContext db, be.Application.Abstractions.Storage.IMediaStorageService media, CancellationToken ct) =>
        {
            try
            {
                if (!await db.Database.CanConnectAsync(ct))
                    return Results.Json(new { status = "not_ready" }, statusCode: 503);
                if (db.Database.IsRelational() && (await db.Database.GetPendingMigrationsAsync(ct)).Any())
                    return Results.Json(new { status = "not_ready" }, statusCode: 503);
                if (storage.Provider == "S3")
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    foreach (var reference in new[] { storage.S3.ReadinessPublicKey, storage.S3.ReadinessPrivateKey }.Where(reference => reference.Length > 0))
                        if (!await media.ExistsAsync(reference, timeout.Token)) return Results.Json(new { status = "not_ready" }, statusCode: 503);
                }
                return Results.Ok(new { status = "ready" });
            }
            catch (Exception)when (!ct.IsCancellationRequested)
            {
                return Results.Json(new { status = "not_ready" }, statusCode: 503);
            }
        });
    }
}
