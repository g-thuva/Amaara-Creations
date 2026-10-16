# Full repository file audit

Baseline: `5b0f639`, 631 tracked files. Every original path is accounted for below. Retained means the original bytes match; updated includes line-ending and documentation corrections. Generated binaries, ignored secrets, uploads, local databases and `.runtime` are excluded. The [result report](full-repository-refactor-result.md) explains the review and verification.

| Original path | Result | Current owner or removal evidence |
| --- | --- | --- |
| `.github/workflows/acceptance.yml` | Retained | `.github/workflows/acceptance.yml` |
| `.github/workflows/pages.yml` | Retained | `.github/workflows/pages.yml` |
| `.github/workflows/release-quality.yml` | Updated | `.github/workflows/release-quality.yml` |
| `.gitignore` | Retained | `.gitignore` |
| `.nvmrc` | Retained | `.nvmrc` |
| `BACKEND_FEATURES.md` | OBSOLETE | Obsolete implementation wishlist; actual features, contracts, module ownership and tested behavior are documented in current architecture/release records. |
| `CONTEXT.md` | Updated | `CONTEXT.md` |
| `DEPLOYMENT.md` | Moved / merged | `docs/runbooks/static-preview.md` |
| `PROJECT_ANALYSIS.md` | OBSOLETE | Obsolete mock-frontend inventory and pre-refactor paths; superseded by tracked architecture and verified reports. |
| `README.md` | Updated | `README.md` |
| `RELEASE_READINESS.md` | Moved / merged | `docs/release/engineering-record.md` |
| `RELEASE_RUNBOOKS.md` | Moved / merged | `docs/runbooks/release-cutover.md` |
| `be/Controllers/AddressesController.cs` | Moved / merged | `be/Api/Controllers/AddressesController.cs` |
| `be/Controllers/AdminCategoriesController.cs` | Moved / merged | `be/Api/Controllers/AdminCategoriesController.cs` |
| `be/Controllers/AdminCollectionsController.cs` | Moved / merged | `be/Api/Controllers/AdminCollectionsController.cs` |
| `be/Controllers/AdminContentController.cs` | Moved / merged | `be/Api/Controllers/AdminContentController.cs` |
| `be/Controllers/AdminController.cs` | Restructured | `be/Api/Controllers/AdminCustomersController.cs`, `be/Api/Controllers/AdminDashboardController.cs` |
| `be/Controllers/AdminCustomBuilderController.cs` | Moved / merged | `be/Api/Controllers/AdminCustomBuilderController.cs` |
| `be/Controllers/AdminCustomDesignsController.cs` | Moved / merged | `be/Api/Controllers/AdminCustomDesignsController.cs` |
| `be/Controllers/AdminInventoryController.cs` | Moved / merged | `be/Api/Controllers/AdminInventoryController.cs` |
| `be/Controllers/AdminMediaController.cs` | Moved / merged | `be/Api/Controllers/AdminMediaController.cs` |
| `be/Controllers/AdminOperationsController.cs` | Restructured | `be/Api/Controllers/AdminAuditController.cs`, `be/Api/Controllers/AdminCapabilitiesController.cs`, `be/Api/Controllers/AdminCustomersController.cs`, `be/Api/Controllers/AdminPaymentsController.cs`, `be/Api/Controllers/AdminReportsController.cs`, `be/Api/Controllers/AdminStaffController.cs` |
| `be/Controllers/AdminProductsController.cs` | Moved / merged | `be/Api/Controllers/AdminProductsController.cs` |
| `be/Controllers/AdminPromotionsController.cs` | Moved / merged | `be/Api/Controllers/AdminPromotionsController.cs` |
| `be/Controllers/AdminSettingsController.cs` | Moved / merged | `be/Api/Controllers/AdminSettingsController.cs` |
| `be/Controllers/AdminShippingController.cs` | Moved / merged | `be/Api/Controllers/AdminShippingController.cs` |
| `be/Controllers/AuthController.cs` | Moved / merged | `be/Api/Controllers/AuthController.cs` |
| `be/Controllers/CartController.cs` | Moved / merged | `be/Api/Controllers/CartController.cs` |
| `be/Controllers/CatalogController.cs` | Moved / merged | `be/Api/Controllers/CatalogController.cs` |
| `be/Controllers/CheckoutController.cs` | Moved / merged | `be/Api/Controllers/CheckoutController.cs` |
| `be/Controllers/ContentController.cs` | Moved / merged | `be/Api/Controllers/ContentController.cs` |
| `be/Controllers/CustomBuilderController.cs` | Moved / merged | `be/Api/Controllers/CustomBuilderController.cs` |
| `be/Controllers/CustomDesignsController.cs` | Moved / merged | `be/Api/Controllers/CustomDesignsController.cs` |
| `be/Controllers/OperationalHealthController.cs` | Moved / merged | `be/Api/Controllers/OperationalHealthController.cs` |
| `be/Controllers/OrdersController.cs` | Moved / merged | `be/Api/Controllers/OrdersController.cs` |
| `be/Controllers/PaymentsController.cs` | Moved / merged | `be/Api/Controllers/PaymentsController.cs` |
| `be/Controllers/ProductsController.cs` | Moved / merged | `be/Api/Controllers/ProductsController.cs` |
| `be/Controllers/PublicSeoController.cs` | Moved / merged | `be/Api/Controllers/PublicSeoController.cs` |
| `be/Controllers/ReviewsController.cs` | Moved / merged | `be/Api/Controllers/ReviewsController.cs` |
| `be/Controllers/UploadController.cs` | Moved / merged | `be/Api/Controllers/UploadController.cs` |
| `be/Controllers/UsersController.cs` | Moved / merged | `be/Api/Controllers/UsersController.cs` |
| `be/Controllers/WishlistController.cs` | Moved / merged | `be/Api/Controllers/WishlistController.cs` |
| `be/DTOs/Address/AddressDtos.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/AddressDtos.cs` |
| `be/DTOs/Admin/CatalogAdminDtos.cs` | Moved / merged | `be/Application/Features/Catalog/Contracts/CatalogAdminDtos.cs` |
| `be/DTOs/Admin/CatalogAdminResponses.cs` | Moved / merged | `be/Application/Features/Catalog/Contracts/CatalogAdminResponses.cs` |
| `be/DTOs/Admin/CustomerDetailResponse.cs` | Moved / merged | `be/Application/Features/Customers/Contracts/CustomerDetailResponse.cs` |
| `be/DTOs/Admin/CustomerResponse.cs` | Moved / merged | `be/Application/Features/Customers/Contracts/CustomerResponse.cs` |
| `be/DTOs/Admin/CustomerStatsResponse.cs` | Moved / merged | `be/Application/Features/Customers/Contracts/CustomerStatsResponse.cs` |
| `be/DTOs/Admin/DashboardStatsResponse.cs` | Moved / merged | `be/Application/Features/Reporting/Contracts/DashboardStatsResponse.cs` |
| `be/DTOs/Admin/OrderStatsResponse.cs` | Moved / merged | `be/Application/Features/Reporting/Contracts/OrderStatsResponse.cs` |
| `be/DTOs/Admin/ProductStatsResponse.cs` | Moved / merged | `be/Application/Features/Reporting/Contracts/ProductStatsResponse.cs` |
| `be/DTOs/Admin/RecentOrderResponse.cs` | Moved / merged | `be/Application/Features/Reporting/Contracts/RecentOrderResponse.cs` |
| `be/DTOs/Admin/RevenueStatsResponse.cs` | Moved / merged | `be/Application/Features/Reporting/Contracts/RevenueStatsResponse.cs` |
| `be/DTOs/Auth/AuthResponse.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/AuthResponse.cs` |
| `be/DTOs/Auth/ChangePasswordRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/ChangePasswordRequest.cs` |
| `be/DTOs/Auth/ConfirmEmailRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/ConfirmEmailRequest.cs` |
| `be/DTOs/Auth/ForgotPasswordRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/ForgotPasswordRequest.cs` |
| `be/DTOs/Auth/LoginRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/LoginRequest.cs` |
| `be/DTOs/Auth/RefreshTokenRequest.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Auth/RegisterRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/RegisterRequest.cs` |
| `be/DTOs/Auth/ResendConfirmationRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/ResendConfirmationRequest.cs` |
| `be/DTOs/Auth/ResetPasswordRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/ResetPasswordRequest.cs` |
| `be/DTOs/Cart/AddToCartRequest.cs` | Moved / merged | `be/Application/Features/Cart/Contracts/AddToCartRequest.cs` |
| `be/DTOs/Cart/CartItemResponse.cs` | Moved / merged | `be/Application/Features/Cart/Contracts/CartItemResponse.cs` |
| `be/DTOs/Cart/CartResponse.cs` | Moved / merged | `be/Application/Features/Cart/Contracts/CartResponse.cs` |
| `be/DTOs/Cart/UpdateCartItemRequest.cs` | Moved / merged | `be/Application/Features/Cart/Contracts/UpdateCartItemRequest.cs` |
| `be/DTOs/Checkout/CheckoutDtos.cs` | Moved / merged | `be/Application/Features/Checkout/Contracts/CheckoutDtos.cs` |
| `be/DTOs/Checkout/PaymentStatusDto.cs` | Restructured | `be/Application/Features/Payments/Contracts/PaymentContracts.cs` |
| `be/DTOs/Checkout/ShippingAdminDtos.cs` | Restructured | `be/Application/Features/Promotions/Contracts/PromotionContracts.cs`, `be/Application/Features/Shipping/Contracts/ShippingContracts.cs` |
| `be/DTOs/Common/PagedResult.cs` | Moved / merged | `be/Application/Common/PagedResult.cs` |
| `be/DTOs/Common/PaginationQuery.cs` | Moved / merged | `be/Application/Common/PaginationQuery.cs` |
| `be/DTOs/CustomBuilder/CustomBuilderDtos.cs` | Moved / merged | `be/Application/Features/CustomDesigns/Contracts/CustomBuilderDtos.cs` |
| `be/DTOs/Order/CreateOrderRequest.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Order/OrderItemResponse.cs` | Moved / merged | `be/Application/Features/Orders/Contracts/OrderItemResponse.cs` |
| `be/DTOs/Order/OrderResponse.cs` | Moved / merged | `be/Application/Features/Orders/Contracts/OrderResponse.cs` |
| `be/DTOs/Order/UpdateOrderStatusRequest.cs` | Moved / merged | `be/Application/Features/Orders/Contracts/UpdateOrderStatusRequest.cs` |
| `be/DTOs/Product/CreateProductRequest.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Product/ProductListResponse.cs` | Moved / merged | `be/Application/Features/Catalog/Contracts/ProductListResponse.cs` |
| `be/DTOs/Product/ProductResponse.cs` | Moved / merged | `be/Application/Features/Catalog/Contracts/ProductResponse.cs` |
| `be/DTOs/Product/UpdateProductRequest.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Review/CreateReviewRequest.cs` | Moved / merged | `be/Application/Features/Reviews/Contracts/CreateReviewRequest.cs` |
| `be/DTOs/Review/ProductReviewsResponse.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Review/ReviewResponse.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Review/ReviewStatsResponse.cs` | DEAD CODE | No compiled, route, schema, reflection-string, script or test consumer; superseded by active feature contracts. |
| `be/DTOs/Review/UpdateReviewRequest.cs` | Moved / merged | `be/Application/Features/Reviews/Contracts/UpdateReviewRequest.cs` |
| `be/DTOs/User/UpdateAvatarRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/UpdateAvatarRequest.cs` |
| `be/DTOs/User/UpdateProfileRequest.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/UpdateProfileRequest.cs` |
| `be/DTOs/User/UserProfileResponse.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/UserProfileResponse.cs` |
| `be/DTOs/User/UserStatsResponse.cs` | Moved / merged | `be/Application/Features/Identity/Contracts/UserStatsResponse.cs` |
| `be/DTOs/Wishlist/AddToWishlistRequest.cs` | Moved / merged | `be/Application/Features/Wishlist/Contracts/AddToWishlistRequest.cs` |
| `be/DTOs/Wishlist/WishlistItemResponse.cs` | Moved / merged | `be/Application/Features/Wishlist/Contracts/WishlistItemResponse.cs` |
| `be/DTOs/Wishlist/WishlistResponse.cs` | Moved / merged | `be/Application/Features/Wishlist/Contracts/WishlistResponse.cs` |
| `be/Data/ApplicationDbContext.CustomBuilder.cs` | Restructured | `be/Infrastructure/Persistence/Configurations/AuditConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CartConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CatalogConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CmsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CustomDesignsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/IdentityConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/InventoryConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/NotificationsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/OrdersConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PaymentsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PromotionsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ReviewsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ShippingConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/WishlistConfiguration.cs` |
| `be/Data/ApplicationDbContext.Foundation.cs` | Restructured | `be/Infrastructure/Persistence/Configurations/AuditConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CartConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CatalogConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CmsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CustomDesignsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/IdentityConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/InventoryConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/NotificationsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/OrdersConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PaymentsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PromotionsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ReviewsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ShippingConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/WishlistConfiguration.cs` |
| `be/Data/ApplicationDbContext.Operations.cs` | Restructured | `be/Infrastructure/Persistence/Configurations/AuditConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CartConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CatalogConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CmsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CustomDesignsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/IdentityConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/InventoryConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/NotificationsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/OrdersConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PaymentsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PromotionsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ReviewsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ShippingConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/WishlistConfiguration.cs` |
| `be/Data/ApplicationDbContext.Phase6.cs` | Restructured | `be/Infrastructure/Persistence/Configurations/AuditConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CartConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CatalogConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CmsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/CustomDesignsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/IdentityConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/InventoryConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/NotificationsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/OrdersConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PaymentsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/PromotionsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ReviewsConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/ShippingConfiguration.cs`, `be/Infrastructure/Persistence/Configurations/WishlistConfiguration.cs` |
| `be/Data/ApplicationDbContext.cs` | Moved / merged | `be/Infrastructure/Persistence/ApplicationDbContext.cs` |
| `be/Infrastructure/AnonymousSwaggerOperationFilter.cs` | Moved / merged | `be/Api/OpenApi/AnonymousSwaggerOperationFilter.cs` |
| `be/Infrastructure/CommerceExceptionMiddleware.cs` | Moved / merged | `be/Api/Middleware/CommerceExceptionMiddleware.cs` |
| `be/Infrastructure/CorrelationIdMiddleware.cs` | Moved / merged | `be/Api/Middleware/CorrelationIdMiddleware.cs` |
| `be/Infrastructure/ReleaseTelemetryMiddleware.cs` | Moved / merged | `be/Api/Middleware/ReleaseTelemetryMiddleware.cs` |
| `be/Migrations/20251101195840_InitialCreate.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101195840_InitialCreate.Designer.cs` |
| `be/Migrations/20251101195840_InitialCreate.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101195840_InitialCreate.cs` |
| `be/Migrations/20251101200718_InitialIdentity.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101200718_InitialIdentity.Designer.cs` |
| `be/Migrations/20251101200718_InitialIdentity.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101200718_InitialIdentity.cs` |
| `be/Migrations/20251101203958_AddProducts.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101203958_AddProducts.Designer.cs` |
| `be/Migrations/20251101203958_AddProducts.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101203958_AddProducts.cs` |
| `be/Migrations/20251101210010_AddCartItems.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101210010_AddCartItems.Designer.cs` |
| `be/Migrations/20251101210010_AddCartItems.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251101210010_AddCartItems.cs` |
| `be/Migrations/20251102095431_AddOrdersAndOrderItems.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102095431_AddOrdersAndOrderItems.Designer.cs` |
| `be/Migrations/20251102095431_AddOrdersAndOrderItems.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102095431_AddOrdersAndOrderItems.cs` |
| `be/Migrations/20251102101324_AddWishlistItems.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102101324_AddWishlistItems.Designer.cs` |
| `be/Migrations/20251102101324_AddWishlistItems.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102101324_AddWishlistItems.cs` |
| `be/Migrations/20251102102538_AddReviews.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102102538_AddReviews.Designer.cs` |
| `be/Migrations/20251102102538_AddReviews.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20251102102538_AddReviews.cs` |
| `be/Migrations/20261003072610_FoundationFoundationDataModel.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261003072610_FoundationFoundationDataModel.Designer.cs` |
| `be/Migrations/20261003072610_FoundationFoundationDataModel.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261003072610_FoundationFoundationDataModel.cs` |
| `be/Migrations/20261005103855_CatalogueMediaCatalogueMediaCms.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261005103855_CatalogueMediaCatalogueMediaCms.Designer.cs` |
| `be/Migrations/20261005103855_CatalogueMediaCatalogueMediaCms.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261005103855_CatalogueMediaCatalogueMediaCms.cs` |
| `be/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.Designer.cs` |
| `be/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.cs` |
| `be/Migrations/20261006202203_Phase6CheckoutPaymentsShipping.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261006202203_Phase6CheckoutPaymentsShipping.Designer.cs` |
| `be/Migrations/20261006202203_Phase6CheckoutPaymentsShipping.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261006202203_Phase6CheckoutPaymentsShipping.cs` |
| `be/Migrations/20261011173054_Phase6TransactionLifecycle.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261011173054_Phase6TransactionLifecycle.Designer.cs` |
| `be/Migrations/20261011173054_Phase6TransactionLifecycle.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261011173054_Phase6TransactionLifecycle.cs` |
| `be/Migrations/20261014112403_Phase6ZahlsTestCurrency.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261014112403_Phase6ZahlsTestCurrency.Designer.cs` |
| `be/Migrations/20261014112403_Phase6ZahlsTestCurrency.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261014112403_Phase6ZahlsTestCurrency.cs` |
| `be/Migrations/20261015185935_Phase7AdminOperations.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015185935_Phase7AdminOperations.Designer.cs` |
| `be/Migrations/20261015185935_Phase7AdminOperations.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015185935_Phase7AdminOperations.cs` |
| `be/Migrations/20261015193736_Phase7ReviewDefaults.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015193736_Phase7ReviewDefaults.Designer.cs` |
| `be/Migrations/20261015193736_Phase7ReviewDefaults.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015193736_Phase7ReviewDefaults.cs` |
| `be/Migrations/20261015194809_Phase7NormalizeLegacyEvidence.Designer.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015194809_Phase7NormalizeLegacyEvidence.Designer.cs` |
| `be/Migrations/20261015194809_Phase7NormalizeLegacyEvidence.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/20261015194809_Phase7NormalizeLegacyEvidence.cs` |
| `be/Migrations/ApplicationDbContextModelSnapshot.cs` | Moved / merged | `be/Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` |
| `be/Models/AuditLog.cs` | Restructured | `be/Domain/Audit/AuditLog.cs` |
| `be/Models/CartItem.cs` | Restructured | `be/Domain/Cart/CartItem.cs`, `be/Domain/Cart/CartStates.cs` |
| `be/Models/CatalogModels.cs` | Restructured | `be/Domain/Catalog/Category.cs`, `be/Domain/Catalog/Collection.cs`, `be/Domain/Catalog/ProductCollection.cs`, `be/Domain/Catalog/ProductMedia.cs`, `be/Domain/Catalog/ProductVariant.cs` |
| `be/Models/CatalogueMediaContentModels.cs` | Restructured | `be/Domain/Cms/CmsPage.cs`, `be/Domain/Cms/CmsSection.cs`, `be/Domain/Cms/SiteSetting.cs`, `be/Domain/Inventory/InventoryTransaction.cs` |
| `be/Models/CommerceExtensions.cs` | Restructured | `be/Domain/Notifications/CommerceNotification.cs`, `be/Domain/Orders/CheckoutAttempt.cs`, `be/Domain/Orders/Order.cs`, `be/Domain/Orders/OrderItem.cs`, `be/Domain/Payments/Payment.cs`, `be/Domain/Payments/PaymentWebhookEvent.cs`, `be/Domain/Payments/Refund.cs`, `be/Domain/Promotions/Promotion.cs`, `be/Domain/Promotions/PromotionRedemption.cs`, `be/Domain/Shipping/Shipment.cs`, `be/Domain/Shipping/ShippingMethod.cs`, `be/Domain/Shipping/ShippingZone.cs` |
| `be/Models/CustomBuilderModels.cs` | Restructured | `be/Domain/CustomDesigns/CustomBuilderConfigurationVersion.cs`, `be/Domain/CustomDesigns/CustomBuilderOption.cs`, `be/Domain/CustomDesigns/CustomDesignProofRevision.cs`, `be/Domain/CustomDesigns/CustomDesignsStates.cs` |
| `be/Models/CustomDesignModels.cs` | Restructured | `be/Domain/CustomDesigns/CustomDesign.cs`, `be/Domain/CustomDesigns/CustomDesignAsset.cs`, `be/Domain/CustomDesigns/CustomDesignsStates.cs` |
| `be/Models/CustomerFoundationModels.cs` | Restructured | `be/Domain/Identity/Address.cs`, `be/Domain/Identity/RefreshSession.cs` |
| `be/Models/Order.cs` | Restructured | `be/Domain/Orders/Order.cs`, `be/Domain/Orders/OrdersStates.cs` |
| `be/Models/OrderFoundationModels.cs` | Restructured | `be/Domain/Orders/OrderStatusHistory.cs`, `be/Domain/Orders/OrdersStates.cs`, `be/Domain/Payments/Payment.cs`, `be/Domain/Payments/PaymentsStates.cs`, `be/Domain/Promotions/Promotion.cs`, `be/Domain/Promotions/PromotionsStates.cs`, `be/Domain/Shipping/Shipment.cs`, `be/Domain/Shipping/ShippingMethod.cs`, `be/Domain/Shipping/ShippingStates.cs`, `be/Domain/Shipping/ShippingZone.cs` |
| `be/Models/OrderItem.cs` | Restructured | `be/Domain/Orders/OrderItem.cs` |
| `be/Models/OrderPhase6Models.cs` | Restructured | `be/Domain/Inventory/InventoryReservation.cs`, `be/Domain/Orders/CheckoutAttempt.cs`, `be/Domain/Payments/Refund.cs` |
| `be/Models/Product.cs` | Restructured | `be/Domain/Catalog/Product.cs` |
| `be/Models/Review.cs` | Restructured | `be/Domain/Reviews/Review.cs` |
| `be/Models/User.cs` | Restructured | `be/Domain/Identity/User.cs` |
| `be/Models/WishlistItem.cs` | Restructured | `be/Domain/Wishlist/WishlistItem.cs` |
| `be/Program.cs` | Updated | `be/Program.cs` |
| `be/Properties/launchSettings.json` | Retained | `be/Properties/launchSettings.json` |
| `be/Security/AuthPolicies.cs` | Restructured | `be/Application/Features/Identity/AppPolicies.cs`, `be/Domain/Identity/AppRoles.cs` |
| `be/Services/AuditService.cs` | Moved / merged | `be/Infrastructure/Observability/AuditService.cs` |
| `be/Services/Checkout/CheckoutPricingService.cs` | Moved / merged | `be/Application/Features/Checkout/CheckoutPricingService.cs` |
| `be/Services/Checkout/CheckoutService.cs` | Moved / merged | `be/Application/Features/Checkout/CheckoutService.cs` |
| `be/Services/Checkout/CommerceOptions.cs` | Moved / merged | `be/Application/Features/Checkout/CommerceOptions.cs` |
| `be/Services/Checkout/CommerceTransaction.cs` | Moved / merged | `be/Infrastructure/Persistence/CommerceTransaction.cs` |
| `be/Services/Checkout/InventoryReservationService.cs` | Moved / merged | `be/Application/Features/Inventory/InventoryReservationService.cs` |
| `be/Services/Checkout/OrderLifecycleService.cs` | Moved / merged | `be/Application/Features/Orders/OrderLifecycleService.cs` |
| `be/Services/Checkout/PromotionService.cs` | Moved / merged | `be/Application/Features/Promotions/PromotionService.cs` |
| `be/Services/Checkout/ShippingService.cs` | Moved / merged | `be/Application/Features/Shipping/ShippingService.cs` |
| `be/Services/CustomBuilderDevelopmentSeeder.cs` | Moved / merged | `be/Infrastructure/Persistence/Seed/CustomBuilderDevelopmentSeeder.cs` |
| `be/Services/CustomBuilderMappings.cs` | Moved / merged | `be/Application/Features/CustomDesigns/CustomBuilderMappings.cs` |
| `be/Services/CustomStickerPricingService.cs` | Moved / merged | `be/Application/Features/CustomDesigns/CustomStickerPricingService.cs` |
| `be/Services/DevelopmentEmailService.cs` | Moved / merged | `be/Infrastructure/Email/DevelopmentEmailService.cs` |
| `be/Services/IAuditService.cs` | Moved / merged | `be/Application/Abstractions/Audit/IAuditService.cs` |
| `be/Services/IEmailService.cs` | Moved / merged | `be/Application/Abstractions/Email/IEmailService.cs` |
| `be/Services/IMediaStorageService.cs` | Moved / merged | `be/Application/Abstractions/Storage/IMediaStorageService.cs` |
| `be/Services/IRefreshSessionService.cs` | Moved / merged | `be/Application/Abstractions/Identity/IRefreshSessionService.cs` |
| `be/Services/ITokenService.cs` | Moved / merged | `be/Application/Abstractions/Identity/ITokenService.cs` |
| `be/Services/InventoryOperations.cs` | Moved / merged | `be/Application/Features/Inventory/InventoryOperations.cs` |
| `be/Services/LocalMediaStorageService.cs` | Moved / merged | `be/Infrastructure/Storage/LocalMediaStorageService.cs` |
| `be/Services/Payments/CommerceMaintenanceService.cs` | Moved / merged | `be/Infrastructure/BackgroundJobs/CommerceMaintenanceService.cs` |
| `be/Services/Payments/CommerceNotifications.cs` | Moved / merged | `be/Application/Features/Notifications/CommerceNotifications.cs` |
| `be/Services/Payments/DatatransOptions.cs` | Moved / merged | `be/Infrastructure/Payments/Datatrans/DatatransOptions.cs` |
| `be/Services/Payments/DatatransPaymentGateway.cs` | Moved / merged | `be/Infrastructure/Payments/Datatrans/DatatransPaymentGateway.cs` |
| `be/Services/Payments/DatatransWebhookVerifier.cs` | Moved / merged | `be/Infrastructure/Payments/Datatrans/DatatransWebhookVerifier.cs` |
| `be/Services/Payments/HostedPaymentUrlPolicy.cs` | Moved / merged | `be/Application/Features/Payments/HostedPaymentUrlPolicy.cs` |
| `be/Services/Payments/IPaymentGateway.cs` | Moved / merged | `be/Application/Abstractions/Payments/IPaymentGateway.cs` |
| `be/Services/Payments/PaymentGatewayRouter.cs` | Moved / merged | `be/Application/Features/Payments/PaymentGatewayRouter.cs` |
| `be/Services/Payments/PaymentReconciliationService.cs` | Moved / merged | `be/Application/Features/Payments/PaymentReconciliationService.cs` |
| `be/Services/Payments/RefundService.cs` | Moved / merged | `be/Application/Features/Payments/RefundService.cs` |
| `be/Services/Payments/ZahlsOptions.cs` | Moved / merged | `be/Infrastructure/Payments/Zahls/ZahlsOptions.cs` |
| `be/Services/Payments/ZahlsPaymentGateway.cs` | Moved / merged | `be/Infrastructure/Payments/Zahls/ZahlsPaymentGateway.cs` |
| `be/Services/Payments/ZahlsWebhookVerifier.cs` | Moved / merged | `be/Infrastructure/Payments/Zahls/ZahlsWebhookVerifier.cs` |
| `be/Services/RefreshSessionService.cs` | Moved / merged | `be/Infrastructure/Identity/RefreshSessionService.cs` |
| `be/Services/SmtpEmailService.cs` | Moved / merged | `be/Infrastructure/Email/SmtpEmailService.cs` |
| `be/Services/TokenService.cs` | Moved / merged | `be/Infrastructure/Identity/TokenService.cs` |
| `be/appsettings.Development.Local.example.json` | Retained | `be/appsettings.Development.Local.example.json` |
| `be/appsettings.json` | Retained | `be/appsettings.json` |
| `be/be.csproj` | Retained | `be/be.csproj` |
| `be/be.http` | Moved / merged | `be/Api/requests.http` |
| `client/.env.example` | Retained | `client/.env.example` |
| `client/.gitignore` | Retained | `client/.gitignore` |
| `client/README.md` | Updated | `client/README.md` |
| `client/e2e/accessibility.ts` | Retained | `client/e2e/accessibility.ts` |
| `client/e2e/account/auth-session.spec.ts` | Retained | `client/e2e/account/auth-session.spec.ts` |
| `client/e2e/account/customer-lifecycle.spec.ts` | Retained | `client/e2e/account/customer-lifecycle.spec.ts` |
| `client/e2e/account/responsive.spec.ts` | Retained | `client/e2e/account/responsive.spec.ts` |
| `client/e2e/admin/accessibility.spec.ts` | Retained | `client/e2e/admin/accessibility.spec.ts` |
| `client/e2e/admin/architecture.spec.ts` | Retained | `client/e2e/admin/architecture.spec.ts` |
| `client/e2e/admin/catalog.spec.ts` | Retained | `client/e2e/admin/catalog.spec.ts` |
| `client/e2e/admin/checkout-payments.spec.ts` | Retained | `client/e2e/admin/checkout-payments.spec.ts` |
| `client/e2e/admin/operations.spec.ts` | Retained | `client/e2e/admin/operations.spec.ts` |
| `client/e2e/admin/payments.spec.ts` | Retained | `client/e2e/admin/payments.spec.ts` |
| `client/e2e/checkout/checkout.spec.ts` | Retained | `client/e2e/checkout/checkout.spec.ts` |
| `client/e2e/checkout/checkoutFixture.ts` | Retained | `client/e2e/checkout/checkoutFixture.ts` |
| `client/e2e/checkout/commerce.spec.ts` | Retained | `client/e2e/checkout/commerce.spec.ts` |
| `client/e2e/fixtures.ts` | Retained | `client/e2e/fixtures.ts` |
| `client/e2e/production/release-production.spec.ts` | Retained | `client/e2e/production/release-production.spec.ts` |
| `client/e2e/storefront/accessibility.spec.ts` | Retained | `client/e2e/storefront/accessibility.spec.ts` |
| `client/e2e/storefront/artwork.spec.ts` | Retained | `client/e2e/storefront/artwork.spec.ts` |
| `client/e2e/storefront/custom-design-state.spec.ts` | Retained | `client/e2e/storefront/custom-design-state.spec.ts` |
| `client/e2e/storefront/custom-design.spec.ts` | Retained | `client/e2e/storefront/custom-design.spec.ts` |
| `client/e2e/storefront/public.spec.ts` | Retained | `client/e2e/storefront/public.spec.ts` |
| `client/eslint.config.js` | Retained | `client/eslint.config.js` |
| `client/index.html` | Retained | `client/index.html` |
| `client/package-lock.json` | Retained | `client/package-lock.json` |
| `client/package.json` | Retained | `client/package.json` |
| `client/playwright.config.ts` | Retained | `client/playwright.config.ts` |
| `client/public/favicon.svg` | Retained | `client/public/favicon.svg` |
| `client/scripts/check-performance.mjs` | Retained | `client/scripts/check-performance.mjs` |
| `client/scripts/run-production-browser.mjs` | Retained | `client/scripts/run-production-browser.mjs` |
| `client/src/app/App.jsx` | Retained | `client/src/app/App.jsx` |
| `client/src/app/config/navigation.js` | Retained | `client/src/app/config/navigation.js` |
| `client/src/app/config/permissions.js` | Retained | `client/src/app/config/permissions.js` |
| `client/src/app/guards/AdminGuard.jsx` | Retained | `client/src/app/guards/AdminGuard.jsx` |
| `client/src/app/guards/AuthGuard.jsx` | Retained | `client/src/app/guards/AuthGuard.jsx` |
| `client/src/app/layout/Footer.jsx` | Retained | `client/src/app/layout/Footer.jsx` |
| `client/src/app/layout/Navbar.jsx` | Retained | `client/src/app/layout/Navbar.jsx` |
| `client/src/app/layout/storefrontLayout.css` | Retained | `client/src/app/layout/storefrontLayout.css` |
| `client/src/app/pages/NotFoundPage.jsx` | Retained | `client/src/app/pages/NotFoundPage.jsx` |
| `client/src/app/providers/AppProviders.jsx` | Retained | `client/src/app/providers/AppProviders.jsx` |
| `client/src/app/providers/StorefrontProviders.jsx` | Retained | `client/src/app/providers/StorefrontProviders.jsx` |
| `client/src/app/routes/AccountRoutes.jsx` | Retained | `client/src/app/routes/AccountRoutes.jsx` |
| `client/src/app/routes/AdminRoutes.jsx` | Retained | `client/src/app/routes/AdminRoutes.jsx` |
| `client/src/app/routes/AppRoutes.jsx` | Retained | `client/src/app/routes/AppRoutes.jsx` |
| `client/src/app/routes/PublicRoutes.jsx` | Retained | `client/src/app/routes/PublicRoutes.jsx` |
| `client/src/app/seo/Seo.jsx` | Retained | `client/src/app/seo/Seo.jsx` |
| `client/src/app/seo/seo.js` | Retained | `client/src/app/seo/seo.js` |
| `client/src/app/seo/seo.test.js` | Retained | `client/src/app/seo/seo.test.js` |
| `client/src/app/storefront.test.jsx` | Retained | `client/src/app/storefront.test.jsx` |
| `client/src/features/account/account.css` | Retained | `client/src/features/account/account.css` |
| `client/src/features/account/accountApi.js` | Retained | `client/src/features/account/accountApi.js` |
| `client/src/features/account/addressApi.js` | Retained | `client/src/features/account/addressApi.js` |
| `client/src/features/account/addressForm.js` | Retained | `client/src/features/account/addressForm.js` |
| `client/src/features/account/avatarApi.js` | Retained | `client/src/features/account/avatarApi.js` |
| `client/src/features/account/components/AccountLayout.jsx` | Retained | `client/src/features/account/components/AccountLayout.jsx` |
| `client/src/features/account/components/AccountSecurityForm.jsx` | Retained | `client/src/features/account/components/AccountSecurityForm.jsx` |
| `client/src/features/account/components/AddressForm.jsx` | Retained | `client/src/features/account/components/AddressForm.jsx` |
| `client/src/features/account/components/ProfileForm.jsx` | Retained | `client/src/features/account/components/ProfileForm.jsx` |
| `client/src/features/account/hooks/useAccountSecurity.js` | Retained | `client/src/features/account/hooks/useAccountSecurity.js` |
| `client/src/features/account/hooks/useAddresses.js` | Retained | `client/src/features/account/hooks/useAddresses.js` |
| `client/src/features/account/pages/AccountSecurityPage.jsx` | Retained | `client/src/features/account/pages/AccountSecurityPage.jsx` |
| `client/src/features/account/pages/AddressesPage.jsx` | Retained | `client/src/features/account/pages/AddressesPage.jsx` |
| `client/src/features/account/pages/ProfilePage.jsx` | Retained | `client/src/features/account/pages/ProfilePage.jsx` |
| `client/src/features/admin/app/AdminLanding.jsx` | Retained | `client/src/features/admin/app/AdminLanding.jsx` |
| `client/src/features/admin/app/adminNavigation.js` | Retained | `client/src/features/admin/app/adminNavigation.js` |
| `client/src/features/admin/features/audit/AuditFilters.jsx` | Retained | `client/src/features/admin/features/audit/AuditFilters.jsx` |
| `client/src/features/admin/features/audit/AuditPage.jsx` | Retained | `client/src/features/admin/features/audit/AuditPage.jsx` |
| `client/src/features/admin/features/audit/AuditTable.jsx` | Retained | `client/src/features/admin/features/audit/AuditTable.jsx` |
| `client/src/features/admin/features/audit/auditApi.js` | Retained | `client/src/features/admin/features/audit/auditApi.js` |
| `client/src/features/admin/features/audit/useAudit.js` | Retained | `client/src/features/admin/features/audit/useAudit.js` |
| `client/src/features/admin/features/builder-configuration/BuilderConfigurationPage.jsx` | Retained | `client/src/features/admin/features/builder-configuration/BuilderConfigurationPage.jsx` |
| `client/src/features/admin/features/builder-configuration/BuilderOptionsEditor.jsx` | Retained | `client/src/features/admin/features/builder-configuration/BuilderOptionsEditor.jsx` |
| `client/src/features/admin/features/builder-configuration/BuilderPricingEditor.jsx` | Retained | `client/src/features/admin/features/builder-configuration/BuilderPricingEditor.jsx` |
| `client/src/features/admin/features/builder-configuration/BuilderVersionSummary.jsx` | Retained | `client/src/features/admin/features/builder-configuration/BuilderVersionSummary.jsx` |
| `client/src/features/admin/features/builder-configuration/builderConfigurationApi.js` | Retained | `client/src/features/admin/features/builder-configuration/builderConfigurationApi.js` |
| `client/src/features/admin/features/builder-configuration/builderForm.js` | Retained | `client/src/features/admin/features/builder-configuration/builderForm.js` |
| `client/src/features/admin/features/builder-configuration/useBuilderConfiguration.js` | Retained | `client/src/features/admin/features/builder-configuration/useBuilderConfiguration.js` |
| `client/src/features/admin/features/categories/CategoriesPage.jsx` | Retained | `client/src/features/admin/features/categories/CategoriesPage.jsx` |
| `client/src/features/admin/features/categories/CategoriesTable.jsx` | Retained | `client/src/features/admin/features/categories/CategoriesTable.jsx` |
| `client/src/features/admin/features/categories/CategoryForm.jsx` | Retained | `client/src/features/admin/features/categories/CategoryForm.jsx` |
| `client/src/features/admin/features/categories/categoriesApi.js` | Retained | `client/src/features/admin/features/categories/categoriesApi.js` |
| `client/src/features/admin/features/categories/categoriesForm.js` | Retained | `client/src/features/admin/features/categories/categoriesForm.js` |
| `client/src/features/admin/features/categories/useCategories.js` | Retained | `client/src/features/admin/features/categories/useCategories.js` |
| `client/src/features/admin/features/collections/CollectionForm.jsx` | Retained | `client/src/features/admin/features/collections/CollectionForm.jsx` |
| `client/src/features/admin/features/collections/CollectionsPage.jsx` | Retained | `client/src/features/admin/features/collections/CollectionsPage.jsx` |
| `client/src/features/admin/features/collections/CollectionsTable.jsx` | Retained | `client/src/features/admin/features/collections/CollectionsTable.jsx` |
| `client/src/features/admin/features/collections/collectionsApi.js` | Retained | `client/src/features/admin/features/collections/collectionsApi.js` |
| `client/src/features/admin/features/collections/collectionsForm.js` | Retained | `client/src/features/admin/features/collections/collectionsForm.js` |
| `client/src/features/admin/features/collections/useCollections.js` | Retained | `client/src/features/admin/features/collections/useCollections.js` |
| `client/src/features/admin/features/content/ContentForm.jsx` | Retained | `client/src/features/admin/features/content/ContentForm.jsx` |
| `client/src/features/admin/features/content/ContentPage.jsx` | Retained | `client/src/features/admin/features/content/ContentPage.jsx` |
| `client/src/features/admin/features/content/ContentPagesTable.jsx` | Retained | `client/src/features/admin/features/content/ContentPagesTable.jsx` |
| `client/src/features/admin/features/content/contentApi.js` | Retained | `client/src/features/admin/features/content/contentApi.js` |
| `client/src/features/admin/features/content/contentForm.js` | Retained | `client/src/features/admin/features/content/contentForm.js` |
| `client/src/features/admin/features/content/useContent.js` | Retained | `client/src/features/admin/features/content/useContent.js` |
| `client/src/features/admin/features/customers/CustomerActivity.jsx` | Retained | `client/src/features/admin/features/customers/CustomerActivity.jsx` |
| `client/src/features/admin/features/customers/CustomerAddresses.jsx` | Retained | `client/src/features/admin/features/customers/CustomerAddresses.jsx` |
| `client/src/features/admin/features/customers/CustomerDetailPage.jsx` | Retained | `client/src/features/admin/features/customers/CustomerDetailPage.jsx` |
| `client/src/features/admin/features/customers/CustomerSummary.jsx` | Retained | `client/src/features/admin/features/customers/CustomerSummary.jsx` |
| `client/src/features/admin/features/customers/CustomersPage.jsx` | Retained | `client/src/features/admin/features/customers/CustomersPage.jsx` |
| `client/src/features/admin/features/customers/CustomersTable.jsx` | Retained | `client/src/features/admin/features/customers/CustomersTable.jsx` |
| `client/src/features/admin/features/customers/customersApi.js` | Retained | `client/src/features/admin/features/customers/customersApi.js` |
| `client/src/features/admin/features/customers/useCustomers.js` | Retained | `client/src/features/admin/features/customers/useCustomers.js` |
| `client/src/features/admin/features/dashboard/Dashboard.module.css` | Retained | `client/src/features/admin/features/dashboard/Dashboard.module.css` |
| `client/src/features/admin/features/dashboard/DashboardPage.jsx` | Retained | `client/src/features/admin/features/dashboard/DashboardPage.jsx` |
| `client/src/features/admin/features/dashboard/DashboardSummary.jsx` | Retained | `client/src/features/admin/features/dashboard/DashboardSummary.jsx` |
| `client/src/features/admin/features/dashboard/RecentOrdersTable.jsx` | Retained | `client/src/features/admin/features/dashboard/RecentOrdersTable.jsx` |
| `client/src/features/admin/features/dashboard/TopProductsList.jsx` | Retained | `client/src/features/admin/features/dashboard/TopProductsList.jsx` |
| `client/src/features/admin/features/dashboard/dashboardApi.js` | Retained | `client/src/features/admin/features/dashboard/dashboardApi.js` |
| `client/src/features/admin/features/dashboard/useDashboard.js` | Retained | `client/src/features/admin/features/dashboard/useDashboard.js` |
| `client/src/features/admin/features/inventory/InventoryAdjustmentForm.jsx` | Retained | `client/src/features/admin/features/inventory/InventoryAdjustmentForm.jsx` |
| `client/src/features/admin/features/inventory/InventoryMovementsTable.jsx` | Retained | `client/src/features/admin/features/inventory/InventoryMovementsTable.jsx` |
| `client/src/features/admin/features/inventory/InventoryPage.jsx` | Retained | `client/src/features/admin/features/inventory/InventoryPage.jsx` |
| `client/src/features/admin/features/inventory/InventorySections.test.jsx` | Retained | `client/src/features/admin/features/inventory/InventorySections.test.jsx` |
| `client/src/features/admin/features/inventory/StockBalancesTable.jsx` | Retained | `client/src/features/admin/features/inventory/StockBalancesTable.jsx` |
| `client/src/features/admin/features/inventory/inventoryApi.js` | Retained | `client/src/features/admin/features/inventory/inventoryApi.js` |
| `client/src/features/admin/features/inventory/inventoryContracts.ts` | Retained | `client/src/features/admin/features/inventory/inventoryContracts.ts` |
| `client/src/features/admin/features/inventory/useInventory.js` | Retained | `client/src/features/admin/features/inventory/useInventory.js` |
| `client/src/features/admin/features/media/MediaPage.jsx` | Retained | `client/src/features/admin/features/media/MediaPage.jsx` |
| `client/src/features/admin/features/media/MediaTable.jsx` | Retained | `client/src/features/admin/features/media/MediaTable.jsx` |
| `client/src/features/admin/features/media/mediaApi.js` | Retained | `client/src/features/admin/features/media/mediaApi.js` |
| `client/src/features/admin/features/orders/OrderDetailPage.jsx` | Retained | `client/src/features/admin/features/orders/OrderDetailPage.jsx` |
| `client/src/features/admin/features/orders/OrderFilters.jsx` | Retained | `client/src/features/admin/features/orders/OrderFilters.jsx` |
| `client/src/features/admin/features/orders/OrderItems.jsx` | Retained | `client/src/features/admin/features/orders/OrderItems.jsx` |
| `client/src/features/admin/features/orders/OrderSections.test.jsx` | Retained | `client/src/features/admin/features/orders/OrderSections.test.jsx` |
| `client/src/features/admin/features/orders/OrderSummary.jsx` | Retained | `client/src/features/admin/features/orders/OrderSummary.jsx` |
| `client/src/features/admin/features/orders/OrderTimeline.jsx` | Retained | `client/src/features/admin/features/orders/OrderTimeline.jsx` |
| `client/src/features/admin/features/orders/OrderTotals.jsx` | Retained | `client/src/features/admin/features/orders/OrderTotals.jsx` |
| `client/src/features/admin/features/orders/OrderTransitionForm.jsx` | Retained | `client/src/features/admin/features/orders/OrderTransitionForm.jsx` |
| `client/src/features/admin/features/orders/OrdersPage.jsx` | Retained | `client/src/features/admin/features/orders/OrdersPage.jsx` |
| `client/src/features/admin/features/orders/OrdersTable.jsx` | Retained | `client/src/features/admin/features/orders/OrdersTable.jsx` |
| `client/src/features/admin/features/orders/orderContracts.ts` | Retained | `client/src/features/admin/features/orders/orderContracts.ts` |
| `client/src/features/admin/features/orders/ordersApi.js` | Retained | `client/src/features/admin/features/orders/ordersApi.js` |
| `client/src/features/admin/features/orders/useOrderDetail.js` | Retained | `client/src/features/admin/features/orders/useOrderDetail.js` |
| `client/src/features/admin/features/orders/useOrders.js` | Retained | `client/src/features/admin/features/orders/useOrders.js` |
| `client/src/features/admin/features/payments/OrderPayments.jsx` | Retained | `client/src/features/admin/features/payments/OrderPayments.jsx` |
| `client/src/features/admin/features/payments/PaymentActions.jsx` | Retained | `client/src/features/admin/features/payments/PaymentActions.jsx` |
| `client/src/features/admin/features/payments/PaymentActions.test.jsx` | Retained | `client/src/features/admin/features/payments/PaymentActions.test.jsx` |
| `client/src/features/admin/features/payments/PaymentAttemptPanel.jsx` | Retained | `client/src/features/admin/features/payments/PaymentAttemptPanel.jsx` |
| `client/src/features/admin/features/payments/PaymentFilters.jsx` | Retained | `client/src/features/admin/features/payments/PaymentFilters.jsx` |
| `client/src/features/admin/features/payments/PaymentsPage.jsx` | Retained | `client/src/features/admin/features/payments/PaymentsPage.jsx` |
| `client/src/features/admin/features/payments/PaymentsTable.jsx` | Retained | `client/src/features/admin/features/payments/PaymentsTable.jsx` |
| `client/src/features/admin/features/payments/RefundForm.jsx` | Retained | `client/src/features/admin/features/payments/RefundForm.jsx` |
| `client/src/features/admin/features/payments/RefundHistory.jsx` | Retained | `client/src/features/admin/features/payments/RefundHistory.jsx` |
| `client/src/features/admin/features/payments/paymentContracts.ts` | Retained | `client/src/features/admin/features/payments/paymentContracts.ts` |
| `client/src/features/admin/features/payments/paymentsApi.js` | Retained | `client/src/features/admin/features/payments/paymentsApi.js` |
| `client/src/features/admin/features/payments/refundNotice.js` | Retained | `client/src/features/admin/features/payments/refundNotice.js` |
| `client/src/features/admin/features/payments/refundNotice.test.js` | Retained | `client/src/features/admin/features/payments/refundNotice.test.js` |
| `client/src/features/admin/features/payments/usePaymentOperations.js` | Retained | `client/src/features/admin/features/payments/usePaymentOperations.js` |
| `client/src/features/admin/features/payments/usePayments.js` | Retained | `client/src/features/admin/features/payments/usePayments.js` |
| `client/src/features/admin/features/production/ArchiveJobForm.jsx` | Retained | `client/src/features/admin/features/production/ArchiveJobForm.jsx` |
| `client/src/features/admin/features/production/PrivateAssetsList.jsx` | Retained | `client/src/features/admin/features/production/PrivateAssetsList.jsx` |
| `client/src/features/admin/features/production/ProductionFilters.jsx` | Retained | `client/src/features/admin/features/production/ProductionFilters.jsx` |
| `client/src/features/admin/features/production/ProductionJobPage.jsx` | Retained | `client/src/features/admin/features/production/ProductionJobPage.jsx` |
| `client/src/features/admin/features/production/ProductionJobSummary.jsx` | Retained | `client/src/features/admin/features/production/ProductionJobSummary.jsx` |
| `client/src/features/admin/features/production/ProductionQueuePage.jsx` | Retained | `client/src/features/admin/features/production/ProductionQueuePage.jsx` |
| `client/src/features/admin/features/production/ProductionQueueTable.jsx` | Retained | `client/src/features/admin/features/production/ProductionQueueTable.jsx` |
| `client/src/features/admin/features/production/ProductionTimeline.jsx` | Retained | `client/src/features/admin/features/production/ProductionTimeline.jsx` |
| `client/src/features/admin/features/production/ProductionTransitionForm.jsx` | Retained | `client/src/features/admin/features/production/ProductionTransitionForm.jsx` |
| `client/src/features/admin/features/production/ProductionTransitionForm.test.jsx` | Retained | `client/src/features/admin/features/production/ProductionTransitionForm.test.jsx` |
| `client/src/features/admin/features/production/ProofRevisionHistory.jsx` | Retained | `client/src/features/admin/features/production/ProofRevisionHistory.jsx` |
| `client/src/features/admin/features/production/ProofUploadForm.jsx` | Retained | `client/src/features/admin/features/production/ProofUploadForm.jsx` |
| `client/src/features/admin/features/production/productionApi.js` | Retained | `client/src/features/admin/features/production/productionApi.js` |
| `client/src/features/admin/features/production/productionContracts.ts` | Retained | `client/src/features/admin/features/production/productionContracts.ts` |
| `client/src/features/admin/features/production/useProductionJob.js` | Retained | `client/src/features/admin/features/production/useProductionJob.js` |
| `client/src/features/admin/features/production/useProductionQueue.js` | Retained | `client/src/features/admin/features/production/useProductionQueue.js` |
| `client/src/features/admin/features/products/ProductForm.jsx` | Retained | `client/src/features/admin/features/products/ProductForm.jsx` |
| `client/src/features/admin/features/products/ProductMediaEditor.jsx` | Retained | `client/src/features/admin/features/products/ProductMediaEditor.jsx` |
| `client/src/features/admin/features/products/ProductTools.jsx` | Retained | `client/src/features/admin/features/products/ProductTools.jsx` |
| `client/src/features/admin/features/products/ProductVariantsEditor.jsx` | Retained | `client/src/features/admin/features/products/ProductVariantsEditor.jsx` |
| `client/src/features/admin/features/products/ProductsPage.jsx` | Retained | `client/src/features/admin/features/products/ProductsPage.jsx` |
| `client/src/features/admin/features/products/ProductsTable.jsx` | Retained | `client/src/features/admin/features/products/ProductsTable.jsx` |
| `client/src/features/admin/features/products/productForm.js` | Retained | `client/src/features/admin/features/products/productForm.js` |
| `client/src/features/admin/features/products/productsApi.js` | Retained | `client/src/features/admin/features/products/productsApi.js` |
| `client/src/features/admin/features/products/useProductTools.js` | Retained | `client/src/features/admin/features/products/useProductTools.js` |
| `client/src/features/admin/features/products/useProducts.js` | Retained | `client/src/features/admin/features/products/useProducts.js` |
| `client/src/features/admin/features/promotions/PromotionForm.jsx` | Retained | `client/src/features/admin/features/promotions/PromotionForm.jsx` |
| `client/src/features/admin/features/promotions/PromotionRedemptionsTable.jsx` | Retained | `client/src/features/admin/features/promotions/PromotionRedemptionsTable.jsx` |
| `client/src/features/admin/features/promotions/PromotionsPage.jsx` | Retained | `client/src/features/admin/features/promotions/PromotionsPage.jsx` |
| `client/src/features/admin/features/promotions/PromotionsTable.jsx` | Retained | `client/src/features/admin/features/promotions/PromotionsTable.jsx` |
| `client/src/features/admin/features/promotions/promotionForm.js` | Retained | `client/src/features/admin/features/promotions/promotionForm.js` |
| `client/src/features/admin/features/promotions/promotionsApi.js` | Retained | `client/src/features/admin/features/promotions/promotionsApi.js` |
| `client/src/features/admin/features/promotions/usePromotions.js` | Retained | `client/src/features/admin/features/promotions/usePromotions.js` |
| `client/src/features/admin/features/reports/ReportDateFilters.jsx` | Retained | `client/src/features/admin/features/reports/ReportDateFilters.jsx` |
| `client/src/features/admin/features/reports/ReportTables.jsx` | Retained | `client/src/features/admin/features/reports/ReportTables.jsx` |
| `client/src/features/admin/features/reports/ReportsPage.jsx` | Retained | `client/src/features/admin/features/reports/ReportsPage.jsx` |
| `client/src/features/admin/features/reports/reportsApi.js` | Retained | `client/src/features/admin/features/reports/reportsApi.js` |
| `client/src/features/admin/features/reports/useReports.js` | Retained | `client/src/features/admin/features/reports/useReports.js` |
| `client/src/features/admin/features/reviews/ReviewModerationForm.jsx` | Retained | `client/src/features/admin/features/reviews/ReviewModerationForm.jsx` |
| `client/src/features/admin/features/reviews/ReviewsPage.jsx` | Retained | `client/src/features/admin/features/reviews/ReviewsPage.jsx` |
| `client/src/features/admin/features/reviews/ReviewsTable.jsx` | Retained | `client/src/features/admin/features/reviews/ReviewsTable.jsx` |
| `client/src/features/admin/features/reviews/reviewsApi.js` | Retained | `client/src/features/admin/features/reviews/reviewsApi.js` |
| `client/src/features/admin/features/reviews/useReviews.js` | Retained | `client/src/features/admin/features/reviews/useReviews.js` |
| `client/src/features/admin/features/settings/SettingForm.jsx` | Retained | `client/src/features/admin/features/settings/SettingForm.jsx` |
| `client/src/features/admin/features/settings/SettingsList.jsx` | Retained | `client/src/features/admin/features/settings/SettingsList.jsx` |
| `client/src/features/admin/features/settings/SettingsPage.jsx` | Retained | `client/src/features/admin/features/settings/SettingsPage.jsx` |
| `client/src/features/admin/features/settings/settingsApi.js` | Retained | `client/src/features/admin/features/settings/settingsApi.js` |
| `client/src/features/admin/features/settings/settingsForm.js` | Retained | `client/src/features/admin/features/settings/settingsForm.js` |
| `client/src/features/admin/features/settings/useSettings.js` | Retained | `client/src/features/admin/features/settings/useSettings.js` |
| `client/src/features/admin/features/shipping/OrderShipments.jsx` | Retained | `client/src/features/admin/features/shipping/OrderShipments.jsx` |
| `client/src/features/admin/features/shipping/ShipmentForm.jsx` | Retained | `client/src/features/admin/features/shipping/ShipmentForm.jsx` |
| `client/src/features/admin/features/shipping/ShippingMethodForm.jsx` | Retained | `client/src/features/admin/features/shipping/ShippingMethodForm.jsx` |
| `client/src/features/admin/features/shipping/ShippingPage.jsx` | Retained | `client/src/features/admin/features/shipping/ShippingPage.jsx` |
| `client/src/features/admin/features/shipping/ShippingZoneForm.jsx` | Retained | `client/src/features/admin/features/shipping/ShippingZoneForm.jsx` |
| `client/src/features/admin/features/shipping/shippingApi.js` | Retained | `client/src/features/admin/features/shipping/shippingApi.js` |
| `client/src/features/admin/features/shipping/shippingForm.js` | Retained | `client/src/features/admin/features/shipping/shippingForm.js` |
| `client/src/features/admin/features/shipping/useShippingConfiguration.js` | Retained | `client/src/features/admin/features/shipping/useShippingConfiguration.js` |
| `client/src/features/admin/features/staff/StaffPage.jsx` | Retained | `client/src/features/admin/features/staff/StaffPage.jsx` |
| `client/src/features/admin/features/staff/StaffRoleForm.jsx` | Retained | `client/src/features/admin/features/staff/StaffRoleForm.jsx` |
| `client/src/features/admin/features/staff/StaffTable.jsx` | Retained | `client/src/features/admin/features/staff/StaffTable.jsx` |
| `client/src/features/admin/features/staff/staffApi.js` | Retained | `client/src/features/admin/features/staff/staffApi.js` |
| `client/src/features/admin/features/staff/useStaff.js` | Retained | `client/src/features/admin/features/staff/useStaff.js` |
| `client/src/features/admin/layout/AdminHeader.jsx` | Retained | `client/src/features/admin/layout/AdminHeader.jsx` |
| `client/src/features/admin/layout/AdminLayout.jsx` | Retained | `client/src/features/admin/layout/AdminLayout.jsx` |
| `client/src/features/admin/layout/AdminLayout.module.css` | Retained | `client/src/features/admin/layout/AdminLayout.module.css` |
| `client/src/features/admin/layout/AdminNavigationItem.jsx` | Retained | `client/src/features/admin/layout/AdminNavigationItem.jsx` |
| `client/src/features/admin/layout/AdminSidebar.jsx` | Retained | `client/src/features/admin/layout/AdminSidebar.jsx` |
| `client/src/features/admin/layout/AdminUserMenu.jsx` | Retained | `client/src/features/admin/layout/AdminUserMenu.jsx` |
| `client/src/features/admin/layout/useAdminNavigation.js` | Retained | `client/src/features/admin/layout/useAdminNavigation.js` |
| `client/src/features/admin/shared/components/AdminPage.jsx` | Retained | `client/src/features/admin/shared/components/AdminPage.jsx` |
| `client/src/features/admin/shared/components/DataTable.jsx` | Retained | `client/src/features/admin/shared/components/DataTable.jsx` |
| `client/src/features/admin/shared/components/EmptyState.jsx` | Retained | `client/src/features/admin/shared/components/EmptyState.jsx` |
| `client/src/features/admin/shared/components/FilterBar.jsx` | Retained | `client/src/features/admin/shared/components/FilterBar.jsx` |
| `client/src/features/admin/shared/components/MutationFeedback.jsx` | Retained | `client/src/features/admin/shared/components/MutationFeedback.jsx` |
| `client/src/features/admin/shared/components/PageHeader.jsx` | Retained | `client/src/features/admin/shared/components/PageHeader.jsx` |
| `client/src/features/admin/shared/components/Pagination.jsx` | Retained | `client/src/features/admin/shared/components/Pagination.jsx` |
| `client/src/features/admin/shared/components/Pagination.test.jsx` | Retained | `client/src/features/admin/shared/components/Pagination.test.jsx` |
| `client/src/features/admin/shared/components/ResourceState.jsx` | Retained | `client/src/features/admin/shared/components/ResourceState.jsx` |
| `client/src/features/admin/shared/components/ResourceState.test.jsx` | Retained | `client/src/features/admin/shared/components/ResourceState.test.jsx` |
| `client/src/features/admin/shared/components/SectionCard.jsx` | Retained | `client/src/features/admin/shared/components/SectionCard.jsx` |
| `client/src/features/admin/shared/components/SnapshotViewer.jsx` | Retained | `client/src/features/admin/shared/components/SnapshotViewer.jsx` |
| `client/src/features/admin/shared/components/SnapshotViewer.test.jsx` | Retained | `client/src/features/admin/shared/components/SnapshotViewer.test.jsx` |
| `client/src/features/admin/shared/components/StatusBadge.jsx` | Retained | `client/src/features/admin/shared/components/StatusBadge.jsx` |
| `client/src/features/admin/shared/hooks/useAdminListParams.js` | Retained | `client/src/features/admin/shared/hooks/useAdminListParams.js` |
| `client/src/features/admin/shared/hooks/useAdminMutation.js` | Retained | `client/src/features/admin/shared/hooks/useAdminMutation.js` |
| `client/src/features/admin/shared/styles/adminBase.css` | Retained | `client/src/features/admin/shared/styles/adminBase.css` |
| `client/src/features/admin/shared/styles/adminTokens.css` | Retained | `client/src/features/admin/shared/styles/adminTokens.css` |
| `client/src/features/admin/shared/utils/pagination.js` | Retained | `client/src/features/admin/shared/utils/pagination.js` |
| `client/src/features/auth/AuthProvider.jsx` | Retained | `client/src/features/auth/AuthProvider.jsx` |
| `client/src/features/auth/auth.css` | Retained | `client/src/features/auth/auth.css` |
| `client/src/features/auth/authApi.js` | Retained | `client/src/features/auth/authApi.js` |
| `client/src/features/auth/components/AuthShell.jsx` | Retained | `client/src/features/auth/components/AuthShell.jsx` |
| `client/src/features/auth/components/ForgotPasswordForm.jsx` | Retained | `client/src/features/auth/components/ForgotPasswordForm.jsx` |
| `client/src/features/auth/components/LoginForm.jsx` | Retained | `client/src/features/auth/components/LoginForm.jsx` |
| `client/src/features/auth/components/RegisterForm.jsx` | Retained | `client/src/features/auth/components/RegisterForm.jsx` |
| `client/src/features/auth/components/ResetPasswordForm.jsx` | Retained | `client/src/features/auth/components/ResetPasswordForm.jsx` |
| `client/src/features/auth/components/VerifyEmailForm.jsx` | Retained | `client/src/features/auth/components/VerifyEmailForm.jsx` |
| `client/src/features/auth/hooks/useAuthenticatedMutation.js` | Retained | `client/src/features/auth/hooks/useAuthenticatedMutation.js` |
| `client/src/features/auth/hooks/useForgotPassword.js` | Retained | `client/src/features/auth/hooks/useForgotPassword.js` |
| `client/src/features/auth/hooks/useLogin.js` | Retained | `client/src/features/auth/hooks/useLogin.js` |
| `client/src/features/auth/hooks/useRegister.js` | Retained | `client/src/features/auth/hooks/useRegister.js` |
| `client/src/features/auth/hooks/useRequireLogin.js` | Retained | `client/src/features/auth/hooks/useRequireLogin.js` |
| `client/src/features/auth/hooks/useResetPassword.js` | Retained | `client/src/features/auth/hooks/useResetPassword.js` |
| `client/src/features/auth/hooks/useVerifyEmail.js` | Retained | `client/src/features/auth/hooks/useVerifyEmail.js` |
| `client/src/features/auth/pages/ForgotPasswordPage.jsx` | Retained | `client/src/features/auth/pages/ForgotPasswordPage.jsx` |
| `client/src/features/auth/pages/LoginPage.jsx` | Retained | `client/src/features/auth/pages/LoginPage.jsx` |
| `client/src/features/auth/pages/RegisterPage.jsx` | Retained | `client/src/features/auth/pages/RegisterPage.jsx` |
| `client/src/features/auth/pages/ResetPasswordPage.jsx` | Retained | `client/src/features/auth/pages/ResetPasswordPage.jsx` |
| `client/src/features/auth/pages/VerifyEmailPage.jsx` | Retained | `client/src/features/auth/pages/VerifyEmailPage.jsx` |
| `client/src/features/cart/CartProvider.jsx` | Retained | `client/src/features/cart/CartProvider.jsx` |
| `client/src/features/cart/cart.css` | Retained | `client/src/features/cart/cart.css` |
| `client/src/features/cart/cartApi.js` | Retained | `client/src/features/cart/cartApi.js` |
| `client/src/features/cart/components/CartItem.jsx` | Retained | `client/src/features/cart/components/CartItem.jsx` |
| `client/src/features/cart/components/CartSummary.jsx` | Retained | `client/src/features/cart/components/CartSummary.jsx` |
| `client/src/features/cart/hooks/useCartPage.js` | Retained | `client/src/features/cart/hooks/useCartPage.js` |
| `client/src/features/cart/pages/CartPage.jsx` | Retained | `client/src/features/cart/pages/CartPage.jsx` |
| `client/src/features/catalog/catalog.css` | Retained | `client/src/features/catalog/catalog.css` |
| `client/src/features/catalog/catalogApi.js` | Retained | `client/src/features/catalog/catalogApi.js` |
| `client/src/features/catalog/catalogContracts.ts` | Retained | `client/src/features/catalog/catalogContracts.ts` |
| `client/src/features/catalog/catalogQuery.test.ts` | Retained | `client/src/features/catalog/catalogQuery.test.ts` |
| `client/src/features/catalog/catalogQuery.ts` | Retained | `client/src/features/catalog/catalogQuery.ts` |
| `client/src/features/catalog/components/CatalogResults.jsx` | Retained | `client/src/features/catalog/components/CatalogResults.jsx` |
| `client/src/features/catalog/components/CatalogToolbar.jsx` | Retained | `client/src/features/catalog/components/CatalogToolbar.jsx` |
| `client/src/features/catalog/components/ProductCard.jsx` | Retained | `client/src/features/catalog/components/ProductCard.jsx` |
| `client/src/features/catalog/components/ProductDetail.jsx` | Retained | `client/src/features/catalog/components/ProductDetail.jsx` |
| `client/src/features/catalog/components/ProductFilters.jsx` | Retained | `client/src/features/catalog/components/ProductFilters.jsx` |
| `client/src/features/catalog/components/ProductGallery.jsx` | Retained | `client/src/features/catalog/components/ProductGallery.jsx` |
| `client/src/features/catalog/components/ProductPagination.jsx` | Retained | `client/src/features/catalog/components/ProductPagination.jsx` |
| `client/src/features/catalog/components/ProductSeo.jsx` | Retained | `client/src/features/catalog/components/ProductSeo.jsx` |
| `client/src/features/catalog/hooks/useCatalog.js` | Retained | `client/src/features/catalog/hooks/useCatalog.js` |
| `client/src/features/catalog/pages/ProductDetailsPage.jsx` | Retained | `client/src/features/catalog/pages/ProductDetailsPage.jsx` |
| `client/src/features/catalog/pages/ProductsPage.jsx` | Retained | `client/src/features/catalog/pages/ProductsPage.jsx` |
| `client/src/features/catalog/productSeo.js` | Retained | `client/src/features/catalog/productSeo.js` |
| `client/src/features/checkout/checkout.css` | Retained | `client/src/features/checkout/checkout.css` |
| `client/src/features/checkout/checkoutApi.js` | Retained | `client/src/features/checkout/checkoutApi.js` |
| `client/src/features/checkout/checkoutApi.test.jsx` | Retained | `client/src/features/checkout/checkoutApi.test.jsx` |
| `client/src/features/checkout/checkoutContracts.ts` | Retained | `client/src/features/checkout/checkoutContracts.ts` |
| `client/src/features/checkout/components/CheckoutForm.jsx` | Retained | `client/src/features/checkout/components/CheckoutForm.jsx` |
| `client/src/features/checkout/components/CheckoutReview.jsx` | Retained | `client/src/features/checkout/components/CheckoutReview.jsx` |
| `client/src/features/checkout/components/CheckoutTotals.jsx` | Retained | `client/src/features/checkout/components/CheckoutTotals.jsx` |
| `client/src/features/checkout/hooks/useCheckout.js` | Retained | `client/src/features/checkout/hooks/useCheckout.js` |
| `client/src/features/checkout/pages/CheckoutPage.jsx` | Retained | `client/src/features/checkout/pages/CheckoutPage.jsx` |
| `client/src/features/content/SiteContentProvider.jsx` | Retained | `client/src/features/content/SiteContentProvider.jsx` |
| `client/src/features/content/components/CmsSections.jsx` | Retained | `client/src/features/content/components/CmsSections.jsx` |
| `client/src/features/content/components/CmsSections.test.jsx` | Retained | `client/src/features/content/components/CmsSections.test.jsx` |
| `client/src/features/content/content.css` | Retained | `client/src/features/content/content.css` |
| `client/src/features/content/contentApi.js` | Retained | `client/src/features/content/contentApi.js` |
| `client/src/features/content/contentContracts.ts` | Retained | `client/src/features/content/contentContracts.ts` |
| `client/src/features/content/contentModel.js` | Retained | `client/src/features/content/contentModel.js` |
| `client/src/features/content/contentModel.test.js` | Retained | `client/src/features/content/contentModel.test.js` |
| `client/src/features/content/pages/ContentPage.jsx` | Retained | `client/src/features/content/pages/ContentPage.jsx` |
| `client/src/features/custom-design/artworkActions.js` | Retained | `client/src/features/custom-design/artworkActions.js` |
| `client/src/features/custom-design/artworkActions.test.js` | Retained | `client/src/features/custom-design/artworkActions.test.js` |
| `client/src/features/custom-design/builderDraft.js` | Retained | `client/src/features/custom-design/builderDraft.js` |
| `client/src/features/custom-design/builderDraft.test.js` | Retained | `client/src/features/custom-design/builderDraft.test.js` |
| `client/src/features/custom-design/components/BuilderControls.jsx` | Retained | `client/src/features/custom-design/components/BuilderControls.jsx` |
| `client/src/features/custom-design/components/BuilderPreview.jsx` | Retained | `client/src/features/custom-design/components/BuilderPreview.jsx` |
| `client/src/features/custom-design/components/SavedDesignCard.jsx` | Retained | `client/src/features/custom-design/components/SavedDesignCard.jsx` |
| `client/src/features/custom-design/customDesign.css` | Retained | `client/src/features/custom-design/customDesign.css` |
| `client/src/features/custom-design/customDesignApi.js` | Retained | `client/src/features/custom-design/customDesignApi.js` |
| `client/src/features/custom-design/designModel.js` | Retained | `client/src/features/custom-design/designModel.js` |
| `client/src/features/custom-design/designModel.test.jsx` | Retained | `client/src/features/custom-design/designModel.test.jsx` |
| `client/src/features/custom-design/hooks/useBuilderQuote.js` | Retained | `client/src/features/custom-design/hooks/useBuilderQuote.js` |
| `client/src/features/custom-design/hooks/useCustomBuilder.js` | Retained | `client/src/features/custom-design/hooks/useCustomBuilder.js` |
| `client/src/features/custom-design/hooks/useProofReview.js` | Retained | `client/src/features/custom-design/hooks/useProofReview.js` |
| `client/src/features/custom-design/hooks/useSavedDesigns.js` | Retained | `client/src/features/custom-design/hooks/useSavedDesigns.js` |
| `client/src/features/custom-design/pages/CustomBuilderPage.jsx` | Retained | `client/src/features/custom-design/pages/CustomBuilderPage.jsx` |
| `client/src/features/custom-design/pages/ProofReviewPage.jsx` | Retained | `client/src/features/custom-design/pages/ProofReviewPage.jsx` |
| `client/src/features/custom-design/pages/SavedDesignsPage.jsx` | Retained | `client/src/features/custom-design/pages/SavedDesignsPage.jsx` |
| `client/src/features/home/components/CategoryCards.jsx` | Retained | `client/src/features/home/components/CategoryCards.jsx` |
| `client/src/features/home/components/CollectionCards.jsx` | Retained | `client/src/features/home/components/CollectionCards.jsx` |
| `client/src/features/home/components/CustomCta.jsx` | Retained | `client/src/features/home/components/CustomCta.jsx` |
| `client/src/features/home/components/HeroCarousel.jsx` | Retained | `client/src/features/home/components/HeroCarousel.jsx` |
| `client/src/features/home/components/HowItWorks.jsx` | Retained | `client/src/features/home/components/HowItWorks.jsx` |
| `client/src/features/home/components/heroSlides.js` | Retained | `client/src/features/home/components/heroSlides.js` |
| `client/src/features/home/home.css` | Retained | `client/src/features/home/home.css` |
| `client/src/features/home/pages/HomePage.jsx` | Retained | `client/src/features/home/pages/HomePage.jsx` |
| `client/src/features/orders/components/OrderItems.jsx` | Retained | `client/src/features/orders/components/OrderItems.jsx` |
| `client/src/features/orders/orderContracts.ts` | Retained | `client/src/features/orders/orderContracts.ts` |
| `client/src/features/orders/orders.css` | Retained | `client/src/features/orders/orders.css` |
| `client/src/features/orders/ordersApi.js` | Retained | `client/src/features/orders/ordersApi.js` |
| `client/src/features/orders/pages/OrderDetailsPage.jsx` | Retained | `client/src/features/orders/pages/OrderDetailsPage.jsx` |
| `client/src/features/orders/pages/OrdersPage.jsx` | Retained | `client/src/features/orders/pages/OrdersPage.jsx` |
| `client/src/features/payments/hooks/useCheckoutPayment.js` | Retained | `client/src/features/payments/hooks/useCheckoutPayment.js` |
| `client/src/features/payments/hooks/usePaymentReturn.js` | Retained | `client/src/features/payments/hooks/usePaymentReturn.js` |
| `client/src/features/payments/pages/PaymentReturnPage.jsx` | Retained | `client/src/features/payments/pages/PaymentReturnPage.jsx` |
| `client/src/features/payments/paymentContracts.ts` | Retained | `client/src/features/payments/paymentContracts.ts` |
| `client/src/features/payments/paymentNavigation.js` | Retained | `client/src/features/payments/paymentNavigation.js` |
| `client/src/features/payments/paymentsApi.js` | Retained | `client/src/features/payments/paymentsApi.js` |
| `client/src/features/reviews/components/ProductReviews.jsx` | Retained | `client/src/features/reviews/components/ProductReviews.jsx` |
| `client/src/features/reviews/components/ReviewCard.jsx` | Retained | `client/src/features/reviews/components/ReviewCard.jsx` |
| `client/src/features/reviews/components/ReviewForm.jsx` | Retained | `client/src/features/reviews/components/ReviewForm.jsx` |
| `client/src/features/reviews/hooks/useProductReviews.js` | Retained | `client/src/features/reviews/hooks/useProductReviews.js` |
| `client/src/features/reviews/reviews.css` | Retained | `client/src/features/reviews/reviews.css` |
| `client/src/features/reviews/reviewsApi.js` | Retained | `client/src/features/reviews/reviewsApi.js` |
| `client/src/features/wishlist/WishlistProvider.jsx` | Retained | `client/src/features/wishlist/WishlistProvider.jsx` |
| `client/src/features/wishlist/components/WishlistItem.jsx` | Retained | `client/src/features/wishlist/components/WishlistItem.jsx` |
| `client/src/features/wishlist/hooks/useWishlistPage.js` | Retained | `client/src/features/wishlist/hooks/useWishlistPage.js` |
| `client/src/features/wishlist/pages/WishlistPage.jsx` | Retained | `client/src/features/wishlist/pages/WishlistPage.jsx` |
| `client/src/features/wishlist/wishlist.css` | Retained | `client/src/features/wishlist/wishlist.css` |
| `client/src/features/wishlist/wishlistApi.js` | Retained | `client/src/features/wishlist/wishlistApi.js` |
| `client/src/main.jsx` | Retained | `client/src/main.jsx` |
| `client/src/shared/api/apiError.test.ts` | Retained | `client/src/shared/api/apiError.test.ts` |
| `client/src/shared/api/apiError.ts` | Retained | `client/src/shared/api/apiError.ts` |
| `client/src/shared/api/authSession.js` | Retained | `client/src/shared/api/authSession.js` |
| `client/src/shared/api/authSession.test.ts` | Retained | `client/src/shared/api/authSession.test.ts` |
| `client/src/shared/api/clientTelemetry.js` | Retained | `client/src/shared/api/clientTelemetry.js` |
| `client/src/shared/api/clientTelemetry.test.js` | Retained | `client/src/shared/api/clientTelemetry.test.js` |
| `client/src/shared/api/httpClient.js` | Retained | `client/src/shared/api/httpClient.js` |
| `client/src/shared/api/requestConfig.js` | Retained | `client/src/shared/api/requestConfig.js` |
| `client/src/shared/components/feedback/EmptyState.jsx` | Retained | `client/src/shared/components/feedback/EmptyState.jsx` |
| `client/src/shared/components/feedback/ErrorBoundary.jsx` | Retained | `client/src/shared/components/feedback/ErrorBoundary.jsx` |
| `client/src/shared/components/feedback/ErrorState.jsx` | Retained | `client/src/shared/components/feedback/ErrorState.jsx` |
| `client/src/shared/components/feedback/Skeleton.jsx` | Retained | `client/src/shared/components/feedback/Skeleton.jsx` |
| `client/src/shared/components/feedback/ToastProvider.jsx` | Retained | `client/src/shared/components/feedback/ToastProvider.jsx` |
| `client/src/shared/components/seo/JsonLd.jsx` | Retained | `client/src/shared/components/seo/JsonLd.jsx` |
| `client/src/shared/components/ui/Button.jsx` | Retained | `client/src/shared/components/ui/Button.jsx` |
| `client/src/shared/components/ui/ConfirmDialog.jsx` | Retained | `client/src/shared/components/ui/ConfirmDialog.jsx` |
| `client/src/shared/components/ui/Dialog.jsx` | Retained | `client/src/shared/components/ui/Dialog.jsx` |
| `client/src/shared/components/ui/Icon.jsx` | Retained | `client/src/shared/components/ui/Icon.jsx` |
| `client/src/shared/components/ui/Media.jsx` | Retained | `client/src/shared/components/ui/Media.jsx` |
| `client/src/shared/components/ui/PageHeader.jsx` | Retained | `client/src/shared/components/ui/PageHeader.jsx` |
| `client/src/shared/components/ui/Price.jsx` | Retained | `client/src/shared/components/ui/Price.jsx` |
| `client/src/shared/components/ui/Quantity.jsx` | Retained | `client/src/shared/components/ui/Quantity.jsx` |
| `client/src/shared/components/ui/SectionHeading.jsx` | Retained | `client/src/shared/components/ui/SectionHeading.jsx` |
| `client/src/shared/hooks/useMutation.js` | Retained | `client/src/shared/hooks/useMutation.js` |
| `client/src/shared/hooks/useResource.js` | Retained | `client/src/shared/hooks/useResource.js` |
| `client/src/shared/styles/globals.css` | Retained | `client/src/shared/styles/globals.css` |
| `client/src/shared/styles/tokens.css` | Retained | `client/src/shared/styles/tokens.css` |
| `client/src/shared/utils/downloadFile.js` | Retained | `client/src/shared/utils/downloadFile.js` |
| `client/src/shared/utils/formatCurrency.ts` | Retained | `client/src/shared/utils/formatCurrency.ts` |
| `client/src/shared/utils/formatDate.ts` | Retained | `client/src/shared/utils/formatDate.ts` |
| `client/src/shared/utils/presentation.test.ts` | Retained | `client/src/shared/utils/presentation.test.ts` |
| `client/src/shared/utils/publicBase.js` | Retained | `client/src/shared/utils/publicBase.js` |
| `client/src/shared/utils/quantity.ts` | Retained | `client/src/shared/utils/quantity.ts` |
| `client/src/shared/utils/safeJson.ts` | Retained | `client/src/shared/utils/safeJson.ts` |
| `client/src/shared/utils/safeLink.ts` | Retained | `client/src/shared/utils/safeLink.ts` |
| `client/src/vite-env.d.ts` | Retained | `client/src/vite-env.d.ts` |
| `client/tsconfig.json` | Retained | `client/tsconfig.json` |
| `client/vite.config.js` | Retained | `client/vite.config.js` |
| `deploy/amaara-api.service.template` | Retained | `deploy/amaara-api.service.template` |
| `deploy/appsettings.Production.example.json` | Retained | `deploy/appsettings.Production.example.json` |
| `deploy/cutover-gate.example.json` | Retained | `deploy/cutover-gate.example.json` |
| `deploy/frontend.production.env.example` | Retained | `deploy/frontend.production.env.example` |
| `deploy/nginx.conf.template` | Retained | `deploy/nginx.conf.template` |
| `docs/admin-architecture.md` | Moved / merged | `docs/architecture/admin.md` |
| `docs/admin-refactor-result.md` | Moved / merged | `docs/reports/admin-refactor-result.md` |
| `docs/frontend-architecture.md` | Moved / merged | `docs/architecture/frontend.md` |
| `docs/frontend-file-audit.md` | Moved / merged | `docs/reports/frontend-file-audit.md` |
| `docs/frontend-refactor-result.md` | Moved / merged | `docs/reports/frontend-refactor-result.md` |
| `global.json` | Retained | `global.json` |
| `scripts/check-cutover-gate.py` | Retained | `scripts/check-cutover-gate.py` |
| `scripts/check-nuget-audit.py` | Retained | `scripts/check-nuget-audit.py` |
| `scripts/check-release-artifacts.py` | Retained | `scripts/check-release-artifacts.py` |
| `scripts/scan-secrets.py` | Retained | `scripts/scan-secrets.py` |
| `test-deployment.md` | TEMPORARY | Temporary GitHub Pages deployment probe with a literal shell date placeholder; no runtime, CI or documentation consumer. |
| `tests/be.AcceptanceHost/Program.cs` | Updated | `tests/be.AcceptanceHost/Program.cs` |
| `tests/be.AcceptanceHost/be.AcceptanceHost.csproj` | Retained | `tests/be.AcceptanceHost/be.AcceptanceHost.csproj` |
| `tests/be.Tests/AcceptanceApiFactory.cs` | Moved / merged | `tests/be.Tests/Fixtures/AcceptanceApiFactory.cs` |
| `tests/be.Tests/AdminOperationsTests.cs` | Moved / merged | `tests/be.Tests/Integration/Api/AdminOperationsTests.cs` |
| `tests/be.Tests/ApiRouteContractTests.cs` | Moved / merged | `tests/be.Tests/Architecture/ApiRouteContractTests.cs` |
| `tests/be.Tests/ApplicationDbContextModelTests.cs` | Moved / merged | `tests/be.Tests/Integration/Persistence/ApplicationDbContextModelTests.cs` |
| `tests/be.Tests/CheckoutLifecycleTests.cs` | Moved / merged | `tests/be.Tests/Integration/Checkout/CheckoutLifecycleTests.cs` |
| `tests/be.Tests/ChfCatalogTests.cs` | Moved / merged | `tests/be.Tests/Integration/Api/ChfCatalogTests.cs` |
| `tests/be.Tests/CommerceFixture.cs` | Moved / merged | `tests/be.Tests/Fixtures/CommerceFixture.cs` |
| `tests/be.Tests/CommerceRulesTests.cs` | Moved / merged | `tests/be.Tests/Unit/Commerce/CommerceRulesTests.cs` |
| `tests/be.Tests/ControlledLoadTests.cs` | Moved / merged | `tests/be.Tests/Integration/Load/ControlledLoadTests.cs` |
| `tests/be.Tests/CorrelationIdMiddlewareTests.cs` | Moved / merged | `tests/be.Tests/Unit/Api/CorrelationIdMiddlewareTests.cs` |
| `tests/be.Tests/CustomBuilderCustomBuilderTests.cs` | Moved / merged | `tests/be.Tests/Unit/CustomDesigns/CustomDesignWorkflowTests.cs` |
| `tests/be.Tests/DatatransAdapterTests.cs` | Moved / merged | `tests/be.Tests/Unit/Payments/DatatransAdapterTests.cs` |
| `tests/be.Tests/ExternalSmtpAcceptance.cs` | Moved / merged | `tests/be.Tests/Fixtures/ExternalSmtpAcceptance.cs` |
| `tests/be.Tests/GlobalUsings.cs` | Retained | `tests/be.Tests/GlobalUsings.cs` |
| `tests/be.Tests/LocalMediaStorageServiceTests.cs` | Moved / merged | `tests/be.Tests/Unit/Storage/LocalMediaStorageServiceTests.cs` |
| `tests/be.Tests/NotificationDeliveryTests.cs` | Moved / merged | `tests/be.Tests/Integration/Notifications/NotificationDeliveryTests.cs` |
| `tests/be.Tests/PaginationTests.cs` | Moved / merged | `tests/be.Tests/Unit/Common/PaginationTests.cs` |
| `tests/be.Tests/PaymentHttpSecurityTests.cs` | Moved / merged | `tests/be.Tests/Integration/Payments/PaymentHttpSecurityTests.cs` |
| `tests/be.Tests/RefreshSessionServiceTests.cs` | Moved / merged | `tests/be.Tests/Unit/Identity/RefreshSessionServiceTests.cs` |
| `tests/be.Tests/RefundHttpTests.cs` | Moved / merged | `tests/be.Tests/Integration/Payments/RefundHttpTests.cs` |
| `tests/be.Tests/ReleaseSecurityAndSeoTests.cs` | Moved / merged | `tests/be.Tests/Integration/Api/ReleaseSecurityAndSeoTests.cs` |
| `tests/be.Tests/ReviewModerationTests.cs` | Moved / merged | `tests/be.Tests/Integration/Api/ReviewModerationTests.cs` |
| `tests/be.Tests/SqlCommerceTests.cs` | Moved / merged | `tests/be.Tests/Integration/Sql/SqlCommerceTests.cs` |
| `tests/be.Tests/SqlReleaseRehearsalTests.cs` | Moved / merged | `tests/be.Tests/Integration/Sql/SqlReleaseRehearsalTests.cs` |
| `tests/be.Tests/StaffAndProductionTests.cs` | Moved / merged | `tests/be.Tests/Integration/Api/StaffAndProductionTests.cs` |
| `tests/be.Tests/ZahlsAdapterTests.cs` | Moved / merged | `tests/be.Tests/Unit/Payments/ZahlsAdapterTests.cs` |
| `tests/be.Tests/be.Tests.csproj` | Retained | `tests/be.Tests/be.Tests.csproj` |
| `tests/webhook-tunnel-proxy.mjs` | Retained | `tests/webhook-tunnel-proxy.mjs` |
| `tests/webhook-tunnel-proxy.test.mjs` | Retained | `tests/webhook-tunnel-proxy.test.mjs` |
