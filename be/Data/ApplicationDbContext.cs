using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using be.Models;

namespace be.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for entities
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<ProductCollection> ProductCollections { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductMedia> ProductMedia { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<CustomDesign> CustomDesigns { get; set; }
        public DbSet<CustomDesignAsset> CustomDesignAssets { get; set; }
        public DbSet<CustomBuilderConfigurationVersion> CustomBuilderConfigurationVersions { get; set; }
        public DbSet<CustomBuilderOption> CustomBuilderOptions { get; set; }
        public DbSet<CustomDesignProofRevision> CustomDesignProofRevisions { get; set; }
        public DbSet<RefreshSession> RefreshSessions { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ShippingZone> ShippingZones { get; set; }
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<CmsPage> CmsPages { get; set; }
        public DbSet<CmsSection> CmsSections { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Identity tables names (optional)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(220);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)").IsRequired();
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
            });

            // Configure CartItem entity
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).HasDefaultValue(1);
                
                // Foreign key relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Index for faster queries
                entity.HasIndex(e => new { e.UserId, e.ProductId })
                    .IsUnique()
                    .HasFilter("[ProductId] IS NOT NULL");
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
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
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete user when order exists
                
                // One-to-many relationship with OrderItems
                entity.HasMany(e => e.OrderItems)
                    .WithOne(e => e.Order)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index for faster queries
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.OrderDate);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
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
                entity.HasOne(e => e.Order)
                    .WithMany(e => e.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete product when order item exists
                
                // Index for faster queries
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.ProductVariantId);
                entity.HasIndex(e => e.CustomDesignId);
            });

            // Configure WishlistItem entity
            modelBuilder.Entity<WishlistItem>(entity =>
            {
                entity.ToTable("WishlistItems");
                entity.HasKey(e => e.Id);
                
                // Foreign key relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Unique constraint: one wishlist item per user-product combination
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            // Configure Review entity
            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.Comment).HasMaxLength(1000);
                
                // Foreign key relationships
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete reviews when product is deleted
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete user when review exists
                
                // Unique constraint: one review per user per product
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
                
                // Index for faster queries
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Rating);
                entity.HasIndex(e => e.CreatedAt);
            });

            ConfigureFoundationFoundation(modelBuilder);
            ConfigureCustomBuilder(modelBuilder);
        }

        partial void ConfigureFoundationFoundation(ModelBuilder modelBuilder);
        partial void ConfigureCustomBuilder(ModelBuilder modelBuilder);
    }
}

