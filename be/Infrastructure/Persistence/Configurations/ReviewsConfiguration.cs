using be.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class ReviewsConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> entity)
    {
        entity.ToTable("Reviews");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Rating).IsRequired();
        entity.Property(e => e.Comment).HasMaxLength(1000);
        // Foreign key relationships
        entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade); // Delete reviews when product is deleted
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict); // Don't delete user when review exists
        // Unique constraint: one review per user per product
        entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        // Index for faster queries
        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.Rating);
        entity.HasIndex(e => e.CreatedAt);
        entity.HasOne(r => r.OrderItem).WithMany().HasForeignKey(r => r.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.ModerationStatus, r.CreatedAt });
        entity.Property(r => r.ModerationStatus).HasDefaultValue("Pending");
    }
}
