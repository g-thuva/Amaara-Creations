using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Application.Features.Identity;
using be.Domain.Identity;
using be.Infrastructure.Identity;
using be.Infrastructure.Observability;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class IdentityProvisioningSqlTests
{
    [SqlCommerceFact]
    public async Task RoleProvisioningPreservesNormalizedMembershipAndRevokesSessionsAtomically()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_IdentityProvision_" + Guid.NewGuid().ToString("N")[..8];
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection.ConnectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IRefreshSessionService, RefreshSessionService>(); services.AddScoped<IAuditService, AuditService>(); services.AddScoped<IdentityRoleProvisioner>();
        await using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope(); var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        try
        {
            await db.Database.MigrateAsync();
            var users = sp.GetRequiredService<UserManager<User>>(); var user = new User { UserName = "operator-fixture@example.test", Email = "operator-fixture@example.test", Name = "Disposable operator fixture", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user)).Succeeded); var initialStamp = user.SecurityStamp;
            await sp.GetRequiredService<IRefreshSessionService>().CreateSessionAsync(user, new("SQL provisioning fixture", null));
            var tool = sp.GetRequiredService<IdentityRoleProvisioner>();
            Assert.True(await tool.Assign(user.Email, AppRoles.Admin, "Development", true, "Disposable SQL proof", default));
            Assert.False(await tool.Assign(user.Email, AppRoles.Admin, "Development", true, "Same approved identity", default));
            Assert.NotEqual(initialStamp, user.SecurityStamp);
            Assert.True(await users.IsInRoleAsync(user, AppRoles.Admin));
            Assert.Equal("ADMIN", (await db.Roles.SingleAsync()).NormalizedName);
            Assert.Single(await db.UserRoles.ToListAsync());
            Assert.False(await db.RefreshSessions.AnyAsync(x => x.RevokedAt == null));
            Assert.Single(await db.AuditLogs.Where(x => x.Action == "OperatorStaffRoleAssigned").ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
