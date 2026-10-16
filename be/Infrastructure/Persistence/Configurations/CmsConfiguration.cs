using be.Domain.Cms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class CmsConfiguration : IEntityTypeConfiguration<CmsPage>, IEntityTypeConfiguration<CmsSection>, IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<CmsPage> entity)
    {
        entity.ToTable("CmsPages");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Title).IsRequired().HasMaxLength(160);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(180);
        entity.Property(e => e.Summary).HasMaxLength(300);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.IsPublished);
    }

    public void Configure(EntityTypeBuilder<CmsSection> entity)
    {
        entity.ToTable("CmsSections");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SectionKey).IsRequired().HasMaxLength(120);
        entity.Property(e => e.ContentType).IsRequired().HasMaxLength(80);
        entity.Property(e => e.Content).IsRequired();
        entity.HasIndex(e => new { e.CmsPageId, e.SectionKey }).IsUnique();
        entity.HasIndex(e => new { e.CmsPageId, e.SortOrder });
        entity.HasOne(e => e.Page).WithMany(e => e.Sections).HasForeignKey(e => e.CmsPageId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<SiteSetting> entity)
    {
        entity.ToTable("SiteSettings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Key).IsRequired().HasMaxLength(120);
        entity.Property(e => e.Value).IsRequired().HasMaxLength(4000);
        entity.Property(e => e.ValueType).IsRequired().HasMaxLength(80);
        entity.Property(e => e.Description).HasMaxLength(300);
        entity.HasIndex(e => e.Key).IsUnique();
        entity.HasIndex(e => e.IsPublic);
    }
}
