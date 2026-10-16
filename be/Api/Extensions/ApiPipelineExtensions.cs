using be.Api.Middleware;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads"));
        Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "private-media"));
        // Enable static file serving for uploaded images
        app.UseStaticFiles();
        // Enable CORS (must be before UseAuthentication and UseAuthorization)
        app.UseCors("AllowReactApp");
        // Authentication & Authorization (order matters!)
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapGet("/health", () => Results.Ok(new { status = "ok", environment = app.Environment.EnvironmentName, timestamp = DateTimeOffset.UtcNow }));
        app.MapControllers();
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/health/ready", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            try
            {
                if (!await db.Database.CanConnectAsync(ct))
                    return Results.Json(new { status = "not_ready" }, statusCode: 503);
                if (db.Database.IsRelational() && (await db.Database.GetPendingMigrationsAsync(ct)).Any())
                    return Results.Json(new { status = "not_ready" }, statusCode: 503);
                return Results.Ok(new { status = "ready" });
            }
            catch (Exception)when (!ct.IsCancellationRequested)
            {
                return Results.Json(new { status = "not_ready" }, statusCode: 503);
            }
        });
    }
}
