using be.Domain.Identity;
using be.Application.Abstractions.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;

public sealed class RefreshSessionStateTests
{
    [Theory]
    [InlineData("expired")]
    [InlineData("unconfirmed")]
    [InlineData("locked")]
    [InlineData("revoked")]
    public async Task IneligibleSessionsCannotIssueReplacementTokens(string state)
    {
        await using var factory = new CommerceApiFactory();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { Name = "Session state fixture", UserName = "state@example.test", Email = "state@example.test", EmailConfirmed = state != "unconfirmed", LockoutEnabled = true };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        if (state == "locked") Assert.True((await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1))).Succeeded);
        var service = scope.ServiceProvider.GetRequiredService<IRefreshSessionService>();
        var issue = await service.CreateSessionAsync(user, new("fixture", null));
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (state == "expired") issue.Session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        if (state == "revoked") issue.Session.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.Null(await service.RotateAsync(issue.RawToken, new("fixture", null)));
        Assert.Equal(1, await db.RefreshSessions.CountAsync());
        Assert.NotNull(issue.Session.RevokedAt);
    }
}
