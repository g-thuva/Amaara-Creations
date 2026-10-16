using be.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class PromotionsConfiguration : IEntityTypeConfiguration<Promotion>, IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(EntityTypeBuilder<Promotion> entity)
    {
        entity.ToTable("Promotions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Code).IsRequired().HasMaxLength(40);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.DiscountType).HasConversion<int>();
        entity.Property(e => e.DiscountValue).HasColumnType("decimal(18,2)");
        entity.Property(e => e.MinimumSpend).HasColumnType("decimal(18,2)");
        entity.HasIndex(e => e.Code).IsUnique();
        entity.HasIndex(e => new { e.IsActive, e.StartsAt, e.EndsAt });
    }

    public void Configure(EntityTypeBuilder<PromotionRedemption> entity)
    {
        entity.HasIndex(e => new { e.OrderId, e.PromotionId }).IsUnique();
        entity.HasIndex(e => new { e.PromotionId, e.UserId, e.Status });
        entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Promotion).WithMany().HasForeignKey(e => e.PromotionId).OnDelete(DeleteBehavior.Restrict);
    }
}
