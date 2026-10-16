using be.Domain.Wishlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class WishlistConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> entity)
    {
        entity.ToTable("WishlistItems");
        entity.HasKey(e => e.Id);
        // Foreign key relationships
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        // Unique constraint: one wishlist item per user-product combination
        entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
    }
}
