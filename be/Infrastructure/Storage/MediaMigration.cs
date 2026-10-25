using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be.Application.Abstractions.Storage;
using be.Domain.Catalog;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Storage;

public sealed class MediaMigrationPlan
{
    public int Version { get; set; } = 1;
    public string DatabaseFingerprint { get; set; } = "";
    public string PublicBucket { get; set; } = "";
    public string PrivateBucket { get; set; } = "";
    public string Region { get; set; } = "";
    public string? FilesBackup { get; set; }
    public string? FilesBackupSha256 { get; set; }
    public string? SqlBackup { get; set; }
    public string? SqlBackupSha256 { get; set; }
    public List<MigrationFile> Files { get; set; } = [];
    public List<string> MissingReferences { get; set; } = [];
    public List<string> ManualReview { get; set; } = [];
}
public sealed class MigrationFile
{
    public string LocalKey { get; set; } = "";
    public string SourceSha256 { get; set; } = "";
    public long SourceLength { get; set; }
    public string Area { get; set; } = "products";
    public bool Private { get; set; }
    public string TargetKey { get; set; } = "";
    public string Status { get; set; } = "Inventoried";
    public string? Error { get; set; }
    public string? RemoteSha256 { get; set; }
    public List<MediaFieldChange> Changes { get; set; } = [];
}
public sealed record MediaFieldChange(string Entity, string Id, string Property, string Before, string After);

/// <summary>Explicit offline CLI workflow; never registered as an HTTP endpoint.</summary>
public sealed class MediaMigration(ApplicationDbContext db, IWebHostEnvironment environment, StorageOptions settings)
{
    public static JsonSerializerOptions Json { get; } = new() { WriteIndented = true };
    private string PublicRoot => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    private string PrivateRoot => Path.Combine(environment.ContentRootPath, "App_Data", "private-media");
    public string DatabaseFingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(db.Database.IsRelational() ? db.Database.GetDbConnection().DataSource + "/" + db.Database.GetDbConnection().Database : db.ContextId.ToString())));
    public static async Task<string> HashFileAsync(string path, CancellationToken ct)
    {
        await using var file = System.IO.File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(file, ct)).ToLowerInvariant();
    }
    public string LocalPath(string key)
    {
        var isPrivate = key.StartsWith("private/", StringComparison.Ordinal);
        if (!isPrivate && !key.StartsWith("uploads/", StringComparison.Ordinal)) throw new InvalidOperationException("Not a local media reference.");
        var root = Path.GetFullPath(isPrivate ? PrivateRoot : PublicRoot);
        if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Migration does not follow reparse-point roots.");
        var relative = isPrivate ? key[8..] : key;
        if (relative.Split('/', '\\').Any(segment => segment is ".." or "." or "")) throw new InvalidOperationException("Invalid local media path.");
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid local media path.");
        for (var directory = new FileInfo(path) as FileSystemInfo; directory != null && !directory.FullName.Equals(root, StringComparison.OrdinalIgnoreCase); directory = Directory.GetParent(directory.FullName))
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Migration does not follow symbolic links or reparse points.");
        return path;
    }
    public async Task<MediaMigrationPlan> InventoryAsync(CancellationToken ct)
    {
        var plan = new MediaMigrationPlan { DatabaseFingerprint = DatabaseFingerprint, PublicBucket = settings.S3.PublicBucket, PrivateBucket = settings.S3.PrivateBucket, Region = settings.S3.Region };
        var references = new Dictionary<string, List<(string Entity, string Id, string Property, string Before, string Area)>>(StringComparer.Ordinal);
        void Reference(string? key, string entity, object id, string property, string? before, string area)
        {
            if (string.IsNullOrEmpty(key)) return;
            var canonical = key.TrimStart('/').Replace('\\', '/');
            if (Uri.TryCreate(key, UriKind.Absolute, out var absolute) && settings.LegacyPublicOrigins.Contains(absolute.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase) && absolute.Query.Length == 0 && absolute.Fragment.Length == 0 && absolute.UserInfo.Length == 0) canonical = absolute.AbsolutePath.TrimStart('/');
            if (!canonical.StartsWith("uploads/", StringComparison.Ordinal) && !canonical.StartsWith("private/", StringComparison.Ordinal))
            {
                if (!canonical.StartsWith("s3:", StringComparison.Ordinal) && !canonical.StartsWith("api/v1/media/public/", StringComparison.Ordinal)) plan.ManualReview.Add($"{entity}:{id}:{property}: external or unmanaged reference");
                return;
            }
            if (!references.TryGetValue(canonical, out var list)) references[canonical] = list = [];
            list.Add((entity, id.ToString()!, property, JsonSerializer.Serialize(before), area));
        }
        foreach (var media in await db.ProductMedia.AsNoTracking().ToListAsync(ct))
        {
            Reference(media.StorageKey, "ProductMedia", media.Id, "StorageKey", media.StorageKey, "products");
            Reference(media.Url, "ProductMedia", media.Id, "Url", media.Url, "products");
        }
        foreach (var asset in await db.CustomDesignAssets.AsNoTracking().ToListAsync(ct)) Reference(asset.StorageKey, "Asset", asset.Id, "StorageKey", asset.StorageKey, $"{(asset.AssetType == "proof" ? "custom-proofs" : "custom-artwork")}/{asset.CustomDesignId}");
        foreach (var user in await db.Users.AsNoTracking().Where(user => user.AvatarUrl != null).Select(user => new { user.Id, user.AvatarUrl }).ToListAsync(ct)) Reference(user.AvatarUrl, "User", user.Id, "AvatarUrl", user.AvatarUrl, "avatars");
        foreach (var product in await db.Products.AsNoTracking().Select(product => new { product.Id, product.ImageUrl }).ToListAsync(ct)) Reference(product.ImageUrl, "Product", product.Id, "ImageUrl", product.ImageUrl, "products");
        foreach (var orderItem in await db.OrderItems.AsNoTracking().Where(item => item.ImageSnapshot != null).Select(item => new { item.Id, item.ImageSnapshot }).ToListAsync(ct)) Reference(orderItem.ImageSnapshot, "OrderItem", orderItem.Id, "ImageSnapshot", orderItem.ImageSnapshot, "products");
        // Embedded content is inventoried for review, never rewritten with string replacement.
        foreach (var section in await db.CmsSections.AsNoTracking().Where(section => section.Content.Contains("/uploads/")).Select(section => section.Id).ToListAsync(ct)) plan.ManualReview.Add($"CmsSection:{section}: embedded legacy URL requires reviewed content update");
        foreach (var setting in await db.SiteSettings.AsNoTracking().Where(setting => setting.Value.Contains("/uploads/")).Select(setting => setting.Id).ToListAsync(ct)) plan.ManualReview.Add($"SiteSetting:{setting}: embedded legacy URL requires reviewed content update");
        var enumeration = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false };
        foreach (var (root, prefix) in new[] { (Path.Combine(PublicRoot, "uploads"), "uploads/"), (PrivateRoot, "private/") })
        {
            if (!Directory.Exists(root)) continue;
            foreach (var path in Directory.EnumerateFiles(root, "*", enumeration).Order(StringComparer.Ordinal))
            {
                var key = prefix + Path.GetRelativePath(root, path).Replace('\\', '/');
                var file = new MigrationFile { LocalKey = key, Private = prefix == "private/", SourceLength = new FileInfo(path).Length, SourceSha256 = await HashFileAsync(LocalPath(key), ct) };
                if (references.Remove(key, out var fields))
                {
                    file.Area = fields.First().Area;
                    var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key + ":" + file.SourceSha256)))[..32].ToLowerInvariant();
                    file.TargetKey = MediaReference.NewKey(file.Area, file.Private, settings.S3.Prefixes, id);
                    foreach (var field in fields)
                    {
                        var next = field.Property == "StorageKey" ? file.Private ? MediaReference.Private(file.TargetKey) : MediaReference.Public(file.TargetKey) : MediaReference.PublicRoute + file.TargetKey;
                        file.Changes.Add(new(field.Entity, field.Id, field.Property, field.Before, JsonSerializer.Serialize(next)));
                    }
                }
                else file.Status = "Orphan — retained";
                plan.Files.Add(file);
            }
        }
        foreach (var missing in references) plan.MissingReferences.Add(missing.Key);
        return plan;
    }
    public async Task BackupFilesAsync(MediaMigrationPlan plan, string destination, string sqlBackup, CancellationToken ct)
    {
        var full = Path.GetFullPath(destination);
        if (full.StartsWith(Path.GetFullPath(PublicRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Backups must be outside the public web root.");
        if (System.IO.File.Exists(full)) throw new InvalidOperationException("Backup destination already exists; choose a new path.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using (var archive = ZipFile.Open(full, ZipArchiveMode.Create))
            foreach (var file in plan.Files)
            {
                var source = LocalPath(file.LocalKey);
                if (await HashFileAsync(source, ct) != file.SourceSha256) throw new InvalidOperationException("A source changed since inventory. Freeze writes and regenerate inventory.");
                archive.CreateEntryFromFile(source, file.LocalKey, CompressionLevel.Optimal);
            }
        plan.FilesBackup = full; plan.FilesBackupSha256 = await HashFileAsync(full, ct);
        plan.SqlBackup = Path.GetFullPath(sqlBackup); plan.SqlBackupSha256 = await HashFileAsync(sqlBackup, ct);
    }
    public async Task ValidatePlanAsync(MediaMigrationPlan plan, CancellationToken ct)
    {
        if (plan.Version != 1 || plan.DatabaseFingerprint != DatabaseFingerprint || plan.PublicBucket != settings.S3.PublicBucket || plan.PrivateBucket != settings.S3.PrivateBucket || plan.Region != settings.S3.Region) throw new InvalidOperationException("Plan database or target does not match this environment.");
        if (plan.FilesBackup == null || plan.SqlBackup == null || await HashFileAsync(plan.FilesBackup, ct) != plan.FilesBackupSha256 || await HashFileAsync(plan.SqlBackup, ct) != plan.SqlBackupSha256) throw new InvalidOperationException("Verified file and SQL backups are required before reference changes.");
    }
    public async Task ImportFileAsync(MigrationFile file, S3MediaStorageService storage, CancellationToken ct)
    {
        var path = LocalPath(file.LocalKey);
        if (await HashFileAsync(path, ct) != file.SourceSha256) throw new InvalidOperationException("Source checksum changed since inventory.");
        var mime = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", ".gif" when file.Area == "avatars" => "image/gif", _ => throw new InvalidOperationException("Unsupported legacy format; original retained for review.") };
        var stored = await storage.ImportAsync(new MediaUpload(Path.GetFileName(path), mime, file.SourceLength, () => System.IO.File.OpenRead(path)), file.Area, file.Private, file.TargetKey, ct);
        await storage.VerifyAsync(stored.StorageKey, stored.FileSize, stored.Sha256!, ct);
        file.RemoteSha256 = stored.Sha256;
        // Record every metadata change before applying SQL, so rollback restores
        // the original JPEG/WebP MIME/dimensions as well as its reference.
        foreach (var group in file.Changes.GroupBy(change => (change.Entity, change.Id)).ToArray())
        {
            var entity = await EntityAsync(group.Key.Entity, group.Key.Id, ct);
            var updates = group.Key.Entity == "ProductMedia" ? new Dictionary<string, object?> { ["StorageKey"] = stored.StorageKey, ["Url"] = stored.PublicUrl, ["StorageProvider"] = "s3", ["ContentType"] = stored.ContentType, ["FileSize"] = stored.FileSize, ["Width"] = stored.Width, ["Height"] = stored.Height }
                : group.Key.Entity == "Asset" ? new Dictionary<string, object?> { ["ContentType"] = stored.ContentType, ["SizeBytes"] = stored.FileSize, ["Width"] = stored.Width, ["Height"] = stored.Height } : [];
            foreach (var update in updates)
                if (!file.Changes.Any(change => change.Entity == group.Key.Entity && change.Id == group.Key.Id && change.Property == update.Key)) file.Changes.Add(new(group.Key.Entity, group.Key.Id, update.Key, JsonSerializer.Serialize(db.Entry(entity).Property(update.Key).CurrentValue), JsonSerializer.Serialize(update.Value)));
        }
        file.Status = "Uploaded and verified";
    }
    public async Task ApplyReferencesAsync(MigrationFile file, bool rollback, CancellationToken ct)
    {
        if (file.RemoteSha256 == null) throw new InvalidOperationException("Remote verification must precede SQL updates.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        foreach (var change in file.Changes)
        {
            var property = db.Entry(await EntityAsync(change.Entity, change.Id, ct)).Property(change.Property);
            var allowed = change.Entity switch { "ProductMedia" => new[] { "StorageKey", "Url", "StorageProvider", "ContentType", "FileSize", "Width", "Height" }, "Asset" => new[] { "StorageKey", "ContentType", "SizeBytes", "Width", "Height" }, "User" => ["AvatarUrl"], "Product" => ["ImageUrl"], "OrderItem" => ["ImageSnapshot"], _ => [] };
            if (!allowed.Contains(change.Property)) throw new InvalidOperationException("Invalid migration property.");
            var expected = rollback ? change.After : change.Before; var next = rollback ? change.Before : change.After;
            var current = JsonSerializer.Serialize(property.CurrentValue);
            if (current == next) continue; // interrupted run already committed
            if (current != expected) throw new InvalidOperationException("Reference changed since inventory; refusing to overwrite a concurrent update.");
            property.CurrentValue = JsonSerializer.Deserialize(next, property.Metadata.ClrType);
        }
        await db.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear(); file.Status = rollback ? "Rolled back" : "Applied";
    }
    private async Task<object> EntityAsync(string entity, string id, CancellationToken ct) => entity switch
    {
        "ProductMedia" => await db.ProductMedia.FindAsync([int.Parse(id)], ct) ?? throw new InvalidOperationException("Media record missing."),
        "Asset" => await db.CustomDesignAssets.FindAsync([int.Parse(id)], ct) ?? throw new InvalidOperationException("Asset record missing."),
        "User" => await db.Users.FindAsync([id], ct) ?? throw new InvalidOperationException("User record missing."),
        "Product" => await db.Products.FindAsync([int.Parse(id)], ct) ?? throw new InvalidOperationException("Product record missing."),
        "OrderItem" => await db.OrderItems.FindAsync([int.Parse(id)], ct) ?? throw new InvalidOperationException("Order snapshot missing."),
        _ => throw new InvalidOperationException("Unknown migration record type.")
    };
}
