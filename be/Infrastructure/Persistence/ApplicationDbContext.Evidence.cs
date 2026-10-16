using be.Application.Abstractions.Observability;
using be.Domain.Audit;
using be.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Persistence;
public partial class ApplicationDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectEvidence();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectEvidence();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ProtectEvidence()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(e => e.State == EntityState.Added))
        {
            var request = _requestContext?.HttpContext;
            entry.Entity.CorrelationId ??= request?.Items[RequestCorrelation.ItemName]?.ToString();
        }

        if (ChangeTracker.Entries().Any(e => (e.Entity is AuditLog or InventoryTransaction) && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit and inventory evidence is append-only.");
    }
}
