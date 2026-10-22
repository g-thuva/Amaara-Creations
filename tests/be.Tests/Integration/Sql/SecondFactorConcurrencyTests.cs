using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Domain.Identity;
using be.Infrastructure.Identity;
using be.Infrastructure.Observability;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace be.Tests;

public sealed class SecondFactorConcurrencyTests
{
    [SqlCommerceFact]
    public async Task SqlOwnedLockMakesConcurrentAuthenticatorConsumptionSingleUse()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_MfaRace_" + Guid.NewGuid().ToString("N")[..8];
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<IRefreshSessionService, RefreshSessionService>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection.ConnectionString));
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRequestActor>(new TestRequestActor());
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISecondFactorService, SecondFactorService>();
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.AddScoped<IUserStore<User>, ProtectedIdentityUserStore>();
        await using var provider = services.BuildServiceProvider();
        string userId;
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = new User { UserName = "mfa-race@example.test", Email = "mfa-race@example.test", EmailConfirmed = true, Name = "MFA fixture" };
            Assert.True((await users.CreateAsync(user)).Succeeded);
            Assert.True((await users.SetAuthenticationTokenAsync(user, "[AspNetUserStore]", "AuthenticatorKey", TotpFixture.Secret)).Succeeded);
            Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
            userId = user.Id;
        }
        var code = TotpFixture.Code;
        async Task<bool> Verify()
        {
            using var scope = provider.CreateScope();
            var user = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByIdAsync(userId))!;
            return await scope.ServiceProvider.GetRequiredService<ISecondFactorService>().VerifyAsync(user, code, null, default);
        }
        var results = await Task.WhenAll(Verify(), Verify());
        Assert.Single(results, result => result);
        Assert.Single(results, result => !result);
    }
}
