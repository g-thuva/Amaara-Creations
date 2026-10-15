using be.Models;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public partial class ApplicationDbContext
{
    partial void ConfigureOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>().HasOne(r => r.OrderItem).WithMany().HasForeignKey(r => r.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().HasIndex(r => new { r.ModerationStatus, r.CreatedAt });
        modelBuilder.Entity<Review>().Property(r => r.ModerationStatus).HasDefaultValue("Pending");
        modelBuilder.Entity<InventoryTransaction>().Property(r => r.MovementType).HasDefaultValue("Adjustment");
        modelBuilder.Entity<InventoryTransaction>().HasIndex(r => new { r.ProductId, r.ProductVariantId, r.CreatedAt });
        modelBuilder.Entity<CustomDesign>().HasIndex(r => new { r.Status, r.ProductionStatus, r.ProofStatus, r.UpdatedAt });
    }

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
            entry.Entity.CorrelationId ??= request?.Items[be.Infrastructure.CorrelationIdMiddleware.ItemName]?.ToString();
        }
        if (ChangeTracker.Entries().Any(e => (e.Entity is AuditLog or InventoryTransaction) && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit and inventory evidence is append-only.");
    }
}
