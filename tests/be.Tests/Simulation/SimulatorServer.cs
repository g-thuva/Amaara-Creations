using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Application.Features.Payments;
using be.Domain.Common;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace be.Tests.Simulation;

public static class SimulatorServer
{
    private const string Cookie = "amaara_local_payment_lab";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static async Task RunAsync(string[] args)
    {
        SimulatorSafety.RequireDevelopment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), Environment.GetEnvironmentVariable("AMAARA_LOCAL_PAYMENT_SIMULATOR"), args.Contains("--local-payments"));
        if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is { Length: > 0 } other && other != "Development") throw new InvalidOperationException("Conflicting simulator environment.");
        string Option(string name, string fallback) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback; }
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Simulator requires Development.");
        builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 5208));
        await using var lab = await SimulatorLab.CreateAsync(Option("--sql-server", "localhost"), Option("--currency", "LKR"), Option("--state-root", Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "local-payments")));
        await using var app = builder.Build();
        var capability = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        app.Use(async (context, next) =>
        {
            if (!SimulatorSafety.LocalRequest(app.Environment.EnvironmentName, context.Connection.RemoteIpAddress, context.Request.Host.Value)) { context.Response.StatusCode = 404; return; }
            context.Response.Headers.CacheControl = "no-store"; context.Response.Headers["X-Content-Type-Options"] = "nosniff"; context.Response.Headers["X-Frame-Options"] = "DENY";
            if (context.Request.Path == "/simulator/health") { await next(); return; }
            // Browsers omit cookies from CORS preflight. The inner API still
            // validates its exact origin policy; actual requests require the run cookie.
            if (HttpMethods.IsOptions(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/v1") && new[] { "http://127.0.0.1:5188", "http://127.0.0.1:5189" }.Contains(context.Request.Headers.Origin.ToString(), StringComparer.Ordinal)) { await next(); return; }
            if (context.Request.Path == "/simulator" && HttpMethods.IsGet(context.Request.Method))
            {
                if (context.Request.Headers["Sec-Fetch-Site"] == "cross-site") { context.Response.StatusCode = 403; return; }
                context.Response.Cookies.Append(Cookie, capability, new() { HttpOnly = true, SameSite = SameSiteMode.Strict, Path = "/", IsEssential = true });
                await next(); return;
            }
            if (!context.Request.Cookies.TryGetValue(Cookie, out var value) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(capability))) { context.Response.StatusCode = 403; return; }
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
            {
                var origin = context.Request.Headers.Origin.ToString();
                var allowed = context.Request.Path.StartsWithSegments("/api/v1") ? new[] { SimulatorSafety.Origin, "http://127.0.0.1:5188", "http://127.0.0.1:5189" } : new[] { SimulatorSafety.Origin };
                if (!allowed.Contains(origin, StringComparer.Ordinal)) { context.Response.StatusCode = 403; return; }
            }
            try { await next(); }
            catch (CommerceException error) { context.Response.StatusCode = error.Status; await context.Response.WriteAsJsonAsync(new { code = error.Code, message = error.Message }); }
            catch (JsonException) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { message = "Invalid simulator request." }); }
            catch (Exception error)
            {
                // Do not let Development's exception page expose request cookies.
                Console.Error.WriteLine("Simulator request failed: " + error.GetType().Name);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { message = "The local simulation could not complete. Check the local lab log and retry; a repeated admin sign-in may need the next 30-second authenticator period." });
            }
        });
        app.MapGet("/simulator/health", () => Results.Ok(new { simulated = true, database = lab.Database, currency = lab.Currency, ready = true }));
        app.MapGet("/simulator", () => Html("index.html"));
        app.MapGet("/simulator/pay/{id}", (string id) => Html("index.html"));
        app.MapGet("/simulator/lab.js", () => Asset("lab.js", "text/javascript"));
        app.MapGet("/simulator/lab.css", () => Asset("lab.css", "text/css"));
        app.MapGet("/simulator/api/inspect", (CancellationToken ct) => lab.InspectAsync(ct));
        app.MapPost("/simulator/session/{kind}", async (string kind, HttpContext context, CancellationToken ct) =>
        {
            if (kind is not ("customer" or "admin")) return Results.BadRequest();
            using var response = await lab.SignInAsync(kind == "admin", ct);
            if (!response.IsSuccessStatusCode) return Results.Json(new { message = "Synthetic sign-in could not complete. If you just opened admin, wait for the next 30-second code and try again." }, statusCode: (int)response.StatusCode);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies)) foreach (var cookie in cookies) context.Response.Headers.Append("Set-Cookie", cookie);
            return Results.Ok(new { url = kind == "admin" ? SimulatorSafety.Management + "/login" : SimulatorSafety.Storefront + "/products" });
        });
        app.MapPost("/simulator/api/events", async (HttpContext context, CancellationToken ct) =>
        {
            var input = await Read<EventInput>(context, ct);
            await lab.EventAsync(input.PaymentId, input.Outcome, input.EventId, input.Delivery == "webhook" ? true : input.Delivery == "reconciliation" ? false : throw new CommerceException("SIMULATOR_DELIVERY_INVALID", "Choose webhook or reconciliation."), ct);
            return Results.Ok(await lab.InspectAsync(ct));
        });
        app.MapPost("/simulator/api/payments/{id:long}/reconcile", async (long id, CancellationToken ct) =>
        {
            await RequirePayment(lab, id, ct);
            using var response = await lab.AdminActionAsync($"/api/v1/admin/payments/{id}/reconcile", new { }, ct);
            return Results.Content(await response.Content.ReadAsStringAsync(ct), "application/json", statusCode: (int)response.StatusCode);
        });
        app.MapPost("/simulator/api/payments/{id:long}/refund", async (long id, HttpContext context, CancellationToken ct) =>
        {
            await RequirePayment(lab, id, ct); var input = await Read<RefundInput>(context, ct);
            var state = SimulatorLab.Outcome(input.Outcome);
            if (state == GatewayState.Cancelled || input.IdempotencyKey == Guid.Empty) return Results.BadRequest(new { message = "Choose success, pending or decline and a refund attempt identifier." });
            if (lab.Gateway.RefundOutcomes.TryGetValue(input.IdempotencyKey, out var previous) && previous != state) return Results.Conflict(new { message = "This refund identifier already has another outcome." });
            lab.Gateway.RefundOutcomes[input.IdempotencyKey] = state;
            using var response = await lab.AdminActionAsync($"/api/v1/admin/payments/{id}/refunds", new RefundRequest(input.Amount, "SIMULATED developer refund", input.IdempotencyKey), ct);
            return Results.Content(await response.Content.ReadAsStringAsync(ct), "application/json", statusCode: (int)response.StatusCode);
        });
        app.MapPost("/simulator/api/refunds/{id:long}/complete", async (long id, CancellationToken ct) =>
        {
            using var scope = lab.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var refund = await db.Refunds.Include(r => r.Payment).SingleOrDefaultAsync(r => r.Id == id && r.Payment!.Provider == SimulatorSafety.Provider && r.Payment.ProviderMode == SimulatorSafety.Mode, ct) ?? throw new CommerceException("SIMULATOR_REFUND_NOT_FOUND", "Simulated refund not found.", 404);
            if (refund.ProviderTransactionId == null) throw new CommerceException("SIMULATOR_REFUND_UNCERTAIN", "No known simulated refund transaction.");
            var status = (await lab.Gateway.StatusAsync(refund.ProviderTransactionId, ct)) with { State = GatewayState.Paid, ProviderStatus = "SIMULATED_success" };
            lab.Gateway.SetStatus(status); var payload = lab.Parser.Payload(Guid.NewGuid(), status);
            await scope.ServiceProvider.GetRequiredService<be.Application.Features.Payments.PaymentWebhookService>().ProcessAsync(SimulatorSafety.Provider, payload, lab.Parser.Sign(payload), ct);
            return Results.Ok(await lab.InspectAsync(ct));
        });
        app.MapGet("/__fixture/image/{name}", () => Results.Bytes(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="), "image/png"));
        using var api = lab.Factory.CreateClient(new() { HandleCookies = false, AllowAutoRedirect = false });
        app.Map("/{**path}", async context =>
        {
            var path = context.Request.Path.Value ?? "";
            // Never collect credentials/profile/address data in this lab. Accounts and delivery are synthetic fixtures.
            var authWrite = path.StartsWith("/api/v1/auth", StringComparison.Ordinal) && context.Request.Method is not ("GET" or "OPTIONS") && !new[] { "/api/v1/auth/refresh", "/api/v1/auth/staff/refresh", "/api/v1/auth/logout", "/api/v1/auth/staff/logout" }.Contains(path);
            var identityWrite = context.Request.Method is not ("GET" or "OPTIONS") && (path.StartsWith("/api/v1/addresses", StringComparison.Ordinal) || path.StartsWith("/api/v1/profile", StringComparison.Ordinal));
            if (!path.StartsWith("/api/v1/", StringComparison.Ordinal) || authWrite || identityWrite) { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { message = "Use the payment lab's synthetic accounts and delivery address. No real credentials or personal data." }); return; }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString) { Content = new StreamContent(context.Request.Body) };
            foreach (var header in context.Request.Headers) if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) && !request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            using var response = await api.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers)) context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding"); await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        Console.WriteLine($"SIMULATED PAYMENTS ONLY — database {lab.Database}; open {SimulatorSafety.Origin}/simulator. No merchant credentials, real people or real money.");
        await app.RunAsync();
    }

    private static async Task RequirePayment(SimulatorLab lab, long id, CancellationToken ct)
    {
        using var scope = lab.Factory.Services.CreateScope();
        if (!await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Payments.AnyAsync(p => p.Id == id && p.Provider == SimulatorSafety.Provider && p.ProviderMode == SimulatorSafety.Mode && p.Order!.UserId == lab.CustomerId, ct)) throw new CommerceException("SIMULATOR_PAYMENT_NOT_FOUND", "Simulated payment not found.", 404);
    }
    private static async Task<T> Read<T>(HttpContext context, CancellationToken ct)
    {
        if (context.Request.ContentLength is null or > 8192) throw new CommerceException("SIMULATOR_REQUEST_INVALID", "Use a small JSON simulator request.");
        return await JsonSerializer.DeserializeAsync<T>(context.Request.Body, Json, ct) ?? throw new CommerceException("SIMULATOR_REQUEST_INVALID", "Missing simulator request.");
    }
    private static IResult Html(string name) => Asset(name, "text/html");
    private static IResult Asset(string name)
        => Asset(name, "text/plain");
    private static IResult Asset(string name, string type)
    {
        using var stream = typeof(SimulatorServer).Assembly.GetManifestResourceStream("be.Tests.Simulation.Ui." + name) ?? throw new InvalidOperationException("Simulator UI is unavailable.");
        using var reader = new StreamReader(stream); return Results.Text(reader.ReadToEnd(), type);
    }
    private sealed record EventInput(long PaymentId, string Outcome, Guid EventId, string Delivery);
    private sealed record RefundInput(decimal Amount, string Outcome, Guid IdempotencyKey);
}
