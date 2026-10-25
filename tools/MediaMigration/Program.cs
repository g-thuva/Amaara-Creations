using System.Text.Json;
using be.Infrastructure.Persistence;
using be.Infrastructure.Storage;
using be.Application.Abstractions.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

static string? Option(string[] arguments, string name) { var index = Array.IndexOf(arguments, name); return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null; }
try
{
    var command = args.FirstOrDefault();
    if (command is not ("inventory" or "dry-run" or "backup" or "apply" or "rollback")) throw new InvalidOperationException("Use inventory, dry-run, backup, apply or rollback; see docs/storage/MEDIA_MIGRATION.md.");
    var root = Path.GetFullPath(Option(args, "--content-root") ?? throw new InvalidOperationException("An explicit --content-root is required."));
    var environmentName = Option(args, "--environment") ?? throw new InvalidOperationException("An explicit --environment is required.");
    if (environmentName is not ("Development" or "Testing" or "Staging" or "Production")) throw new InvalidOperationException("Invalid environment.");
    var changing = command is "apply" or "rollback";
    if (changing && (!args.Contains("--execute") || !args.Contains("--maintenance-window"))) throw new InvalidOperationException("Reference changes require --execute --maintenance-window after backup and dry-run review.");
    if (changing && environmentName == "Production" && !args.Contains("--approve-production")) throw new InvalidOperationException("Production requires separate owner approval and --approve-production.");
    var configuration = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environmentName}.json", optional: true);
    if (environmentName == "Development") configuration.AddUserSecrets<ApplicationDbContextFactory>(optional: true);
    var config = configuration.AddJsonFile($"appsettings.{environmentName}.Local.json", optional: true).AddEnvironmentVariables().Build();
    if (args.Contains("--require-loopback"))
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? "");
        var host = connection.DataSource.Replace("tcp:", "", StringComparison.OrdinalIgnoreCase).Split('\\')[0].Split(',')[0];
        if (!new[] { ".", "localhost", "127.0.0.1", "(localdb)", System.Environment.MachineName }.Contains(host, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("The configured SQL target is not loopback; local audit was not executed.");
    }
    var settings = StorageOptions.Read(config);
    var environment = new ToolEnvironment(root, environmentName);
    var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(config); services.AddSingleton<IWebHostEnvironment>(environment);
    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("SQL connection is not configured.")));
    if (changing) services.AddMediaStorage(config, environment);
    services.AddHttpClient<IUploadScanner, HttpUploadScanner>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    await using var provider = services.BuildServiceProvider(); await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migration = new MediaMigration(db, environment, settings);
    var path = Path.GetFullPath(Option(args, "--manifest") ?? throw new InvalidOperationException("An explicit private --manifest path is required."));
    if (path.StartsWith(Path.GetFullPath(environment.WebRootPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Manifest must not be under the public web root.");
    async Task Save(MediaMigrationPlan plan)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(plan, MediaMigration.Json));
        File.Move(temporary, path, true);
    }
    var plan = command == "inventory" ? await migration.InventoryAsync(default) : JsonSerializer.Deserialize<MediaMigrationPlan>(await File.ReadAllTextAsync(path)) ?? throw new InvalidOperationException("Invalid plan.");
    if (command == "inventory") { if (File.Exists(path)) throw new InvalidOperationException("Manifest exists; preserve it and choose a new path."); await Save(plan); }
    if (command == "dry-run")
    {
        foreach (var file in plan.Files.Where(file => file.Changes.Count > 0))
        {
            try
            {
                var source = migration.LocalPath(file.LocalKey);
                if (await MediaMigration.HashFileAsync(source, default) != file.SourceSha256) throw new InvalidOperationException("Source changed since inventory.");
                var mime = Path.GetExtension(source).ToLowerInvariant() switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", ".gif" when file.Area == "avatars" => "image/gif", _ => throw new InvalidOperationException("Unsupported legacy format.") };
                var maximum = file.Area == "avatars" ? settings.Limits.AvatarBytes : !file.Private ? settings.Limits.ProductBytes : file.Area.StartsWith("custom-proofs/", StringComparison.Ordinal) ? settings.Limits.ProofBytes : settings.Limits.ArtworkBytes;
                await ImageUploadProcessor.PrepareAsync(new(Path.GetFileName(source), mime, file.SourceLength, () => File.OpenRead(source)), config, scope.ServiceProvider.GetRequiredService<IUploadScanner>(), maximum, file.Area == "avatars", default);
                file.Error = null;
                if (file.Status != "Applied") file.Status = "Dry-run validated";
            }
            catch (Exception exception) { file.Error = exception.GetType().Name; }
        }
        await Save(plan);
    }
    if (command == "backup")
    {
        var sqlBackup = Option(args, "--sql-backup") ?? throw new InvalidOperationException("Supply an existing COPY_ONLY/CHECKSUM SQL backup file, accessible locally and at the same SQL Server path.");
        // Parameterized restore verification is read-only; it does not restore or alter a database.
        await db.Database.ExecuteSqlInterpolatedAsync($"RESTORE VERIFYONLY FROM DISK = {sqlBackup} WITH CHECKSUM");
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using (var header = connection.CreateCommand())
        {
            header.CommandText = "RESTORE HEADERONLY FROM DISK = @path";
            var parameter = header.CreateParameter(); parameter.ParameterName = "@path"; parameter.Value = sqlBackup; header.Parameters.Add(parameter);
            await using var reader = await header.ExecuteReaderAsync(); var sets = 0;
            while (await reader.ReadAsync())
            {
                sets++;
                if (!string.Equals(reader["DatabaseName"].ToString(), connection.Database, StringComparison.OrdinalIgnoreCase) || Convert.ToInt32(reader["BackupType"]) != 1 || !Convert.ToBoolean(reader["HasBackupChecksums"])) throw new InvalidOperationException("Use a single full CHECKSUM backup of the selected database.");
            }
            if (sets != 1) throw new InvalidOperationException("A single verified full SQL backup set is required.");
        }
        await migration.BackupFilesAsync(plan, Option(args, "--files-backup") ?? throw new InvalidOperationException("Choose a new private --files-backup ZIP path."), sqlBackup, default);
        await Save(plan);
    }
    if (changing)
    {
        if (settings.Provider != "S3") throw new InvalidOperationException("S3 settings are required for migration and verification.");
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply reviewed additive schema migrations through the release process first.");
        await migration.ValidatePlanAsync(plan, default);
        if (command == "apply" && (plan.ManualReview.Count > 0 || plan.MissingReferences.Count > 0) && !args.Contains("--acknowledge-review-items")) throw new InvalidOperationException("Resolve and review inventory gaps before applying. Explicit --acknowledge-review-items is required for reviewed exceptions.");
        var storage = scope.ServiceProvider.GetRequiredService<S3MediaStorageService>();
        foreach (var file in plan.Files.Where(file => file.Changes.Count > 0))
        {
            try
            {
                if (command == "rollback")
                {
                    if (file.Status == "Rolled back" || file.RemoteSha256 == null) continue;
                    if (await MediaMigration.HashFileAsync(migration.LocalPath(file.LocalKey), default) != file.SourceSha256) throw new InvalidOperationException("Rollback original changed or is missing; restore the verified file backup first.");
                    await migration.ApplyReferencesAsync(file, true, default);
                }
                else
                {
                    if (file.Status == "Applied") continue;
                    await migration.ImportFileAsync(file, storage, default);
                    await Save(plan); // preserve upload verification and rollback metadata before SQL
                    await migration.ApplyReferencesAsync(file, false, default);
                }
                file.Error = null;
            }
            catch (Exception exception) { db.ChangeTracker.Clear(); file.Error = exception.GetType().Name; file.Status = "Failed — original retained"; }
            await Save(plan);
        }
    }
    Console.WriteLine(JsonSerializer.Serialize(new { command, files = plan.Files.Count, referenced = plan.Files.Count(file => file.Changes.Count > 0), missing = plan.MissingReferences.Count, manualReview = plan.ManualReview.Count, results = plan.Files.GroupBy(file => file.Status).ToDictionary(group => group.Key, group => group.Count()), secretsPrinted = false, originalsDeleted = false }));
    return plan.Files.Any(file => file.Error != null) ? 2 : 0;
}
catch (Exception exception)
{
    // Configuration/domain errors are intentionally descriptive; provider/SQL
    // exception messages may contain deployment details and are not printed.
    Console.Error.WriteLine(exception is InvalidOperationException ? exception.Message : $"Migration failed ({exception.GetType().Name}); no originals were deleted.");
    return 1;
}
sealed class ToolEnvironment(string root, string name) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "Amaara.MediaMigration";
    public string ContentRootPath { get; set; } = root;
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
