using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace be.Infrastructure.Persistence;
// Model inspection does not start HTTP hosting, seed data, or require payment/auth secrets.
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(directory, "be.csproj")))
            directory = Path.Combine(directory, "be");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var settings = new ConfigurationBuilder().SetBasePath(directory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        if (environment == "Development")
            settings.AddUserSecrets<ApplicationDbContextFactory>(optional: true);
        var configuration = settings.AddJsonFile($"appsettings.{environment}.Local.json", optional: true).AddEnvironmentVariables().AddCommandLine(args).Build();
        // Offline model/migration commands need a provider, not a database connection.
        // Online commands must provide the real connection through configuration or --connection.
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            connection = "Server=design-time.invalid;Database=AmaaraModelOnly;Integrated Security=true;TrustServerCertificate=true";
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    }
}
