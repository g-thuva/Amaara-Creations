using be.Domain.Cart;
using be.Domain.CustomDesigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class CartConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.ToTable("CartItems");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Quantity).HasDefaultValue(1);
        // Foreign key relationships
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        // Index for faster queries
        entity.HasIndex(e => new { e.UserId, e.ProductId, e.ProductVariantId }).IsUnique().HasFilter("[ProductId] IS NOT NULL");
        entity.HasOne(e => e.ProductVariant).WithMany().HasForeignKey(e => e.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("CartItems", table => table.HasCheckConstraint("CK_CartItems_ItemType", "([ItemType] = 0 AND [ProductId] IS NOT NULL AND [CustomDesignId] IS NULL) OR ([ItemType] = 1 AND [ProductId] IS NULL AND [ProductVariantId] IS NULL AND [CustomDesignId] IS NOT NULL)"));
        entity.Property(x => x.ItemType).HasConversion<int>();
        entity.Property(x => x.UnitPriceSnapshot).HasColumnType("decimal(18,2)");
        entity.HasIndex(x => new { x.UserId, x.CustomDesignId }).IsUnique().HasFilter("[CustomDesignId] IS NOT NULL");
        entity.HasOne(x => x.CustomDesign).WithMany().HasForeignKey(x => x.CustomDesignId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<CustomBuilderConfigurationVersion>()
            .WithMany()
            .HasForeignKey(x => x.BuilderConfigurationVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
