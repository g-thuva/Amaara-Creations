using be.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
public sealed class MediaCleanupConfiguration : IEntityTypeConfiguration<MediaCleanupWork>
{
    public void Configure(EntityTypeBuilder<MediaCleanupWork> entity)
    {
        entity.ToTable("MediaCleanupWork");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.StorageKey).HasMaxLength(500).IsRequired();
        entity.Property(item => item.PublicUrl).HasMaxLength(500);
        entity.HasIndex(item => item.NotBeforeUtc);
        entity.HasIndex(item => item.StorageKey);
    }
}
