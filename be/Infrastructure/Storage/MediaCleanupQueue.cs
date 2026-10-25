using be.Application.Abstractions.Storage;
using be.Domain.Catalog;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Storage;

public sealed class MediaCleanupQueue(ApplicationDbContext db, IServiceScopeFactory scopes, IOptions<StorageOptions> options) : IMediaCleanupQueue
{
    public async Task ReserveAsync(string reference, string? publicUrl, CancellationToken ct)
    {
        // Persist before uploading, outside the caller's uncommitted SQL transaction.
        // Only the reference/URL is stored: never credentials or original file bytes.
        await using var scope = scopes.CreateAsyncScope();
        var reservationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        reservationDb.MediaCleanupWork.Add(new MediaCleanupWork { StorageKey = reference, PublicUrl = publicUrl, NotBeforeUtc = DateTime.UtcNow.AddHours(options.Value.OrphanGraceHours) });
        await reservationDb.SaveChangesAsync(ct);
    }
    public void EnqueueDelete(string reference)
    {
        // The Local provider retains original local files as before. Transition
        // originals are never automatically deleted by the S3 cleanup worker.
        if (reference.StartsWith(MediaReference.PublicRoute, StringComparison.Ordinal)) reference = MediaReference.Public(reference[MediaReference.PublicRoute.Length..]);
        if (!reference.StartsWith("s3:", StringComparison.Ordinal)) return;
        var (isPrivate, key) = MediaReference.Parse(reference, options.Value.S3.Prefixes);
        db.MediaCleanupWork.Add(new MediaCleanupWork { StorageKey = reference, PublicUrl = isPrivate ? null : MediaReference.PublicRoute + key, NotBeforeUtc = DateTime.UtcNow.AddMinutes(5) });
    }

    public static async Task<bool> IsReferencedAsync(ApplicationDbContext db, string reference, string? publicUrl, CancellationToken ct)
    {
        if (await db.CustomDesignAssets.AnyAsync(asset => asset.StorageKey == reference, ct) || await db.ProductMedia.AnyAsync(media => media.StorageKey == reference, ct)) return true;
        if (publicUrl == null) return false;
        // Earlier React clients persisted origin-qualified URLs. Treat those
        // references conservatively, so a domain change cannot orphan live media.
        return await db.Users.AnyAsync(user => user.AvatarUrl != null && user.AvatarUrl.EndsWith(publicUrl), ct)
            || await db.Products.AnyAsync(product => product.ImageUrl != null && product.ImageUrl.EndsWith(publicUrl), ct)
            || await db.ProductMedia.AnyAsync(media => media.Url != null && media.Url.EndsWith(publicUrl), ct)
            || await db.OrderItems.AnyAsync(item => item.ImageSnapshot != null && item.ImageSnapshot.EndsWith(publicUrl), ct)
            || await db.CmsSections.AnyAsync(section => section.Content.Contains(publicUrl), ct)
            || await db.SiteSettings.AnyAsync(setting => setting.Value.Contains(publicUrl), ct);
    }
}

public sealed class MediaCleanupWorker(IServiceScopeFactory scopes, ILogger<MediaCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await ProcessAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Media cleanup deferred; {ErrorType}", ex.GetType().Name); }
        }
    }
    public async Task ProcessAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var storage = services.GetRequiredService<IMediaStorageService>();
        var jobs = await db.MediaCleanupWork.Where(item => item.NotBeforeUtc <= DateTime.UtcNow).OrderBy(item => item.NotBeforeUtc).Take(25).ToListAsync(ct);
        foreach (var job in jobs)
        {
            try
            {
                // Recheck committed references on every attempt, including avatar
                // profile saves and immutable order image snapshots.
                if (!await MediaCleanupQueue.IsReferencedAsync(db, job.StorageKey, job.PublicUrl, ct))
                    await storage.DeleteAsync(job.StorageKey, ct);
                db.MediaCleanupWork.Remove(job);
                await db.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                db.Entry(job).State = EntityState.Unchanged;
                job.Attempts++; job.LastAttemptUtc = DateTime.UtcNow;
                job.NotBeforeUtc = DateTime.UtcNow.AddMinutes(Math.Min(60, 5 * Math.Pow(2, Math.Min(job.Attempts, 4))));
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Media cleanup work {WorkId} deferred; {ErrorType}", job.Id, ex.GetType().Name);
            }
        }
    }
}
