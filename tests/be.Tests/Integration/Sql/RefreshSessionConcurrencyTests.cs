using System.Data.Common;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class RefreshSessionConcurrencyTests
{
    [SqlCommerceFact]
    public async Task ConcurrentRotationIssuesOneReplacementAndReplayRevokesTheWholeFamily()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_RefreshRace_" + Guid.NewGuid().ToString("N")[..8];
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection.ConnectionString).AddInterceptors(new OverlappingReads()));
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<RefreshSessionService>();
        await using var provider = services.BuildServiceProvider();
        string raw;
        string userId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            var user = new User { UserName = "refresh-race@example.test", Email = "refresh-race@example.test", EmailConfirmed = true, Name = "Refresh fixture" };
            Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<User>>().CreateAsync(user)).Succeeded);
            userId = user.Id;
            raw = (await scope.ServiceProvider.GetRequiredService<RefreshSessionService>().CreateSessionAsync(user, new("fixture", null))).RawToken;
        }
        async Task<RefreshTokenRotationResult?> Rotate()
        {
            using var scope = provider.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<RefreshSessionService>().RotateAsync(raw, new("fixture", null));
        }
        var results = await Task.WhenAll(Rotate(), Rotate());
        Assert.Equal(1, results.Count(result => result != null));
        using var verify = provider.CreateScope();
        var context = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await context.RefreshSessions.AsNoTracking().Where(session => session.UserId == userId).ToListAsync();
        Assert.Equal(2, family.Count);
        Assert.All(family, session => Assert.NotNull(session.RevokedAt));
        var sessions = verify.ServiceProvider.GetRequiredService<RefreshSessionService>();
        var userManager = verify.ServiceProvider.GetRequiredService<UserManager<User>>();
        var login = await sessions.CreateSessionAsync((await userManager.FindByIdAsync(userId))!, new("fixture", null));
        await sessions.RevokeAllForUserAsync(userId);
        Assert.Null(await sessions.RotateAsync(login.RawToken, new("fixture", null)));
    }

    // Widen the read/write overlap without relying on thread scheduling or real providers.
    private sealed class OverlappingReads : DbCommandInterceptor
    {
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("RefreshSessions") && command.CommandText.Contains("SELECT"))
                await Task.Delay(150, cancellationToken);
            return result;
        }
    }
}
