using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using be.Domain.Catalog;
using be.Domain.Cms;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace be.ReleaseRehearsal;

public static class RehearsalRunner
{
    public static async Task<int> Main(string[] args)
    {
        try { await Rehearse(args); return 0; }
        catch (Exception error) { Console.Error.WriteLine($"Published release rehearsal: FAIL; {error.GetType().Name}: {error.Message}"); return 1; }
    }

    private static async Task Rehearse(string[] args)
    {
        if (args.Length != 1) throw new InvalidOperationException("Provide one verified local release directory.");
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "be", "be.csproj"))) directory = directory.Parent;
        var repository = directory?.FullName ?? throw new InvalidOperationException("Run the rehearsal from a repository checkout.");
        var artifact = Path.GetFullPath(Path.Combine(repository, args[0]));
        var allowed = Path.GetFullPath(Path.Combine(repository, ".runtime", "releases")) + Path.DirectorySeparatorChar;
        if (!artifact.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only a local .runtime/releases artifact can be rehearsed.");
        var verification = new ProcessStartInfo(OperatingSystem.IsWindows() ? "python" : "python3") { WorkingDirectory = repository, UseShellExecute = false, CreateNoWindow = true };
        verification.ArgumentList.Add("scripts/verify-release.py"); verification.ArgumentList.Add(artifact);
        using (var verifier = Process.Start(verification) ?? throw new InvalidOperationException("Artifact verification did not start."))
        {
            await verifier.WaitForExitAsync();
            if (verifier.ExitCode != 0) throw new InvalidOperationException("Artifact verification failed before database access.");
        }
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(artifact, "manifest.json")));
        if (manifest.RootElement.GetProperty("mode").GetString() != "rehearsal") throw new InvalidOperationException("This executable accepts rehearsal artifacts only.");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION") ?? throw new InvalidOperationException("Set an isolated AMAARA_TEST_SQL_CONNECTION."));
        var server = connection.DataSource.Replace("tcp:", "", StringComparison.OrdinalIgnoreCase).Split(',')[0].Split('\\')[0];
        if (!connection.InitialCatalog.StartsWith("AmaaraPhase6Tests_", StringComparison.Ordinal) || !new[] { "localhost", "127.0.0.1", ".", "(localdb)" }.Contains(server, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("A local, explicitly disposable SQL connection is required.");
        connection.InitialCatalog = "AmaaraPhase6Tests_Published_" + Guid.NewGuid().ToString("N")[..12];
        var runtime = Path.Combine(repository, ".runtime", "rehearsals", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runtime);
        var password = "RehearsalAa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var user = User("release-owner@example.test", password);
        var other = User("release-other@example.test", password);
        var admin = User("release-admin@example.test", password);
        admin.TwoFactorEnabled = true;
        var role = new IdentityRole("SuperAdmin") { NormalizedName = "SUPERADMIN" };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        await db.Database.MigrateAsync();
        var product = new Product { Name = "REHEARSAL CHF print", Slug = "rehearsal-chf-print", Currency = "CHF", Price = 25.25m, BasePrice = 25.25m, Stock = 2, ImageUrl = "/uploads/rehearsal.png" };
        var design = new CustomDesign { UserId = user.Id, Name = "REHEARSAL private design" };
        var asset = new CustomDesignAsset { CustomDesign = design, StorageKey = "private/rehearsal/artwork.png", AssetType = "artwork", ContentType = "image/png", OriginalFileName = "fixture.png" };
        db.AddRange(user, other, admin, role, product, design, asset, new IdentityUserRole<string> { UserId = admin.Id, RoleId = role.Id },
            new IdentityUserToken<string> { UserId = admin.Id, LoginProvider = "[AspNetUserStore]", Name = "AuthenticatorKey", Value = be.Tests.TotpFixture.Secret },
            new CmsPage { Slug = "release-information", Title = "REHEARSAL public information", IsPublished = true });
        await db.SaveChangesAsync();
        // Execute the operator's exact aggregate script against the disposable schema.
        var reconciliationSql = (await File.ReadAllTextAsync(Path.Combine(repository, "deploy", "reconcile-database.sql")))
            .Replace("$(EXPECTED_DATABASE)", connection.InitialCatalog, StringComparison.Ordinal);
        await using (var reconciliation = new SqlConnection(connection.ConnectionString))
        {
            await reconciliation.OpenAsync();
            await using var command = new SqlCommand(reconciliationSql, reconciliation) { CommandTimeout = 60 };
            await using var reader = await command.ExecuteReaderAsync();
            do { while (await reader.ReadAsync()) { } } while (await reader.NextResultAsync());
        }

        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var apiOrigin = $"http://127.0.0.1:{port}";
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
        certificateRequest.CertificateExtensions.Add(names.Build());
        using var generated = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        // Windows SChannel requires an imported key handle for server authentication.
        // This certificate remains a disposable localhost fixture, never a production certificate.
        using var certificate = new X509Certificate2(generated.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        var frontBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = runtime, EnvironmentName = "Rehearsal" });
        frontBuilder.Logging.ClearProviders();
        frontBuilder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(certificate)));
        await using var front = frontBuilder.Build();
        using var publicFiles = new PhysicalFileProvider(Path.Combine(artifact, "web"));
        front.UseDefaultFiles(new DefaultFilesOptions { FileProvider = publicFiles });
        front.UseStaticFiles(new StaticFileOptions { FileProvider = publicFiles });
        using var bridge = new HttpClient(new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false, UseProxy = false });
        front.Map("/api/{**path}", context => Proxy(context, bridge, apiOrigin));
        front.Map("/uploads/{**path}", context => Proxy(context, bridge, apiOrigin));
        front.MapGet("/health/{kind}", context => Proxy(context, bridge, apiOrigin));
        front.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = publicFiles });
        await front.StartAsync();
        var origin = front.Urls.Single();
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = runtime, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { Path.Combine(artifact, "api", "be.dll"), "--urls", apiOrigin, "--contentRoot", runtime }) start.ArgumentList.Add(argument);
        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production", ["DOTNET_ENVIRONMENT"] = "Production",
            ["ConnectionStrings__DefaultConnection"] = connection.ConnectionString,
            ["Jwt__Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), ["Jwt__Issuer"] = "Rehearsal", ["Jwt__Audience"] = "Rehearsal",
            ["AllowedHosts"] = "127.0.0.1;localhost", ["Authentication__FrontendBaseUrl"] = origin,
            ["Cors__AllowedOrigins__0"] = origin, ["DataProtection__KeysPath"] = Path.Combine(runtime, "keys"),
            ["ReverseProxy__KnownProxies__0"] = "127.0.0.1", ["PublicSite__BaseUrl"] = "https://store.example.test",
            ["Checkout__Currency"] = "CHF", ["Email__Enabled"] = "false", ["Zahls__Enabled"] = "false", ["Datatrans__Enabled"] = "false",
            ["AdminBootstrap__Enabled"] = "false", ["ReleaseControls__AcceptNewPayments"] = "false", ["ReleaseControls__AcceptUploads"] = "false", ["ReleaseControls__AllowAdminChanges"] = "false",
            ["Administration__Enabled"] = "true" // Disposable rehearsal only; packaged defaults remain closed.
        };
        foreach (var entry in settings) start.Environment[entry.Key] = entry.Value;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Published API did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false, ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert?.GetCertHashString() == certificate.GetCertHashString() }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(15) };
            var deadline = DateTime.UtcNow.AddSeconds(45);
            var lastProbe = "not attempted";
            while (true)
            {
                if (process.HasExited) throw new InvalidOperationException("Published API exited; inspect rehearsal api.log.");
                try
                {
                    using var response = await client.GetAsync("/health/ready");
                    lastProbe = "HTTP " + (int)response.StatusCode;
                    if (response.StatusCode == HttpStatusCode.OK) break;
                }
                catch (HttpRequestException error) { lastProbe = error.GetBaseException().Message; }
                if (DateTime.UtcNow > deadline) throw new InvalidOperationException("Published API readiness timed out: " + lastProbe);
                await Task.Delay(250);
            }
            var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
            var publicPath = Path.Combine(runtime, "wwwroot", "uploads", "rehearsal.png");
            var privatePath = Path.Combine(runtime, "App_Data", "private-media", "rehearsal", "artwork.png");
            Directory.CreateDirectory(Path.GetDirectoryName(publicPath)!); Directory.CreateDirectory(Path.GetDirectoryName(privatePath)!);
            await File.WriteAllBytesAsync(publicPath, image); await File.WriteAllBytesAsync(privatePath, image);
            await Status(client, "/health/live", HttpStatusCode.OK);
            await Status(client, "/api/v1/products?search=REHEARSAL", HttpStatusCode.OK);
            await Status(client, $"/api/v1/products/{product.Id}", HttpStatusCode.OK);
            await Status(client, "/api/v1/content/pages/release-information", HttpStatusCode.OK);
            await Status(client, "/uploads/rehearsal.png", HttpStatusCode.OK);
            await Status(client, $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}", HttpStatusCode.Unauthorized);
            async Task Login(User account, bool secondFactor = false)
            {
                using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = account.Email, password, authenticatorCode = secondFactor ? be.Tests.TotpFixture.Code : null });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
            }
            await Login(user);
            await Status(client, $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}", HttpStatusCode.OK);
            await Status(client, "/api/v1/admin/reports", HttpStatusCode.Forbidden);
            using (var missingFactor = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = admin.Email, password }))
                if (missingFactor.StatusCode != HttpStatusCode.Unauthorized) throw new InvalidOperationException("Staff login without its enrolled factor was accepted.");
            await Login(admin, secondFactor: true);
            await Status(client, $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}", HttpStatusCode.OK);
            foreach (var path in new[] { "dashboard", "orders", "customers", "inventory", "custom-designs", "reports", "operational-health" })
                await Status(client, "/api/v1/admin/" + path, HttpStatusCode.OK);
            using var paused = await client.PostAsJsonAsync("/api/v1/checkout/session", new { });
            if (paused.StatusCode != HttpStatusCode.ServiceUnavailable) throw new InvalidOperationException("New-payment pause was not enforced.");
            await Login(other);
            await Status(client, $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}", HttpStatusCode.NotFound);
            await Status(client, "/api/v1/admin/reports", HttpStatusCode.Forbidden);
            var node = new ProcessStartInfo("node") { WorkingDirectory = repository, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "client/scripts/smoke-release.mjs", origin, product.Id.ToString() }) node.ArgumentList.Add(argument);
            node.Environment["AMAARA_REHEARSAL_EMAIL"] = user.Email;
            node.Environment["AMAARA_REHEARSAL_PASSWORD"] = password;
            using var browser = Process.Start(node) ?? throw new InvalidOperationException("Browser smoke did not start.");
            var browserOutput = browser.StandardOutput.ReadToEndAsync(); var browserError = browser.StandardError.ReadToEndAsync();
            using var browserDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try { await browser.WaitForExitAsync(browserDeadline.Token); }
            catch (OperationCanceledException) { if (!browser.HasExited) browser.Kill(entireProcessTree: true); throw new InvalidOperationException("Published browser smoke exceeded its deadline."); }
            Console.Write(await browserOutput); Console.Error.Write(await browserError);
            if (browser.ExitCode != 0) throw new InvalidOperationException("Published browser smoke failed.");
            if (await db.Payments.AnyAsync() || await db.Orders.AnyAsync()) throw new InvalidOperationException("Readiness rehearsal unexpectedly created financial records.");
            await File.WriteAllTextAsync(Path.Combine(runtime, "result.json"), JsonSerializer.Serialize(new { status = "PASS", sourceCommit = manifest.RootElement.GetProperty("sourceCommit").GetString(), publishedApi = true, realSql = true, aggregateReconciliationScript = true, httpsLoopbackProxy = true, productionCertificateVerified = false, publicMedia = true, privateMediaOwnership = true, providerNetworkCalls = 0, productionDeployment = false }));
            Console.WriteLine("Published release rehearsal: PASS; real SQL, HTTPS loopback proxy, public/private media, auth, admin reads, paused payments and browser smoke. No production deployment.");
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await File.WriteAllTextAsync(Path.Combine(runtime, "api.log"), await stdout + await stderr);
            await front.StopAsync();
        }
    }

    private static User User(string email, string password)
    {
        var user = new User { Name = "REHEARSAL only", UserName = email, NormalizedUserName = email.ToUpperInvariant(), Email = email, NormalizedEmail = email.ToUpperInvariant(), EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        return user;
    }
    private static async Task Status(HttpClient client, string path, HttpStatusCode expected)
    {
        using var response = await client.GetAsync(path);
        if (response.StatusCode != expected) throw new InvalidOperationException($"Published smoke {path} returned {(int)response.StatusCode}, expected {(int)expected}.");
    }
    private static async Task Proxy(HttpContext context, HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), origin + context.Request.Path + context.Request.QueryString);
        request.Content = new StreamContent(context.Request.Body);
        foreach (var header in context.Request.Headers)
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) && !request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        request.Headers.Remove("X-Forwarded-Proto"); request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers.Concat(response.Content.Headers)) context.Response.Headers[header.Key] = header.Value.ToArray();
        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
