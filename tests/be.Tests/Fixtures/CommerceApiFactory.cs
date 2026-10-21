using be.Application.Abstractions.Email;
using be.Application.Abstractions.Payments;
using be.Application.Features.Notifications;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace be.Tests;
internal sealed class CommerceApiFactory(string? sqlConnection = null) : WebApplicationFactory<Program>
{
    public FakePaymentGateway Gateway { get; } = new()
    {
        Provider = "Datatrans"
    };
    public static byte[] HmacBytes => Enumerable.Repeat((byte)7, 32).ToArray();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (sqlConnection != null)
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlConnection);
            if (!connection.InitialCatalog.StartsWith("AmaaraPhase6Tests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Disposable SQL database required.");
            using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sqlConnection).Options);
            db.Database.Migrate();
        }

        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=unreachable-test-only;Database=Test;Integrated Security=true;Encrypt=false;", ["Jwt:Key"] = "DeterministicTestSigningKeyOnly-32Characters", ["Jwt:Issuer"] = "Test", ["Jwt:Audience"] = "Test", ["Cors:AllowedOrigins:0"] = "http://localhost", ["AdminBootstrap:Enabled"] = "false", ["Administration:Enabled"] = "true" }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=unreachable-test-only;Database=Test;Integrated Security=true;Encrypt=false;", ["Jwt:Key"] = "DeterministicTestSigningKeyOnly-32Characters", ["Jwt:Issuer"] = "Test", ["Jwt:Audience"] = "Test", ["Cors:AllowedOrigins:0"] = "http://localhost", ["AdminBootstrap:Enabled"] = "false", ["Administration:Enabled"] = "true", ["Datatrans:Enabled"] = "true", ["Datatrans:HmacKey"] = Convert.ToHexString(HmacBytes) }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            var options = sqlConnection == null ? new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options : new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sqlConnection).Options;
            services.AddSingleton(options);
            services.AddScoped<ApplicationDbContext>();
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(Gateway);
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, TestEmail>();
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ =>
            {
            });
        });
    }

    private sealed class TestEmail : IEmailService
    {
        public Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default) => Task.CompletedTask;
    }
}

internal sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers["X-Test-User"].ToString();
        if (string.IsNullOrEmpty(user))
            return Task.FromResult(AuthenticateResult.NoResult());
        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? (user == "admin" ? "Admin" : "Customer");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user),
            new(ClaimTypes.Role, role),
            new("amr", "mfa"),
            new("mfa_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}
