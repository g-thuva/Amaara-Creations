using be.Domain.Audit;
using be.Domain.Cart;
using be.Domain.Catalog;
using be.Domain.Cms;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using be.Domain.Inventory;
using be.Domain.Notifications;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Domain.Promotions;
using be.Domain.Reviews;
using be.Domain.Shipping;
using be.Domain.Wishlist;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Persistence
{
    public partial class ApplicationDbContext : IdentityDbContext<User>, be.Application.Abstractions.Persistence.IApplicationDbContext
    {
        private readonly IHttpContextAccessor? _requestContext;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? requestContext = null) : base(options)
        {
            _requestContext = requestContext;
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
        public DbSet<MediaCleanupWork> MediaCleanupWork { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<CustomDesign> CustomDesigns { get; set; }
        public DbSet<CustomDesignAsset> CustomDesignAssets { get; set; }
        public DbSet<CustomBuilderConfigurationVersion> CustomBuilderConfigurationVersions { get; set; }
        public DbSet<CustomBuilderOption> CustomBuilderOptions { get; set; }
        public DbSet<CustomDesignProofRevision> CustomDesignProofRevisions { get; set; }
        public DbSet<RefreshSession> RefreshSessions { get; set; }
        public DbSet<CustomerRequest> CustomerRequests { get; set; }
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
        public DbSet<InventoryReservation> InventoryReservations { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<CheckoutAttempt> CheckoutAttempts { get; set; }
        public DbSet<PromotionRedemption> PromotionRedemptions { get; set; }
        public DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }
        public DbSet<CommerceNotification> CommerceNotifications { get; set; }

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginCommerceTransactionAsync(CancellationToken cancellationToken) => CommerceTransaction.BeginAsync(this, cancellationToken);
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
