using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Tests;

// Upgrade rehearsals seed only the columns present in their pinned old schema.
// Current mapped User properties cannot be inserted before their migration runs.
internal static class LegacyIdentityFixture
{
    public static Task Insert(ApplicationDbContext db, User user) => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Users] ([Id],[Name],[UserName],[Email],[CreatedAt],[UpdatedAt],[EmailConfirmed],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES ({user.Id},{user.Name},{user.UserName},{user.Email},{user.CreatedAt},{user.UpdatedAt},0,0,0,0,0);");
}
