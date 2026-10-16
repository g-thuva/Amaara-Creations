using be.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class InventoryConfiguration : IEntityTypeConfiguration<InventoryTransaction>, IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> entity)
    {
        entity.ToTable("InventoryTransactions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Reason).IsRequired().HasMaxLength(300);
        entity.HasIndex(e => new { e.ProductId, e.CreatedAt });
        entity.HasIndex(e => new { e.ProductVariantId, e.CreatedAt });
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ProductVariant).WithMany().HasForeignKey(e => e.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.PerformedByUser).WithMany().HasForeignKey(e => e.PerformedByUserId).OnDelete(DeleteBehavior.SetNull);
        entity.Property(r => r.MovementType).HasDefaultValue("Adjustment");
        entity.HasIndex(r => new { r.ProductId, r.ProductVariantId, r.CreatedAt });
    }

    public void Configure(EntityTypeBuilder<InventoryReservation> entity)
    {
        entity.HasIndex(e => new { e.Status, e.ExpiresAt });
        entity.HasIndex(e => new { e.OrderId, e.ProductId, e.ProductVariantId });
        entity.ToTable("InventoryReservations");
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ProductVariant).WithMany().HasForeignKey(e => e.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
