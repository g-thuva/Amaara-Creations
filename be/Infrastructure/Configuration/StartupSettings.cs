namespace be.Infrastructure.Configuration;
public static class StartupSettings
{
    public static string[] GetManagementOrigins(IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = configuration.GetSection("Administration:AllowedOrigins").Get<string[]>()?.Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray() ?? [];
        return origins.Length > 0 ? origins : environment.IsDevelopment() ? ["http://localhost:5179", "http://127.0.0.1:5179"] : [];
    }
    public static string RequireConfiguration(WebApplicationBuilder builder, string key)
    {
        var value = builder.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required. Configure it through appsettings.Development.json, user secrets, or environment variables.");
        }

        return value;
    }

    public static string RequireJwtKey(WebApplicationBuilder builder)
    {
        var jwtKey = RequireConfiguration(builder, "Jwt:Key");
        if (jwtKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        }

        var knownDevelopmentKey = string.Equals(jwtKey, "DevelopmentOnlyJwtSigningKey-Change-With-User-Secrets", StringComparison.Ordinal);
        if (!builder.Environment.IsDevelopment() && knownDevelopmentKey)
        {
            throw new InvalidOperationException("Production cannot start with the development JWT signing key.");
        }

        return jwtKey;
    }

    public static string[] GetAllowedOrigins(WebApplicationBuilder builder)
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray() ?? Array.Empty<string>();
        if (origins.Length == 0)
        {
            if (builder.Environment.IsDevelopment())
            {
                return new[]
                {
                    "http://localhost:5173",
                    "http://127.0.0.1:5173",
                    "http://localhost:5179",
                    "http://127.0.0.1:5179"
                };
            }

            throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
        }

        return origins;
    }
}
