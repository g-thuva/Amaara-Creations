using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Application.Features.Identity;
using be.Domain.Identity;
using be.Infrastructure.Identity;
using be.Infrastructure.Observability;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amaara.IdentityAdmin;
internal static class IdentityAdminRunner
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            string? Option(string key) { var index = Array.IndexOf(args, key); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
            var command = args.FirstOrDefault();
            if (command is not ("inspect" or "assign-role" or "enable-local-management")) throw new InvalidOperationException("Use inspect, assign-role or enable-local-management; see docs/security/admin-login-root-cause.md.");
            var environment = Option("--environment") ?? throw new InvalidOperationException("Specify --environment explicitly.");
            if (environment is not ("Development" or "Staging" or "Production")) throw new InvalidOperationException("Unknown environment.");
            if (command == "assign-role" && (environment == "Production" || !args.Contains("--approve-role-change"))) throw new InvalidOperationException("Role changes require explicit non-production approval.");
            var root = Path.GetFullPath(Option("--content-root") ?? Path.Combine(Directory.GetCurrentDirectory(), "be"));
            var settings = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environment}.json", optional: true);
            if (environment == "Development") settings.AddUserSecrets<ApplicationDbContextFactory>(optional: true);
            var config = settings.AddJsonFile($"appsettings.{environment}.Local.json", optional: true).AddEnvironmentVariables().Build();
            var email = args.Contains("--configured-admin") ? config["AdminBootstrap:Email"] : Option("--email");
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Select --configured-admin or an explicit --email; no identity is inferred.");
            var connection = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("The selected environment has no SQL connection.");
            var services = new ServiceCollection();
            services.AddLogging(); services.AddSingleton<IConfiguration>(config);
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connection));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddScoped<IRefreshSessionService, RefreshSessionService>(); services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IdentityRoleProvisioner>();
            await using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            if (command == "assign-role")
            {
                if (pending.Length > 0) throw new InvalidOperationException("Review and apply pending migrations through the backup/cutover process before provisioning.");
                var changed = await scope.ServiceProvider.GetRequiredService<IdentityRoleProvisioner>().Assign(email, Option("--role") ?? "", environment, true, Option("--reason") ?? "", default);
                Console.WriteLine(JsonSerializer.Serialize(new { changed, passwordChanged = false, sessionsRevokedIfChanged = true }));
                return 0;
            }
            var normalizedEmail = manager.NormalizeEmail(email);
            var user = await db.Users.AsNoTracking().Where(item => item.NormalizedEmail == normalizedEmail)
                .Select(item => new { item.Id, item.EmailConfirmed, item.TwoFactorEnabled }).SingleOrDefaultAsync();
            var memberships = user == null ? Array.Empty<string>() : await db.UserRoles.Where(item => item.UserId == user.Id)
                .Join(db.Roles, membership => membership.RoleId, role => role.Id, (_, role) => role.Name!).ToArrayAsync();
            if (command == "enable-local-management")
            {
                var server = new SqlConnectionStringBuilder(connection).DataSource.Replace("tcp:", "", StringComparison.OrdinalIgnoreCase).Split(',')[0].Split('\\')[0];
                if (environment != "Development" || !args.Contains("--approve-local-management") || !new[] { "localhost", "127.0.0.1", ".", "(localdb)" }.Contains(server, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Local activation requires explicit Development approval and a loopback SQL instance.");
                if (config.GetValue<bool>("AdminBootstrap:Enabled")) throw new InvalidOperationException("Disable bootstrap before activating local management.");
                if (user == null || !user.EmailConfirmed || !memberships.Any(AppRoles.AdministrativeRoles.Contains)) throw new InvalidOperationException("The explicitly selected local identity must already be verified and administrative.");
                var path = Path.Combine(root, "appsettings.Development.Local.json");
                var local = File.Exists(path) ? JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject() : new JsonObject();
                var administration = local["Administration"] as JsonObject;
                if (administration == null) { administration = new JsonObject(); local["Administration"] = administration; }
                administration["Enabled"] = true;
                administration["AllowedOrigins"] = new JsonArray("http://localhost:5179", "http://127.0.0.1:5179");
                var cors = local["Cors"] as JsonObject;
                if (cors == null) { cors = new JsonObject(); local["Cors"] = cors; }
                var origins = (config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []).Concat(new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5179", "http://127.0.0.1:5179" }).Distinct(StringComparer.Ordinal);
                cors["AllowedOrigins"] = new JsonArray(origins.Select(origin => (JsonNode?)JsonValue.Create(origin)).ToArray());
                await File.WriteAllTextAsync(path, local.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
                Console.WriteLine(JsonSerializer.Serialize(new { localManagementEnabled = true, requiresAuthenticatorEnrollment = !user.TwoFactorEnabled, databaseChanged = false, bootstrapEnabled = false, pendingMigrations = pending }));
                return 0;
            }
            Console.WriteLine(JsonSerializer.Serialize(new { environment, identityPresent = user != null, roles = memberships, emailConfirmed = user?.EmailConfirmed, mfaEnabled = user?.TwoFactorEnabled, administrationEnabled = config.GetValue<bool>("Administration:Enabled"), bootstrapEnabled = config.GetValue<bool>("AdminBootstrap:Enabled"), pendingMigrations = pending, databaseChanged = false }));
            return 0;
        }
        catch (Exception error)
        {
            // SQL/provider exceptions can contain connection/account details.
            Console.Error.WriteLine(error is InvalidOperationException && error.GetType() == typeof(InvalidOperationException) ? error.Message : "Identity tool failed (" + error.GetType().Name + "). Check the selected environment privately.");
            return 1;
        }
    }
}
