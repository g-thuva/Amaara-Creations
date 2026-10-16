using be.Domain.Common;
using be.Application.Abstractions.Email;
using be.Application.Features.Payments;
using be.Domain.Notifications;
using be.Domain.Payments;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.BackgroundJobs;
public sealed class CommerceMaintenanceService(IServiceScopeFactory scopes, ILogger<CommerceMaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var payments = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
                var refunds = scope.ServiceProvider.GetRequiredService<RefundService>();
                var ids = await db.Payments.AsNoTracking()
                    .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Authorized) && (!p.LastReconciledAt.HasValue || p.LastReconciledAt < DateTime.UtcNow.AddSeconds(-30)))
                    .OrderBy(p => p.LastReconciledAt)
                    .Take(20)
                    .Select(p => p.Id)
                    .ToListAsync(stoppingToken);
                foreach (var id in ids)
                {
                    try
                    {
                        await payments.ReconcileAsync(id, stoppingToken);
                        await payments.ExpireAsync(id, stoppingToken);
                    }
                    catch (Exception ex)when (ex is CommerceException or HttpRequestException or TaskCanceledException)
                    {
                        logger.LogWarning("Payment maintenance deferred for PaymentId {Id}; {Type}", id, ex.GetType().Name);
                    }
                    finally
                    {
                        db.ChangeTracker.Clear();
                    }
                }

                var refundIds = await db.Refunds.AsNoTracking()
                    .Where(r => r.Status == "Pending" && r.ProviderTransactionId != null && (!r.LastReconciledAt.HasValue || r.LastReconciledAt < DateTime.UtcNow.AddSeconds(-30)))
                    .OrderBy(r => r.LastReconciledAt)
                    .Take(20)
                    .Select(r => r.Id)
                    .ToListAsync(stoppingToken);
                foreach (var id in refundIds)
                {
                    try
                    {
                        await refunds.ReconcileAsync(id, stoppingToken);
                    }
                    catch (Exception ex)when (ex is CommerceException or HttpRequestException or TaskCanceledException)
                    {
                        logger.LogWarning("Refund maintenance deferred for RefundId {Id}; {Type}", id, ex.GetType().Name);
                    }
                    finally
                    {
                        db.ChangeTracker.Clear();
                    }
                }

                await ProcessNotificationsAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Commerce maintenance deferred; {Type}", ex.GetType().Name);
            }
        }
    }

    public async Task ProcessNotificationsAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var email = services.GetRequiredService<IEmailService>();
        var ids = await db.CommerceNotifications.Where(n => n.SentAt == null && n.Attempts < 5 && (!n.LastAttemptAt.HasValue || n.LastAttemptAt < DateTime.UtcNow.AddMinutes(-5)))
            .OrderBy(n => n.Id)
            .Take(10)
            .Select(n => n.Id)
            .ToListAsync(ct);
        foreach (var id in ids)
        {
            CommerceNotification notification;
            db.ChangeTracker.Clear();
            await using (var tx = await CommerceTransaction.BeginAsync(db, ct))
            {
                notification = await db.CommerceNotifications.Include(n => n.Order).ThenInclude(o => o.User).SingleAsync(n => n.Id == id, ct);
                if (notification.SentAt.HasValue || notification.LastAttemptAt > DateTime.UtcNow.AddMinutes(-5))
                    continue;
                notification.Attempts++;
                notification.LastAttemptAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                if (tx != null)
                    await tx.CommitAsync(ct);
            }

            try
            {
                await email.SendTransactionAsync(notification.Order.User!, notification.Subject, notification.Body, ct);
                notification.SentAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Transactional email deferred for NotificationId {Id}; {Type}", id, ex.GetType().Name);
            }
        }
    }
}
