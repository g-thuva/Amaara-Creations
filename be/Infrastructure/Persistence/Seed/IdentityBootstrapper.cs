using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace be.Infrastructure.Persistence.Seed;
public static class IdentityBootstrapper
{
    public static async Task BootstrapIdentityAsync(this WebApplication app)
    {
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var roles = AppRoles.AdministrativeRoles.Append(AppRoles.Customer);
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                var adminBootstrapEnabled = app.Configuration.GetValue<bool>("AdminBootstrap:Enabled");
                if (adminBootstrapEnabled)
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityBootstrap");
                    var adminEmail = RequireRuntimeConfiguration(app, "AdminBootstrap:Email");
                    var adminPassword = RequireRuntimeConfiguration(app, "AdminBootstrap:Password");
                    if (adminPassword.EnumerateRunes().Count() < 6)
                    {
                        throw new InvalidOperationException("AdminBootstrap:Password must be at least 6 characters.");
                    }

                    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                    if (existingAdmin == null)
                    {
                        logger.LogInformation("Creating configured bootstrap admin user.");
                        var adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            Name = "Admin User",
                            EmailConfirmed = true
                        };
                        var result = await userManager.CreateAsync(adminUser, adminPassword);
                        if (!result.Succeeded)
                        {
                            throw new InvalidOperationException($"Configured bootstrap admin user could not be created: {string.Join(", ", result.Errors.Select(e => e.Code))}");
                        }

                        var roleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.SuperAdmin);
                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException($"Configured bootstrap admin role could not be assigned: {string.Join(", ", roleResult.Errors.Select(e => e.Code))}");
                        }

                        logger.LogInformation("Configured bootstrap admin user created.");
                    }
                    else
                    {
                        var isInAdminRole = await userManager.IsInRoleAsync(existingAdmin, AppRoles.Admin);
                        var isInSuperAdminRole = await userManager.IsInRoleAsync(existingAdmin, AppRoles.SuperAdmin);
                        if (!isInAdminRole && !isInSuperAdminRole)
                        {
                            var roleResult = await userManager.AddToRoleAsync(existingAdmin, AppRoles.SuperAdmin);
                            if (!roleResult.Succeeded)
                            {
                                throw new InvalidOperationException($"Configured bootstrap admin role could not be assigned: {string.Join(", ", roleResult.Errors.Select(e => e.Code))}");
                            }

                            logger.LogInformation("Admin role added to configured bootstrap user.");
                        }
                    }
                }

                if (app.Environment.IsDevelopment())
                {
                    await CustomBuilderDevelopmentSeeder.SeedAsync(scope.ServiceProvider);
                    await ShippingDevelopmentSeeder.SeedAsync(scope.ServiceProvider);
                }
            }
        }
        catch (Exception ex)when (app.Environment.IsDevelopment())
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityBootstrap");
            logger.LogWarning(ex, "Database role/admin bootstrap skipped. Configure SQL Server and run migrations for API endpoints that require data.");
        }
    }

    private static string RequireRuntimeConfiguration(WebApplication app, string key)
    {
        var value = app.Configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required when AdminBootstrap:Enabled is true.");
        }

        return value;
    }
}
