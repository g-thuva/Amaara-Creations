using be.Domain.Common;
using be.Domain.Catalog;
using be.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class CatalogConfiguration : IEntityTypeConfiguration<Product>, IEntityTypeConfiguration<Category>, IEntityTypeConfiguration<Collection>, IEntityTypeConfiguration<ProductCollection>, IEntityTypeConfiguration<ProductVariant>, IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Products");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(220);
        entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue(Money.DefaultCurrency).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.ShortDescription).HasMaxLength(300);
        entity.Property(e => e.ImageUrl).HasMaxLength(500);
        entity.Property(e => e.Category).HasMaxLength(50);
        entity.Property(e => e.BaseSku).HasMaxLength(64);
        entity.Property(e => e.Stock).HasDefaultValue(0);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.IsFeatured).HasDefaultValue(false);
        entity.HasIndex(e => e.Slug).IsUnique().HasFilter("[Slug] <> ''");
        entity.HasIndex(e => e.CategoryId);
        entity.HasIndex(e => e.IsActive);
        entity.HasOne(e => e.CategoryEntity).WithMany(e => e.Products).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("Categories");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(140);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.SortOrder).HasDefaultValue(0);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.ParentCategoryId);
        entity.HasIndex(e => new { e.IsActive, e.SortOrder });
        entity.HasOne(e => e.ParentCategory)
            .WithMany(e => e.ChildCategories)
            .HasForeignKey(e => e.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.ImageMedia).WithMany().HasForeignKey(e => e.ImageMediaId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Collection> entity)
    {
        entity.ToTable("Collections");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(140);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.SortOrder).HasDefaultValue(0);
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => new { e.IsActive, e.SortOrder });
    }

    public void Configure(EntityTypeBuilder<ProductCollection> entity)
    {
        entity.ToTable("ProductCollections");
        entity.HasKey(e => new { e.ProductId, e.CollectionId });
        entity.HasOne(e => e.Product).WithMany(e => e.ProductCollections).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Collection).WithMany(e => e.ProductCollections).HasForeignKey(e => e.CollectionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.CollectionId, e.SortOrder });
    }

    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        entity.ToTable("ProductVariants");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Sku).IsRequired().HasMaxLength(64);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.PriceOverride).HasColumnType("decimal(18,2)");
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.RowVersion).IsRowVersion();
        entity.HasIndex(e => e.Sku).IsUnique();
        entity.HasIndex(e => new { e.ProductId, e.IsActive });
        entity.HasOne(e => e.Product).WithMany(e => e.Variants).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ProductMedia> entity)
    {
        entity.ToTable("ProductMedia");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Url).HasMaxLength(500);
        entity.Property(e => e.MediaType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.AltText).HasMaxLength(200);
        entity.Property(e => e.OriginalFileName).HasMaxLength(255);
        entity.Property(e => e.ContentType).HasMaxLength(120);
        entity.Property(e => e.StorageProvider).IsRequired().HasMaxLength(80).HasDefaultValue("local");
        entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
        entity.HasIndex(e => new { e.ProductId, e.IsPrimary });
        entity.HasIndex(e => new { e.ProductId, e.SortOrder });
        entity.HasIndex(e => e.CreatedAt);
        entity.HasOne(e => e.Product).WithMany(e => e.Media).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
