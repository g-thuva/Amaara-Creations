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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace be.Application.Abstractions.Persistence;
// EF Core remains the query/unit-of-work abstraction; no repository layer is added.
public interface IApplicationDbContext
{
    DbSet<MediaCleanupWork> MediaCleanupWork { get; }
    DbSet<Product> Products { get; }

    DbSet<CartItem> CartItems { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<WishlistItem> WishlistItems { get; }

    DbSet<Review> Reviews { get; }

    DbSet<Category> Categories { get; }

    DbSet<Collection> Collections { get; }

    DbSet<ProductCollection> ProductCollections { get; }

    DbSet<ProductVariant> ProductVariants { get; }

    DbSet<ProductMedia> ProductMedia { get; }

    DbSet<Address> Addresses { get; }

    DbSet<CustomDesign> CustomDesigns { get; }

    DbSet<CustomDesignAsset> CustomDesignAssets { get; }

    DbSet<CustomBuilderConfigurationVersion> CustomBuilderConfigurationVersions { get; }

    DbSet<CustomBuilderOption> CustomBuilderOptions { get; }

    DbSet<CustomDesignProofRevision> CustomDesignProofRevisions { get; }

    DbSet<RefreshSession> RefreshSessions { get; }
    DbSet<CustomerRequest> CustomerRequests { get; }

    DbSet<OrderStatusHistory> OrderStatusHistories { get; }

    DbSet<Payment> Payments { get; }

    DbSet<ShippingZone> ShippingZones { get; }

    DbSet<ShippingMethod> ShippingMethods { get; }

    DbSet<Shipment> Shipments { get; }

    DbSet<Promotion> Promotions { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<InventoryTransaction> InventoryTransactions { get; }

    DbSet<CmsPage> CmsPages { get; }

    DbSet<CmsSection> CmsSections { get; }

    DbSet<SiteSetting> SiteSettings { get; }

    DbSet<InventoryReservation> InventoryReservations { get; }

    DbSet<Refund> Refunds { get; }

    DbSet<CheckoutAttempt> CheckoutAttempts { get; }

    DbSet<PromotionRedemption> PromotionRedemptions { get; }

    DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; }

    DbSet<CommerceNotification> CommerceNotifications { get; }

    DbSet<User> Users { get; }

    DbSet<IdentityRole> Roles { get; }

    DbSet<IdentityUserRole<string>> UserRoles { get; }

    ChangeTracker ChangeTracker { get; }

    DatabaseFacade Database { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction?> BeginCommerceTransactionAsync(CancellationToken cancellationToken);
}
