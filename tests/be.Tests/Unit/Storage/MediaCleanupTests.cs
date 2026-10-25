using be.Application.Abstractions.Storage;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Infrastructure.Persistence;
using be.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;
public sealed class MediaCleanupTests
{
    [Fact]
    public async Task SweepRetainsAbsoluteAvatarReferencesAndDeletesOnlyUnclaimedObjects()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        using var s3 = new S3MediaStorageTests.FakeS3(); var storage = S3MediaStorageTests.Storage(s3);
        var avatar = await storage.UploadAsync(S3MediaStorageTests.File(), "avatars");
        var orphan = await storage.UploadPrivateAsync(S3MediaStorageTests.File(), "custom-artwork/1");
        db.Users.Add(new User { Id = "owner", Name = "Test", AvatarUrl = "https://old-api.example.test" + avatar.PublicUrl });
        db.MediaCleanupWork.AddRange(new MediaCleanupWork { StorageKey = avatar.StorageKey, PublicUrl = avatar.PublicUrl, NotBeforeUtc = DateTime.UtcNow.AddHours(-1) }, new MediaCleanupWork { StorageKey = orphan.StorageKey, NotBeforeUtc = DateTime.UtcNow.AddHours(-1) });
        await db.SaveChangesAsync();
        await using var services = new ServiceCollection().AddSingleton(db).AddSingleton<IMediaStorageService>(storage).BuildServiceProvider();
        await new MediaCleanupWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<MediaCleanupWorker>.Instance).ProcessAsync(services, default);
        Assert.True(await storage.ExistsAsync(avatar.StorageKey)); Assert.False(await storage.ExistsAsync(orphan.StorageKey)); Assert.Empty(await db.MediaCleanupWork.ToListAsync());
    }
}
