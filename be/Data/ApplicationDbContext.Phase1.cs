using Microsoft.EntityFrameworkCore;
using be.Models;

namespace be.Data
{
    public partial class ApplicationDbContext
    {
        partial void ConfigurePhase1Foundation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
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
                entity.HasOne(e => e.ImageMedia)
                    .WithMany()
                    .HasForeignKey(e => e.ImageMediaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Collection>(entity =>
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
            });

            modelBuilder.Entity<ProductCollection>(entity =>
            {
                entity.ToTable("ProductCollections");
                entity.HasKey(e => new { e.ProductId, e.CollectionId });
                entity.HasOne(e => e.Product)
                    .WithMany(e => e.ProductCollections)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Collection)
                    .WithMany(e => e.ProductCollections)
                    .HasForeignKey(e => e.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CollectionId, e.SortOrder });
            });

            modelBuilder.Entity<ProductVariant>(entity =>
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
                entity.HasOne(e => e.Product)
                    .WithMany(e => e.Variants)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductMedia>(entity =>
            {
                entity.ToTable("ProductMedia");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Url).HasMaxLength(500);
                entity.Property(e => e.MediaType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AltText).HasMaxLength(200);
                entity.HasIndex(e => new { e.ProductId, e.IsPrimary });
                entity.HasIndex(e => new { e.ProductId, e.SortOrder });
                entity.HasOne(e => e.Product)
                    .WithMany(e => e.Media)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Product>()
                .HasOne(e => e.CategoryEntity)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Addresses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(80);
                entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Phone).HasMaxLength(40);
                entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AddressLine2).HasMaxLength(200);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DistrictOrProvince).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(30);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(80);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsDefault });
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshSession>(entity =>
            {
                entity.ToTable("RefreshSessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
                entity.Property(e => e.FamilyId).IsRequired().HasMaxLength(64);
                entity.Property(e => e.UserAgent).HasMaxLength(300);
                entity.Property(e => e.IpAddress).HasMaxLength(64);
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.FamilyId });
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ReplacedBySession)
                    .WithMany()
                    .HasForeignKey(e => e.ReplacedBySessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomDesign>(entity =>
            {
                entity.ToTable("CustomDesigns");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.DesignJson).IsRequired();
                entity.Property(e => e.Width).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Height).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaterialCode).HasMaxLength(120);
                entity.Property(e => e.FinishCode).HasMaxLength(120);
                entity.Property(e => e.CalculatedPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PricingRuleVersion).HasMaxLength(80);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.PreviewMedia)
                    .WithMany()
                    .HasForeignKey(e => e.PreviewMediaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomDesignAsset>(entity =>
            {
                entity.ToTable("CustomDesignAssets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AssetType).IsRequired().HasMaxLength(80);
                entity.Property(e => e.OriginalFileName).HasMaxLength(255);
                entity.Property(e => e.ContentType).HasMaxLength(120);
                entity.HasIndex(e => e.CustomDesignId);
                entity.HasOne(e => e.CustomDesign)
                    .WithMany(e => e.Assets)
                    .HasForeignKey(e => e.CustomDesignId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.ToTable("OrderStatusHistories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PreviousStatus).HasMaxLength(40);
                entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.HasIndex(e => new { e.OrderId, e.CreatedAt });
                entity.HasOne(e => e.Order)
                    .WithMany(e => e.StatusHistory)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(80);
                entity.Property(e => e.Method).IsRequired().HasMaxLength(80);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
                entity.Property(e => e.ProviderTransactionId).HasMaxLength(160);
                entity.Property(e => e.ProviderReference).HasMaxLength(160);
                entity.Property(e => e.FailureReason).HasMaxLength(500);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.Provider, e.ProviderTransactionId })
                    .IsUnique()
                    .HasFilter("[ProviderTransactionId] IS NOT NULL");
                entity.HasOne(e => e.Order)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ShippingZone>(entity =>
            {
                entity.ToTable("ShippingZones");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<ShippingMethod>(entity =>
            {
                entity.ToTable("ShippingMethods");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FreeShippingThreshold).HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasIndex(e => new { e.ShippingZoneId, e.IsActive });
                entity.HasOne(e => e.ShippingZone)
                    .WithMany(e => e.ShippingMethods)
                    .HasForeignKey(e => e.ShippingZoneId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.ToTable("Shipments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Method).HasMaxLength(120);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Courier).HasMaxLength(120);
                entity.Property(e => e.TrackingNumber).HasMaxLength(120);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.TrackingNumber);
                entity.HasOne(e => e.Order)
                    .WithMany(e => e.Shipments)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Promotion>(entity =>
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
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(120);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(120);
                entity.Property(e => e.CorrelationId).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(64);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.ActorUserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasOne(e => e.ActorUser)
                    .WithMany()
                    .HasForeignKey(e => e.ActorUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
