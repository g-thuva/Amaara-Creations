using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace be.Api.Middleware;
public sealed class ReleaseTelemetryMiddleware(RequestDelegate next, ILogger<ReleaseTelemetryMiddleware> logger)
{
    public static readonly Meter Meter = new("Amaara.Api", "1.0");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("amaara.http.duration", "ms");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("amaara.http.server_errors");
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!context.Request.Path.StartsWithSegments("/swagger"))
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        if (context.Request.Headers.ContainsKey("Authorization") || context.Request.Path.StartsWithSegments("/api/v1/admin") || context.Request.Path.StartsWithSegments("/api/v1/auth") || context.Request.Path.StartsWithSegments("/api/v1/custom-designs") || context.Request.Path.StartsWithSegments("/api/v1/orders") || context.Request.Path.StartsWithSegments("/api/v1/payments"))
            context.Response.Headers.CacheControl = "no-store";
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var tags = new TagList
            {
                {
                    "route",
                    route
                },
                {
                    "method",
                    context.Request.Method
                },
                {
                    "status",
                    context.Response.StatusCode
                }
            };
            Duration.Record(elapsed, tags);
            if (context.Response.StatusCode >= 500)
                Failures.Add(1, tags);
            logger.LogInformation("HTTP {Method} {Route} returned {StatusCode} in {ElapsedMs} ms", context.Request.Method, route, context.Response.StatusCode, Math.Round(elapsed, 1));
        }
    }
}
