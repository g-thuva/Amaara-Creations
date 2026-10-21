using be.Application.Features.Checkout;
using be.Application.Features.CustomDesigns;
using be.Application.Features.Inventory;
using be.Application.Features.Orders;
using be.Application.Features.Payments;
using be.Application.Features.Promotions;

namespace be.Application;
public static class DependencyInjection
{
    public static void AddAmaaraApplication(this IServiceCollection services)
    {
        services.AddScoped<be.Application.Features.Reporting.DashboardQueries>();
        services.AddScoped<be.Application.Features.Customers.CustomerQueries>();
        services.AddScoped<be.Application.Features.Identity.CapabilityQueries>();
        services.AddScoped<be.Application.Features.Payments.PaymentQueries>();
        services.AddScoped<be.Application.Features.Customers.CustomerOperationsQueries>();
        services.AddScoped<be.Application.Features.Audit.AuditQueries>();
        services.AddScoped<be.Application.Features.Reporting.ReportingQueries>();
        services.AddScoped<be.Application.Features.Identity.StaffManagementService>();
        services.AddScoped<be.Application.Features.Catalog.ProductManagementService>();
        services.AddScoped<be.Application.Features.Catalog.ProductVariantService>();
        services.AddScoped<be.Application.Features.Catalog.ProductMediaService>();
        services.AddScoped<be.Application.Features.Reporting.OperationalHealthQueries>();
        services.AddScoped<be.Application.Features.Identity.AddressBookService>();
        services.AddScoped<be.Application.Features.Catalog.CategoryManagementService>();
        services.AddScoped<be.Application.Features.Catalog.CollectionManagementService>();
        services.AddScoped<be.Application.Features.Cms.CmsManagementService>();
        services.AddScoped<be.Application.Features.CustomDesigns.BuilderConfigurationService>();
        services.AddScoped<be.Application.Features.Production.ProductionService>();
        services.AddScoped<be.Application.Features.Inventory.InventoryQueries>();
        services.AddScoped<be.Application.Features.Catalog.MediaQueries>();
        services.AddScoped<be.Application.Features.Promotions.PromotionManagementService>();
        services.AddScoped<be.Application.Features.Cms.SettingsService>();
        services.AddScoped<be.Application.Features.Shipping.ShippingManagementService>();
        services.AddScoped<be.Application.Features.Cart.CartService>();
        services.AddScoped<be.Application.Features.Catalog.CatalogQueries>();
        services.AddScoped<be.Application.Features.Cms.CmsQueries>();
        services.AddScoped<be.Application.Features.CustomDesigns.BuilderQueries>();
        services.AddScoped<be.Application.Features.CustomDesigns.CustomDesignService>();
        services.AddScoped<be.Application.Features.Orders.OrderService>();
        services.AddScoped<be.Application.Features.Catalog.ProductQueries>();
        services.AddScoped<be.Application.Features.Reviews.ReviewService>();
        services.AddScoped<be.Application.Features.Identity.AccountService>();
        services.AddScoped<be.Application.Features.Wishlist.WishlistService>();
        services.AddScoped<be.Application.Features.Identity.IdentityService>();
        services.AddScoped<be.Application.Features.Identity.AccountSessionService>();
        services.AddScoped<be.Application.Features.Identity.AccountReauthentication>();
        services.AddScoped<be.Application.Features.Identity.CustomerRequestService>();
        services.AddScoped<be.Application.Features.Identity.AccountPrivacyService>();
        services.AddScoped<be.Application.Features.Cms.PublicSeoService>();
        services.AddScoped<InventoryOperations>();
        services.AddScoped<CustomStickerPricingService>();
        services.AddScoped<CheckoutPricingService>();
        services.AddScoped<PromotionService>();
        services.AddScoped<InventoryReservationService>();
        services.AddScoped<be.Application.Features.Payments.PaymentOperationsService>();
        services.AddScoped<be.Application.Features.Payments.PaymentWebhookService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<OrderLifecycleService>();
        services.AddScoped<PaymentReconciliationService>();
        services.AddScoped<RefundService>();
        services.AddScoped<IPaymentGatewayRouter, PaymentGatewayRouter>();
    }
}
