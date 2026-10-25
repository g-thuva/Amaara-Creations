using be.Domain.Catalog;
using be.Infrastructure.Persistence;
using be.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace be.Tests;
public sealed class MediaMigrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amaara-migration-" + Guid.NewGuid().ToString("N"));
    [Fact]
    public async Task InventoryImportRetryConditionalApplyAndRollbackKeepOriginalFiles()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var environment = new S3MediaStorageTests.Environment { ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot") };
        var directory = Path.Combine(environment.WebRootPath, "uploads", "products"); Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "existing.png"), S3MediaStorageTests.Png);
        await File.WriteAllBytesAsync(Path.Combine(directory, "orphan.png"), S3MediaStorageTests.Png);
        db.Products.Add(new Product { Id = 1, Name = "Test product", Slug = "migration", ImageUrl = "/uploads/products/existing.png" });
        db.ProductMedia.Add(new ProductMedia { Id = 1, ProductId = 1, StorageKey = "uploads/products/existing.png", Url = null }); await db.SaveChangesAsync();
        var migration = new MediaMigration(db, environment, S3MediaStorageTests.Settings());
        var plan = await migration.InventoryAsync(default);
        Assert.Equal(2, plan.Files.Count); Assert.Empty(plan.MissingReferences);
        var file = Assert.Single(plan.Files, file => file.Changes.Count > 0);
        Assert.Contains(plan.Files, file => file.Status == "Orphan — retained");
        using var s3 = new S3MediaStorageTests.FakeS3(); var storage = S3MediaStorageTests.Storage(s3);
        await migration.ImportFileAsync(file, storage, default); await migration.ImportFileAsync(file, storage, default); Assert.Single(s3.Objects);
        await migration.ApplyReferencesAsync(file, false, default); await migration.ApplyReferencesAsync(file, false, default);
        Assert.StartsWith("s3:public/", (await db.ProductMedia.SingleAsync()).StorageKey);
        Assert.StartsWith(MediaReference.PublicRoute, (await db.ProductMedia.SingleAsync()).Url);
        Assert.StartsWith(MediaReference.PublicRoute, (await db.Products.SingleAsync()).ImageUrl);
        await migration.ApplyReferencesAsync(file, true, default);
        Assert.Equal("uploads/products/existing.png", (await db.ProductMedia.SingleAsync()).StorageKey);
        Assert.Equal("local", (await db.ProductMedia.SingleAsync()).StorageProvider);
        Assert.Null((await db.ProductMedia.SingleAsync()).Url);
        Assert.Equal(S3MediaStorageTests.Png, await File.ReadAllBytesAsync(Path.Combine(directory, "existing.png")));
        Assert.True(File.Exists(Path.Combine(directory, "orphan.png")));
        await File.WriteAllBytesAsync(Path.Combine(directory, "existing.png"), [1, 2, 3]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => migration.ImportFileAsync(file, storage, default));
    }
    [Theory]
    [InlineData("private/../../credentials")]
    [InlineData("uploads/../appsettings.json")]
    [InlineData("C:/private/file")]
    public void MigrationRejectsTraversalAndArbitraryLocalPaths(string key)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("path-" + Guid.NewGuid()).Options);
        var migration = new MediaMigration(db, new S3MediaStorageTests.Environment { ContentRootPath = root }, S3MediaStorageTests.Settings());
        Assert.Throws<InvalidOperationException>(() => migration.LocalPath(key));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
