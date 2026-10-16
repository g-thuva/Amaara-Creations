using be.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class OrdersConfiguration : IEntityTypeConfiguration<Order>, IEntityTypeConfiguration<OrderItem>, IEntityTypeConfiguration<OrderStatusHistory>, IEntityTypeConfiguration<CheckoutAttempt>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Total).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.Status).HasConversion<int>(); // Store enum as int
        entity.Property(e => e.ShippingAddress).HasMaxLength(500);
        entity.Property(e => e.ShippingCity).HasMaxLength(100);
        entity.Property(e => e.ShippingPostalCode).HasMaxLength(50);
        entity.Property(e => e.ShippingCountry).HasMaxLength(100);
        entity.Property(e => e.Notes).HasMaxLength(500);
        // Foreign key relationship
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict); // Don't delete user when order exists
        // One-to-many relationship with OrderItems
        entity.HasMany(e => e.OrderItems).WithOne(e => e.Order).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
        // Index for faster queries
        entity.HasIndex(e => e.OrderNumber).IsUnique();
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.OrderDate);
    }

    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.ToTable("OrderItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Quantity).HasDefaultValue(1);
        entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.ItemType).HasConversion<int>();
        entity.Property(e => e.ProductNameSnapshot).IsRequired().HasMaxLength(200);
        entity.Property(e => e.SkuSnapshot).HasMaxLength(64);
        entity.Property(e => e.ImageSnapshot).HasMaxLength(500);
        // Foreign key relationships
        entity.HasOne(e => e.Order).WithMany(e => e.OrderItems).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict); // Don't delete product when order item exists
        // Index for faster queries
        entity.HasIndex(e => e.OrderId);
        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.ProductVariantId);
        entity.HasIndex(e => e.CustomDesignId);
        entity.HasOne(x => x.CustomDesign).WithMany().HasForeignKey(x => x.CustomDesignId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<OrderStatusHistory> entity)
    {
        entity.ToTable("OrderStatusHistories");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.PreviousStatus).HasMaxLength(40);
        entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(40);
        entity.Property(e => e.Reason).HasMaxLength(500);
        entity.HasIndex(e => new { e.OrderId, e.CreatedAt });
        entity.HasOne(e => e.Order).WithMany(e => e.StatusHistory).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ChangedByUser).WithMany().HasForeignKey(e => e.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<CheckoutAttempt> entity)
    {
        entity.HasOne(e => e.Payment).WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("CheckoutAttempts");
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
