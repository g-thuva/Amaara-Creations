# Frontend file audit

Baseline: clean `develop` at `4952565`; 294 tracked frontend files inventoried before editing. Every source/config/test/public file was read by the dependency/source audit. Runtime URLs, dynamic imports, route registration, CSS classes, test harness imports and build entry references were checked before retirement.

Classifications are final dispositions. MOVE/RENAME retain behavior; SPLIT/MERGE retire an old aggregation after its callers migrate. No LEGACY or TEMPORARY COMPAT files remain. Root runtime logs/backups are ignored verification artifacts.

| Original file | Classification | Current owner / reason |
| --- | --- | --- |
| `client/.env.example` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/.gitignore` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/README.md` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/CustomBuilder.spec.ts` | RENAME | `client/e2e/storefront/custom-design.spec.ts`. Behavior retained under client/e2e/storefront. |
| `client/e2e/accessibility.spec.ts` | SPLIT | Cohesive responsibilities now owned by storefront/admin accessibility journeys and shared E2E accessibility helper. |
| `client/e2e/admin/architecture.spec.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/admin/catalog.spec.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/admin/operations.spec.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/admin/payments.spec.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/artwork.spec.ts` | MOVE | `client/e2e/storefront/artwork.spec.ts`. Behavior retained under client/e2e/storefront. |
| `client/e2e/checkout.spec.ts` | SPLIT | Cohesive responsibilities now owned by checkout journey, shared controlled provider fixture and admin checkout/payment checks. |
| `client/e2e/commerce.spec.ts` | MOVE | `client/e2e/checkout/commerce.spec.ts`. Behavior retained under client/e2e/checkout. |
| `client/e2e/fixtures.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/e2e/live.spec.ts` | RENAME | `client/e2e/account/customer-lifecycle.spec.ts`. Behavior retained under client/e2e/account. |
| `client/e2e/protected.spec.ts` | RENAME | `client/e2e/account/responsive.spec.ts`. Behavior retained under client/e2e/account. |
| `client/e2e/public.spec.ts` | MOVE | `client/e2e/storefront/public.spec.ts`. Behavior retained under client/e2e/storefront. |
| `client/e2e/release-production.spec.ts` | MOVE | `client/e2e/production/release-production.spec.ts`. Behavior retained under client/e2e/production. |
| `client/eslint.config.js` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/index.html` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/package-lock.json` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/package.json` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/playwright.config.ts` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/public/favicon.svg` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/scripts/check-performance.mjs` | KEEP | Reviewed and retained active source, test, tooling, documented configuration or public asset. |
| `client/scripts/run-production-browser.mjs` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/src/App.css` | MERGE | client/src/shared/styles/globals.css |
| `client/src/App.jsx` | SPLIT | Cohesive responsibilities now owned by app shell, public/account/admin routes, guards and provider composition. |
| `client/src/admin/app/AdminLanding.jsx` | MOVE | `client/src/features/admin/app/AdminLanding.jsx`. Behavior retained under client/src/features/admin/app. |
| `client/src/admin/app/AdminRouteGuard.jsx` | RENAME | `client/src/app/guards/AdminGuard.jsx`. Behavior retained under client/src/app/guards. |
| `client/src/admin/app/AdminRoutes.jsx` | MOVE | `client/src/app/routes/AdminRoutes.jsx`. Behavior retained under client/src/app/routes. |
| `client/src/admin/app/adminNavigation.js` | MOVE | `client/src/features/admin/app/adminNavigation.js`. Behavior retained under client/src/features/admin/app. |
| `client/src/admin/app/adminPermissions.js` | RENAME | `client/src/app/config/permissions.js`. Behavior retained under client/src/app/config. |
| `client/src/admin/features/audit/AuditFilters.jsx` | MOVE | `client/src/features/admin/features/audit/AuditFilters.jsx`. Behavior retained under client/src/features/admin/features/audit. |
| `client/src/admin/features/audit/AuditPage.jsx` | MOVE | `client/src/features/admin/features/audit/AuditPage.jsx`. Behavior retained under client/src/features/admin/features/audit. |
| `client/src/admin/features/audit/AuditTable.jsx` | MOVE | `client/src/features/admin/features/audit/AuditTable.jsx`. Behavior retained under client/src/features/admin/features/audit. |
| `client/src/admin/features/audit/auditApi.js` | MOVE | `client/src/features/admin/features/audit/auditApi.js`. Behavior retained under client/src/features/admin/features/audit. |
| `client/src/admin/features/audit/useAudit.js` | MOVE | `client/src/features/admin/features/audit/useAudit.js`. Behavior retained under client/src/features/admin/features/audit. |
| `client/src/admin/features/builder-configuration/BuilderConfigurationPage.jsx` | MOVE | `client/src/features/admin/features/builder-configuration/BuilderConfigurationPage.jsx`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/BuilderOptionsEditor.jsx` | MOVE | `client/src/features/admin/features/builder-configuration/BuilderOptionsEditor.jsx`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/BuilderPricingEditor.jsx` | MOVE | `client/src/features/admin/features/builder-configuration/BuilderPricingEditor.jsx`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/BuilderVersionSummary.jsx` | MOVE | `client/src/features/admin/features/builder-configuration/BuilderVersionSummary.jsx`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/builderConfigurationApi.js` | MOVE | `client/src/features/admin/features/builder-configuration/builderConfigurationApi.js`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/builderForm.js` | MOVE | `client/src/features/admin/features/builder-configuration/builderForm.js`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/builder-configuration/useBuilderConfiguration.js` | MOVE | `client/src/features/admin/features/builder-configuration/useBuilderConfiguration.js`. Behavior retained under client/src/features/admin/features/builder-configuration. |
| `client/src/admin/features/categories/CategoriesPage.jsx` | MOVE | `client/src/features/admin/features/categories/CategoriesPage.jsx`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/categories/CategoriesTable.jsx` | MOVE | `client/src/features/admin/features/categories/CategoriesTable.jsx`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/categories/CategoryForm.jsx` | MOVE | `client/src/features/admin/features/categories/CategoryForm.jsx`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/categories/categoriesApi.js` | MOVE | `client/src/features/admin/features/categories/categoriesApi.js`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/categories/categoriesForm.js` | MOVE | `client/src/features/admin/features/categories/categoriesForm.js`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/categories/useCategories.js` | MOVE | `client/src/features/admin/features/categories/useCategories.js`. Behavior retained under client/src/features/admin/features/categories. |
| `client/src/admin/features/collections/CollectionForm.jsx` | MOVE | `client/src/features/admin/features/collections/CollectionForm.jsx`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/collections/CollectionsPage.jsx` | MOVE | `client/src/features/admin/features/collections/CollectionsPage.jsx`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/collections/CollectionsTable.jsx` | MOVE | `client/src/features/admin/features/collections/CollectionsTable.jsx`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/collections/collectionsApi.js` | MOVE | `client/src/features/admin/features/collections/collectionsApi.js`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/collections/collectionsForm.js` | MOVE | `client/src/features/admin/features/collections/collectionsForm.js`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/collections/useCollections.js` | MOVE | `client/src/features/admin/features/collections/useCollections.js`. Behavior retained under client/src/features/admin/features/collections. |
| `client/src/admin/features/content/ContentForm.jsx` | MOVE | `client/src/features/admin/features/content/ContentForm.jsx`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/content/ContentPage.jsx` | MOVE | `client/src/features/admin/features/content/ContentPage.jsx`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/content/ContentPagesTable.jsx` | MOVE | `client/src/features/admin/features/content/ContentPagesTable.jsx`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/content/contentApi.js` | MOVE | `client/src/features/admin/features/content/contentApi.js`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/content/contentForm.js` | MOVE | `client/src/features/admin/features/content/contentForm.js`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/content/useContent.js` | MOVE | `client/src/features/admin/features/content/useContent.js`. Behavior retained under client/src/features/admin/features/content. |
| `client/src/admin/features/customers/CustomerActivity.jsx` | MOVE | `client/src/features/admin/features/customers/CustomerActivity.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/CustomerAddresses.jsx` | MOVE | `client/src/features/admin/features/customers/CustomerAddresses.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/CustomerDetailPage.jsx` | MOVE | `client/src/features/admin/features/customers/CustomerDetailPage.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/CustomerSummary.jsx` | MOVE | `client/src/features/admin/features/customers/CustomerSummary.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/CustomersPage.jsx` | MOVE | `client/src/features/admin/features/customers/CustomersPage.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/CustomersTable.jsx` | MOVE | `client/src/features/admin/features/customers/CustomersTable.jsx`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/customersApi.js` | MOVE | `client/src/features/admin/features/customers/customersApi.js`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/customers/useCustomers.js` | MOVE | `client/src/features/admin/features/customers/useCustomers.js`. Behavior retained under client/src/features/admin/features/customers. |
| `client/src/admin/features/dashboard/Dashboard.module.css` | MOVE | `client/src/features/admin/features/dashboard/Dashboard.module.css`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/DashboardPage.jsx` | MOVE | `client/src/features/admin/features/dashboard/DashboardPage.jsx`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/DashboardSummary.jsx` | MOVE | `client/src/features/admin/features/dashboard/DashboardSummary.jsx`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/RecentOrdersTable.jsx` | MOVE | `client/src/features/admin/features/dashboard/RecentOrdersTable.jsx`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/TopProductsList.jsx` | MOVE | `client/src/features/admin/features/dashboard/TopProductsList.jsx`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/dashboardApi.js` | MOVE | `client/src/features/admin/features/dashboard/dashboardApi.js`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/dashboard/useDashboard.js` | MOVE | `client/src/features/admin/features/dashboard/useDashboard.js`. Behavior retained under client/src/features/admin/features/dashboard. |
| `client/src/admin/features/inventory/InventoryAdjustmentForm.jsx` | MOVE | `client/src/features/admin/features/inventory/InventoryAdjustmentForm.jsx`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/InventoryMovementsTable.jsx` | MOVE | `client/src/features/admin/features/inventory/InventoryMovementsTable.jsx`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/InventoryPage.jsx` | MOVE | `client/src/features/admin/features/inventory/InventoryPage.jsx`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/InventorySections.test.jsx` | MOVE | `client/src/features/admin/features/inventory/InventorySections.test.jsx`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/StockBalancesTable.jsx` | MOVE | `client/src/features/admin/features/inventory/StockBalancesTable.jsx`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/inventoryApi.js` | MOVE | `client/src/features/admin/features/inventory/inventoryApi.js`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/inventoryContracts.ts` | MOVE | `client/src/features/admin/features/inventory/inventoryContracts.ts`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/inventory/useInventory.js` | MOVE | `client/src/features/admin/features/inventory/useInventory.js`. Behavior retained under client/src/features/admin/features/inventory. |
| `client/src/admin/features/media/MediaPage.jsx` | MOVE | `client/src/features/admin/features/media/MediaPage.jsx`. Behavior retained under client/src/features/admin/features/media. |
| `client/src/admin/features/media/MediaTable.jsx` | MOVE | `client/src/features/admin/features/media/MediaTable.jsx`. Behavior retained under client/src/features/admin/features/media. |
| `client/src/admin/features/media/mediaApi.js` | MOVE | `client/src/features/admin/features/media/mediaApi.js`. Behavior retained under client/src/features/admin/features/media. |
| `client/src/admin/features/orders/OrderDetailPage.jsx` | MOVE | `client/src/features/admin/features/orders/OrderDetailPage.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderFilters.jsx` | MOVE | `client/src/features/admin/features/orders/OrderFilters.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderItems.jsx` | MOVE | `client/src/features/admin/features/orders/OrderItems.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderSections.test.jsx` | MOVE | `client/src/features/admin/features/orders/OrderSections.test.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderSummary.jsx` | MOVE | `client/src/features/admin/features/orders/OrderSummary.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderTimeline.jsx` | MOVE | `client/src/features/admin/features/orders/OrderTimeline.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderTotals.jsx` | MOVE | `client/src/features/admin/features/orders/OrderTotals.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrderTransitionForm.jsx` | MOVE | `client/src/features/admin/features/orders/OrderTransitionForm.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrdersPage.jsx` | MOVE | `client/src/features/admin/features/orders/OrdersPage.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/OrdersTable.jsx` | MOVE | `client/src/features/admin/features/orders/OrdersTable.jsx`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/orderContracts.ts` | SPLIT | Cohesive responsibilities now owned by orders/orderContracts plus admin-only order filters/transitions. |
| `client/src/admin/features/orders/ordersApi.js` | MOVE | `client/src/features/admin/features/orders/ordersApi.js`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/useOrderDetail.js` | MOVE | `client/src/features/admin/features/orders/useOrderDetail.js`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/orders/useOrders.js` | MOVE | `client/src/features/admin/features/orders/useOrders.js`. Behavior retained under client/src/features/admin/features/orders. |
| `client/src/admin/features/payments/OrderPayments.jsx` | MOVE | `client/src/features/admin/features/payments/OrderPayments.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentActions.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentActions.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentActions.test.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentActions.test.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentAttemptPanel.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentAttemptPanel.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentFilters.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentFilters.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentsPage.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentsPage.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/PaymentsTable.jsx` | MOVE | `client/src/features/admin/features/payments/PaymentsTable.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/RefundForm.jsx` | MOVE | `client/src/features/admin/features/payments/RefundForm.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/RefundHistory.jsx` | MOVE | `client/src/features/admin/features/payments/RefundHistory.jsx`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/paymentContracts.ts` | SPLIT | Cohesive responsibilities now owned by payments/paymentContracts plus admin-only filters/refund requests. |
| `client/src/admin/features/payments/paymentsApi.js` | MOVE | `client/src/features/admin/features/payments/paymentsApi.js`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/refundNotice.js` | MOVE | `client/src/features/admin/features/payments/refundNotice.js`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/refundNotice.test.js` | MOVE | `client/src/features/admin/features/payments/refundNotice.test.js`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/usePaymentOperations.js` | MOVE | `client/src/features/admin/features/payments/usePaymentOperations.js`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/payments/usePayments.js` | MOVE | `client/src/features/admin/features/payments/usePayments.js`. Behavior retained under client/src/features/admin/features/payments. |
| `client/src/admin/features/production/ArchiveJobForm.jsx` | MOVE | `client/src/features/admin/features/production/ArchiveJobForm.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/PrivateAssetsList.jsx` | MOVE | `client/src/features/admin/features/production/PrivateAssetsList.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionFilters.jsx` | MOVE | `client/src/features/admin/features/production/ProductionFilters.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionJobPage.jsx` | MOVE | `client/src/features/admin/features/production/ProductionJobPage.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionJobSummary.jsx` | MOVE | `client/src/features/admin/features/production/ProductionJobSummary.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionQueuePage.jsx` | MOVE | `client/src/features/admin/features/production/ProductionQueuePage.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionQueueTable.jsx` | MOVE | `client/src/features/admin/features/production/ProductionQueueTable.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionTimeline.jsx` | MOVE | `client/src/features/admin/features/production/ProductionTimeline.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionTransitionForm.jsx` | MOVE | `client/src/features/admin/features/production/ProductionTransitionForm.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProductionTransitionForm.test.jsx` | MOVE | `client/src/features/admin/features/production/ProductionTransitionForm.test.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProofRevisionHistory.jsx` | MOVE | `client/src/features/admin/features/production/ProofRevisionHistory.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/ProofUploadForm.jsx` | MOVE | `client/src/features/admin/features/production/ProofUploadForm.jsx`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/productionApi.js` | MOVE | `client/src/features/admin/features/production/productionApi.js`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/productionContracts.ts` | MOVE | `client/src/features/admin/features/production/productionContracts.ts`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/useProductionJob.js` | MOVE | `client/src/features/admin/features/production/useProductionJob.js`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/production/useProductionQueue.js` | MOVE | `client/src/features/admin/features/production/useProductionQueue.js`. Behavior retained under client/src/features/admin/features/production. |
| `client/src/admin/features/products/ProductForm.jsx` | MOVE | `client/src/features/admin/features/products/ProductForm.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/ProductMediaEditor.jsx` | MOVE | `client/src/features/admin/features/products/ProductMediaEditor.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/ProductTools.jsx` | MOVE | `client/src/features/admin/features/products/ProductTools.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/ProductVariantsEditor.jsx` | MOVE | `client/src/features/admin/features/products/ProductVariantsEditor.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/ProductsPage.jsx` | MOVE | `client/src/features/admin/features/products/ProductsPage.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/ProductsTable.jsx` | MOVE | `client/src/features/admin/features/products/ProductsTable.jsx`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/productForm.js` | MOVE | `client/src/features/admin/features/products/productForm.js`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/productsApi.js` | MOVE | `client/src/features/admin/features/products/productsApi.js`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/useProductTools.js` | MOVE | `client/src/features/admin/features/products/useProductTools.js`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/products/useProducts.js` | MOVE | `client/src/features/admin/features/products/useProducts.js`. Behavior retained under client/src/features/admin/features/products. |
| `client/src/admin/features/promotions/PromotionForm.jsx` | MOVE | `client/src/features/admin/features/promotions/PromotionForm.jsx`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/PromotionRedemptionsTable.jsx` | MOVE | `client/src/features/admin/features/promotions/PromotionRedemptionsTable.jsx`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/PromotionsPage.jsx` | MOVE | `client/src/features/admin/features/promotions/PromotionsPage.jsx`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/PromotionsTable.jsx` | MOVE | `client/src/features/admin/features/promotions/PromotionsTable.jsx`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/promotionForm.js` | MOVE | `client/src/features/admin/features/promotions/promotionForm.js`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/promotionsApi.js` | MOVE | `client/src/features/admin/features/promotions/promotionsApi.js`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/promotions/usePromotions.js` | MOVE | `client/src/features/admin/features/promotions/usePromotions.js`. Behavior retained under client/src/features/admin/features/promotions. |
| `client/src/admin/features/reports/ReportDateFilters.jsx` | MOVE | `client/src/features/admin/features/reports/ReportDateFilters.jsx`. Behavior retained under client/src/features/admin/features/reports. |
| `client/src/admin/features/reports/ReportTables.jsx` | MOVE | `client/src/features/admin/features/reports/ReportTables.jsx`. Behavior retained under client/src/features/admin/features/reports. |
| `client/src/admin/features/reports/ReportsPage.jsx` | MOVE | `client/src/features/admin/features/reports/ReportsPage.jsx`. Behavior retained under client/src/features/admin/features/reports. |
| `client/src/admin/features/reports/reportsApi.js` | MOVE | `client/src/features/admin/features/reports/reportsApi.js`. Behavior retained under client/src/features/admin/features/reports. |
| `client/src/admin/features/reports/useReports.js` | MOVE | `client/src/features/admin/features/reports/useReports.js`. Behavior retained under client/src/features/admin/features/reports. |
| `client/src/admin/features/reviews/ReviewModerationForm.jsx` | MOVE | `client/src/features/admin/features/reviews/ReviewModerationForm.jsx`. Behavior retained under client/src/features/admin/features/reviews. |
| `client/src/admin/features/reviews/ReviewsPage.jsx` | MOVE | `client/src/features/admin/features/reviews/ReviewsPage.jsx`. Behavior retained under client/src/features/admin/features/reviews. |
| `client/src/admin/features/reviews/ReviewsTable.jsx` | MOVE | `client/src/features/admin/features/reviews/ReviewsTable.jsx`. Behavior retained under client/src/features/admin/features/reviews. |
| `client/src/admin/features/reviews/reviewsApi.js` | MOVE | `client/src/features/admin/features/reviews/reviewsApi.js`. Behavior retained under client/src/features/admin/features/reviews. |
| `client/src/admin/features/reviews/useReviews.js` | MOVE | `client/src/features/admin/features/reviews/useReviews.js`. Behavior retained under client/src/features/admin/features/reviews. |
| `client/src/admin/features/settings/SettingForm.jsx` | MOVE | `client/src/features/admin/features/settings/SettingForm.jsx`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/settings/SettingsList.jsx` | MOVE | `client/src/features/admin/features/settings/SettingsList.jsx`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/settings/SettingsPage.jsx` | MOVE | `client/src/features/admin/features/settings/SettingsPage.jsx`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/settings/settingsApi.js` | MOVE | `client/src/features/admin/features/settings/settingsApi.js`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/settings/settingsForm.js` | MOVE | `client/src/features/admin/features/settings/settingsForm.js`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/settings/useSettings.js` | MOVE | `client/src/features/admin/features/settings/useSettings.js`. Behavior retained under client/src/features/admin/features/settings. |
| `client/src/admin/features/shipping/OrderShipments.jsx` | MOVE | `client/src/features/admin/features/shipping/OrderShipments.jsx`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/ShipmentForm.jsx` | MOVE | `client/src/features/admin/features/shipping/ShipmentForm.jsx`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/ShippingMethodForm.jsx` | MOVE | `client/src/features/admin/features/shipping/ShippingMethodForm.jsx`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/ShippingPage.jsx` | MOVE | `client/src/features/admin/features/shipping/ShippingPage.jsx`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/ShippingZoneForm.jsx` | MOVE | `client/src/features/admin/features/shipping/ShippingZoneForm.jsx`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/shippingApi.js` | MOVE | `client/src/features/admin/features/shipping/shippingApi.js`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/shippingForm.js` | MOVE | `client/src/features/admin/features/shipping/shippingForm.js`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/shipping/useShippingConfiguration.js` | MOVE | `client/src/features/admin/features/shipping/useShippingConfiguration.js`. Behavior retained under client/src/features/admin/features/shipping. |
| `client/src/admin/features/staff/StaffPage.jsx` | MOVE | `client/src/features/admin/features/staff/StaffPage.jsx`. Behavior retained under client/src/features/admin/features/staff. |
| `client/src/admin/features/staff/StaffRoleForm.jsx` | MOVE | `client/src/features/admin/features/staff/StaffRoleForm.jsx`. Behavior retained under client/src/features/admin/features/staff. |
| `client/src/admin/features/staff/StaffTable.jsx` | MOVE | `client/src/features/admin/features/staff/StaffTable.jsx`. Behavior retained under client/src/features/admin/features/staff. |
| `client/src/admin/features/staff/staffApi.js` | MOVE | `client/src/features/admin/features/staff/staffApi.js`. Behavior retained under client/src/features/admin/features/staff. |
| `client/src/admin/features/staff/useStaff.js` | MOVE | `client/src/features/admin/features/staff/useStaff.js`. Behavior retained under client/src/features/admin/features/staff. |
| `client/src/admin/layout/AdminHeader.jsx` | MOVE | `client/src/features/admin/layout/AdminHeader.jsx`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/AdminLayout.jsx` | MOVE | `client/src/features/admin/layout/AdminLayout.jsx`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/AdminLayout.module.css` | MOVE | `client/src/features/admin/layout/AdminLayout.module.css`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/AdminNavigationItem.jsx` | MOVE | `client/src/features/admin/layout/AdminNavigationItem.jsx`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/AdminSidebar.jsx` | MOVE | `client/src/features/admin/layout/AdminSidebar.jsx`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/AdminUserMenu.jsx` | MOVE | `client/src/features/admin/layout/AdminUserMenu.jsx`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/layout/useAdminNavigation.js` | MOVE | `client/src/features/admin/layout/useAdminNavigation.js`. Behavior retained under client/src/features/admin/layout. |
| `client/src/admin/shared/components/AdminPage.jsx` | MOVE | `client/src/features/admin/shared/components/AdminPage.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/DataTable.jsx` | MOVE | `client/src/features/admin/shared/components/DataTable.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/EmptyState.jsx` | MOVE | `client/src/features/admin/shared/components/EmptyState.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/FilterBar.jsx` | MOVE | `client/src/features/admin/shared/components/FilterBar.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/MutationFeedback.jsx` | MOVE | `client/src/features/admin/shared/components/MutationFeedback.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/PageHeader.jsx` | MOVE | `client/src/features/admin/shared/components/PageHeader.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/Pagination.jsx` | MOVE | `client/src/features/admin/shared/components/Pagination.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/Pagination.test.jsx` | MOVE | `client/src/features/admin/shared/components/Pagination.test.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/ResourceState.jsx` | MOVE | `client/src/features/admin/shared/components/ResourceState.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/ResourceState.test.jsx` | MOVE | `client/src/features/admin/shared/components/ResourceState.test.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/SectionCard.jsx` | MOVE | `client/src/features/admin/shared/components/SectionCard.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/SnapshotViewer.jsx` | MOVE | `client/src/features/admin/shared/components/SnapshotViewer.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/SnapshotViewer.test.jsx` | MOVE | `client/src/features/admin/shared/components/SnapshotViewer.test.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/components/StatusBadge.jsx` | MOVE | `client/src/features/admin/shared/components/StatusBadge.jsx`. Behavior retained under client/src/features/admin/shared/components. |
| `client/src/admin/shared/hooks/useAdminListParams.js` | MOVE | `client/src/features/admin/shared/hooks/useAdminListParams.js`. Behavior retained under client/src/features/admin/shared/hooks. |
| `client/src/admin/shared/hooks/useAdminMutation.js` | MOVE | `client/src/features/admin/shared/hooks/useAdminMutation.js`. Behavior retained under client/src/features/admin/shared/hooks. |
| `client/src/admin/shared/styles/adminBase.css` | MOVE | `client/src/features/admin/shared/styles/adminBase.css`. Behavior retained under client/src/features/admin/shared/styles. |
| `client/src/admin/shared/styles/adminTokens.css` | MOVE | `client/src/features/admin/shared/styles/adminTokens.css`. Behavior retained under client/src/features/admin/shared/styles. |
| `client/src/admin/shared/utils/downloadFile.js` | MOVE | `client/src/shared/utils/downloadFile.js`. Behavior retained under client/src/shared/utils. |
| `client/src/admin/shared/utils/formatDateTime.js` | MERGE | client/src/shared/utils/formatDate.ts |
| `client/src/admin/shared/utils/pagination.js` | MOVE | `client/src/features/admin/shared/utils/pagination.js`. Behavior retained under client/src/features/admin/shared/utils. |
| `client/src/components/AppIcon.jsx` | RENAME | `client/src/shared/components/ui/Icon.jsx`. Behavior retained under client/src/shared/components/ui. |
| `client/src/components/Footer.jsx` | MOVE | `client/src/app/layout/Footer.jsx`. Behavior retained under client/src/app/layout. |
| `client/src/components/Navbar.jsx` | MOVE | `client/src/app/layout/Navbar.jsx`. Behavior retained under client/src/app/layout. |
| `client/src/components/ProtectedRoute.jsx` | RENAME | `client/src/app/guards/AuthGuard.jsx`. Behavior retained under client/src/app/guards. |
| `client/src/components/Seo.jsx` | SPLIT | Cohesive responsibilities now owned by app/seo, catalog/ProductSeo and shared JsonLd. |
| `client/src/components/home/CategoryCards.jsx` | MOVE | `client/src/features/home/components/CategoryCards.jsx`. Behavior retained under client/src/features/home/components. |
| `client/src/components/home/CollectionCards.jsx` | MOVE | `client/src/features/home/components/CollectionCards.jsx`. Behavior retained under client/src/features/home/components. |
| `client/src/components/home/CustomCta.jsx` | MOVE | `client/src/features/home/components/CustomCta.jsx`. Behavior retained under client/src/features/home/components. |
| `client/src/components/home/HeroCarousel.jsx` | MOVE | `client/src/features/home/components/HeroCarousel.jsx`. Behavior retained under client/src/features/home/components. |
| `client/src/components/home/HowItWorks.jsx` | MOVE | `client/src/features/home/components/HowItWorks.jsx`. Behavior retained under client/src/features/home/components. |
| `client/src/components/home/heroSlides.js` | MOVE | `client/src/features/home/components/heroSlides.js`. Behavior retained under client/src/features/home/components. |
| `client/src/components/storefront/AccountLayout.jsx` | MOVE | `client/src/features/account/components/AccountLayout.jsx`. Behavior retained under client/src/features/account/components. |
| `client/src/components/storefront/AuthShell.jsx` | MOVE | `client/src/features/auth/components/AuthShell.jsx`. Behavior retained under client/src/features/auth/components. |
| `client/src/components/storefront/CheckoutTotals.jsx` | MOVE | `client/src/features/checkout/components/CheckoutTotals.jsx`. Behavior retained under client/src/features/checkout/components. |
| `client/src/components/storefront/CmsSections.jsx` | MOVE | `client/src/features/content/components/CmsSections.jsx`. Behavior retained under client/src/features/content/components. |
| `client/src/components/storefront/ErrorBoundary.jsx` | MOVE | `client/src/shared/components/feedback/ErrorBoundary.jsx`. Behavior retained under client/src/shared/components/feedback. |
| `client/src/components/storefront/ProductCard.jsx` | MOVE | `client/src/features/catalog/components/ProductCard.jsx`. Behavior retained under client/src/features/catalog/components. |
| `client/src/components/storefront/Reviews.jsx` | SPLIT | Cohesive responsibilities now owned by reviews composition, card, form and state/actions. |
| `client/src/components/storefront/UI.jsx` | SPLIT | Cohesive responsibilities now owned by shared/components/ui and feedback primitives; catalog ProductPagination. |
| `client/src/components/storefront/storefront.test.jsx` | MOVE | `client/src/app/storefront.test.jsx`. Behavior retained under client/src/app. |
| `client/src/components/ui/ConfirmDialog.jsx` | MOVE | `client/src/shared/components/ui/ConfirmDialog.jsx`. Behavior retained under client/src/shared/components/ui. |
| `client/src/components/ui/Dialog.jsx` | MOVE | `client/src/shared/components/ui/Dialog.jsx`. Behavior retained under client/src/shared/components/ui. |
| `client/src/contexts/AuthContext.jsx` | RENAME | `client/src/features/auth/AuthProvider.jsx`. Behavior retained under client/src/features/auth. |
| `client/src/contexts/StoreContext.jsx` | SPLIT | Cohesive responsibilities now owned by cart/CartProvider, wishlist/WishlistProvider, content/SiteContentProvider, ToastProvider and authenticated mutation hooks. |
| `client/src/hooks/useCheckout.js` | MOVE | `client/src/features/checkout/hooks/useCheckout.js`. Behavior retained under client/src/features/checkout/hooks. |
| `client/src/hooks/useResource.js` | MOVE | `client/src/shared/hooks/useResource.js`. Behavior retained under client/src/shared/hooks. |
| `client/src/index.css` | MERGE | client/src/shared/styles/tokens.css and globals.css |
| `client/src/main.jsx` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/src/pages/AccountSecurity.jsx` | RENAME | `client/src/features/account/pages/AccountSecurityPage.jsx`. Behavior retained under client/src/features/account/pages. |
| `client/src/pages/Addresses.jsx` | RENAME | `client/src/features/account/pages/AddressesPage.jsx`. Behavior retained under client/src/features/account/pages. |
| `client/src/pages/Auth.css` | RENAME | `client/src/features/auth/auth.css`. Behavior retained under client/src/features/auth. |
| `client/src/pages/Cart.jsx` | RENAME | `client/src/features/cart/pages/CartPage.jsx`. Behavior retained under client/src/features/cart/pages. |
| `client/src/pages/Checkout.jsx` | SPLIT | Cohesive responsibilities now owned by checkout page, form and order review. |
| `client/src/pages/ContentPage.jsx` | MOVE | `client/src/features/content/pages/ContentPage.jsx`. Behavior retained under client/src/features/content/pages. |
| `client/src/pages/CustomBuilder.jsx` | SPLIT | Cohesive responsibilities now owned by custom-design page, controls, preview, drafts, quote and persistence/artwork modules. |
| `client/src/pages/CustomBuilder.test.jsx` | RENAME | `client/src/features/custom-design/designModel.test.jsx`. Behavior retained under client/src/features/custom-design. |
| `client/src/pages/ForgotPassword.jsx` | RENAME | `client/src/features/auth/pages/ForgotPasswordPage.jsx`. Behavior retained under client/src/features/auth/pages. |
| `client/src/pages/Home.jsx` | RENAME | `client/src/features/home/pages/HomePage.jsx`. Behavior retained under client/src/features/home/pages. |
| `client/src/pages/Login.jsx` | RENAME | `client/src/features/auth/pages/LoginPage.jsx`. Behavior retained under client/src/features/auth/pages. |
| `client/src/pages/NotFound.jsx` | RENAME | `client/src/app/pages/NotFoundPage.jsx`. Behavior retained under client/src/app/pages. |
| `client/src/pages/OrderDetails.jsx` | RENAME | `client/src/features/orders/pages/OrderDetailsPage.jsx`. Behavior retained under client/src/features/orders/pages. |
| `client/src/pages/Orders.jsx` | RENAME | `client/src/features/orders/pages/OrdersPage.jsx`. Behavior retained under client/src/features/orders/pages. |
| `client/src/pages/PaymentReturn.jsx` | RENAME | `client/src/features/payments/pages/PaymentReturnPage.jsx`. Behavior retained under client/src/features/payments/pages. |
| `client/src/pages/ProductDetails.jsx` | SPLIT | Cohesive responsibilities now owned by catalog page, detail/purchase section and gallery. |
| `client/src/pages/Products.jsx` | SPLIT | Cohesive responsibilities now owned by catalog page, filters, results, toolbar and query/state hook. |
| `client/src/pages/Profile.jsx` | RENAME | `client/src/features/account/pages/ProfilePage.jsx`. Behavior retained under client/src/features/account/pages. |
| `client/src/pages/ProofReview.jsx` | RENAME | `client/src/features/custom-design/pages/ProofReviewPage.jsx`. Behavior retained under client/src/features/custom-design/pages. |
| `client/src/pages/Register.jsx` | RENAME | `client/src/features/auth/pages/RegisterPage.jsx`. Behavior retained under client/src/features/auth/pages. |
| `client/src/pages/ResetPassword.jsx` | RENAME | `client/src/features/auth/pages/ResetPasswordPage.jsx`. Behavior retained under client/src/features/auth/pages. |
| `client/src/pages/SavedDesigns.jsx` | RENAME | `client/src/features/custom-design/pages/SavedDesignsPage.jsx`. Behavior retained under client/src/features/custom-design/pages. |
| `client/src/pages/VerifyEmail.jsx` | RENAME | `client/src/features/auth/pages/VerifyEmailPage.jsx`. Behavior retained under client/src/features/auth/pages. |
| `client/src/pages/Wishlisst.jsx` | RENAME | `client/src/features/wishlist/pages/WishlistPage.jsx`. Behavior retained under client/src/features/wishlist/pages. |
| `client/src/services/addressApi.js` | MOVE | `client/src/features/account/addressApi.js`. Behavior retained under client/src/features/account. |
| `client/src/services/api.js` | RENAME | `client/src/shared/api/httpClient.js`. Behavior retained under client/src/shared/api. |
| `client/src/services/apiError.test.ts` | MOVE | `client/src/shared/api/apiError.test.ts`. Behavior retained under client/src/shared/api. |
| `client/src/services/apiError.ts` | MOVE | `client/src/shared/api/apiError.ts`. Behavior retained under client/src/shared/api. |
| `client/src/services/artworkActions.js` | MOVE | `client/src/features/custom-design/artworkActions.js`. Behavior retained under client/src/features/custom-design. |
| `client/src/services/artworkActions.test.js` | MOVE | `client/src/features/custom-design/artworkActions.test.js`. Behavior retained under client/src/features/custom-design. |
| `client/src/services/authApi.js` | MOVE | `client/src/features/auth/authApi.js`. Behavior retained under client/src/features/auth. |
| `client/src/services/authSession.js` | MOVE | `client/src/shared/api/authSession.js`. Behavior retained under client/src/shared/api. |
| `client/src/services/authSession.test.ts` | MOVE | `client/src/shared/api/authSession.test.ts`. Behavior retained under client/src/shared/api. |
| `client/src/services/cartApi.js` | MOVE | `client/src/features/cart/cartApi.js`. Behavior retained under client/src/features/cart. |
| `client/src/services/checkoutApi.js` | SPLIT | Cohesive responsibilities now owned by checkout/checkoutApi, payments/paymentsApi, paymentNavigation and shared currency display. |
| `client/src/services/checkoutApi.test.jsx` | MOVE | `client/src/features/checkout/checkoutApi.test.jsx`. Behavior retained under client/src/features/checkout. |
| `client/src/services/clientTelemetry.js` | MOVE | `client/src/shared/api/clientTelemetry.js`. Behavior retained under client/src/shared/api. |
| `client/src/services/clientTelemetry.test.js` | MOVE | `client/src/shared/api/clientTelemetry.test.js`. Behavior retained under client/src/shared/api. |
| `client/src/services/config.js` | RENAME | `client/src/shared/api/requestConfig.js`. Behavior retained under client/src/shared/api. |
| `client/src/services/contentApi.js` | MOVE | `client/src/features/content/contentApi.js`. Behavior retained under client/src/features/content. |
| `client/src/services/customBuilderApi.js` | RENAME | `client/src/features/custom-design/customDesignApi.js`. Behavior retained under client/src/features/custom-design. |
| `client/src/services/orderApi.js` | RENAME | `client/src/features/orders/ordersApi.js`. Behavior retained under client/src/features/orders. |
| `client/src/services/productApi.js` | RENAME | `client/src/features/catalog/catalogApi.js`. Behavior retained under client/src/features/catalog. |
| `client/src/services/reviewApi.js` | RENAME | `client/src/features/reviews/reviewsApi.js`. Behavior retained under client/src/features/reviews. |
| `client/src/services/uploadApi.js` | RENAME | `client/src/features/account/avatarApi.js`. Behavior retained under client/src/features/account. |
| `client/src/services/userApi.js` | RENAME | `client/src/features/account/accountApi.js`. Behavior retained under client/src/features/account. |
| `client/src/services/wishlistApi.js` | MOVE | `client/src/features/wishlist/wishlistApi.js`. Behavior retained under client/src/features/wishlist. |
| `client/src/styles/storefront.css` | SPLIT | Cohesive responsibilities now owned by shared tokens/primitives and layout/feature-owned CSS. |
| `client/src/types/api.ts` | REMOVE | retired unused diagnostic-only types; active DTOs are feature-owned |
| `client/src/types/storefront.ts` | RENAME | `client/src/features/catalog/catalogContracts.ts`. Behavior retained under client/src/features/catalog. |
| `client/src/utils/customBuilder.js` | RENAME | `client/src/features/custom-design/designModel.js`. Behavior retained under client/src/features/custom-design. |
| `client/src/utils/permissions.js` | REMOVE | LEGACY SHIM: all callers now import the single app/config/permissions module directly. |
| `client/src/utils/seo.js` | MOVE | `client/src/app/seo/seo.js`. Behavior retained under client/src/app/seo. |
| `client/src/utils/seo.test.js` | MOVE | `client/src/app/seo/seo.test.js`. Behavior retained under client/src/app/seo. |
| `client/src/utils/storefront.test.ts` | RENAME | `client/src/shared/utils/presentation.test.ts`. Behavior retained under client/src/shared/utils. |
| `client/src/utils/storefront.ts` | SPLIT | Cohesive responsibilities now owned by shared formatting/quantity/links/apiError and catalog/catalogQuery. |
| `client/src/vite-env.d.ts` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/tsconfig.json` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |
| `client/vite.config.js` | KEEP | Retained active file; updated composition, tooling, test imports or documentation for the final architecture. |

## Retired files and proof

Each entry has no remaining active static import, dynamic import, route, test import, stylesheet/build reference or runtime-string consumer. Old paths in this audit and the historical admin report are intentional records.

- `client/src/components/storefront/UI.jsx` — SPLIT: retired mixed UI bundle and dialog re-export shim; all consumers import the owning primitive directly.
- `client/src/utils/storefront.ts` — SPLIT: currency, date, quantity, safe links, safe API errors and catalog query handling now have explicit owners; no remaining bundle callers.
- `client/src/shared/api/contracts.ts` — UNUSED: ProblemDetails and ApiErrorSummary supported only retired normalizeApiError; PagedResult had no consumers. Feature-specific response contracts remain.
- `client/src/contexts/StoreContext.jsx` — SPLIT: cart/wishlist caches, site settings, authentication-required actions and notifications now have focused owners; no static, dynamic or test callers remain.
- `client/src/utils/permissions.js` — LEGACY SHIM: all callers now import the single app/config/permissions module directly.
- `client/src/features/admin/shared/utils/formatDateTime.js` — MERGE: date/date-time display now lives in shared/utils/formatDate.ts; admin local formatting and customer Asia/Colombo formatting are preserved explicitly.
- `client/src/index.css` — MERGE: active root tokens and reset rules now live in shared/styles/tokens.css and globals.css.
- `client/src/styles/storefront.css` — SPLIT: active styles now belong to feature/layout CSS and shared design primitives; six unused profile/order selectors and redundant box-sizing removed after source/runtime-string audit.
- `client/src/app/App.css` — MERGE: active application flex shell rules consolidated into shared/styles/globals.css.
- `package-lock.json` — UNUSED: empty orphaned lockfile with no root package.json, dependencies or workflow callers; client/package-lock.json remains authoritative.
- `client/e2e/accessibility.spec.ts` — SPLIT: public/admin accessibility journeys now live with their owners; shared Axe assertion extracted to e2e/accessibility.ts.

## Removed API operations and exports

- `client/src/features/auth/authApi.js` / `getCurrentUser` — UNUSED: no static/dynamic call, route, test, computed property or runtime-string consumer in the complete frontend inventory.
- `client/src/features/account/accountApi.js` / `getUserStats` — UNUSED: no static/dynamic call, route, test, computed property or runtime-string consumer in the complete frontend inventory.
- `client/src/features/account/accountApi.js` / `getAvatar` — UNUSED: no static/dynamic call, route, test, computed property or runtime-string consumer in the complete frontend inventory.
- `client/src/features/wishlist/wishlistApi.js` / `clearWishlist` — UNUSED: no static/dynamic call, route, test, computed property or runtime-string consumer in the complete frontend inventory.
- `client/src/services/apiError.ts` / `normalizeApiError` — OBSOLETE: only its own tests called this unsafe diagnostic formatter; customerError is the active safe, status-aware error interface.
- `client/src/features/admin/features/customers/customersApi.js` / `getCustomerDetails` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/customers/customersApi.js` / `getCustomerOrders` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/customers/customersApi.js` / `getCustomerStats` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/dashboard/dashboardApi.js` / `getRevenueStats` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/dashboard/dashboardApi.js` / `getOrderStats` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/products/productsApi.js` / `getProduct` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.
- `client/src/features/admin/features/products/productsApi.js` / `updateProductVariant` — UNUSED: full source/E2E/runtime-string search found only the definition; no computed API calls exist. Active operations/customer/dashboard/product workflows use other named operations.

Additionally removed 10 unused import bindings after counting both JavaScript and JSX references.

## Removed empty directories

- `client/src/admin/features/audit`
- `client/src/admin/features/builder-configuration`
- `client/src/admin/features/categories`
- `client/src/admin/features/collections`
- `client/src/admin/features/content`
- `client/src/admin/features/customers`
- `client/src/admin/features/dashboard`
- `client/src/admin/features/inventory`
- `client/src/admin/features/media`
- `client/src/admin/features/orders`
- `client/src/admin/features/payments`
- `client/src/admin/features/production`
- `client/src/admin/features/products`
- `client/src/admin/features/promotions`
- `client/src/admin/features/reports`
- `client/src/admin/features/reviews`
- `client/src/admin/features/settings`
- `client/src/admin/features/shipping`
- `client/src/admin/features/staff`
- `client/src/admin/shared/components`
- `client/src/admin/shared/hooks`
- `client/src/admin/shared/styles`
- `client/src/admin/shared/utils`
- `client/src/admin/app`
- `client/src/admin/features`
- `client/src/admin/layout`
- `client/src/admin/shared`
- `client/src/components/home`
- `client/src/components/storefront`
- `client/src/components/ui`
- `client/src/pages/admin`
- `client/src/admin`
- `client/src/components`
- `client/src/contexts`
- `client/src/hooks`
- `client/src/pages`
- `client/src/services`
- `client/src/styles`
- `client/src/types`
- `client/src/utils`

## New cohesive modules and tests

- `client/e2e/accessibility.ts`
- `client/e2e/account/auth-session.spec.ts`
- `client/e2e/admin/accessibility.spec.ts`
- `client/e2e/admin/checkout-payments.spec.ts`
- `client/e2e/checkout/checkoutFixture.ts`
- `client/e2e/storefront/accessibility.spec.ts`
- `client/e2e/storefront/custom-design-state.spec.ts`
- `client/src/app/config/navigation.js`
- `client/src/app/layout/storefrontLayout.css`
- `client/src/app/providers/AppProviders.jsx`
- `client/src/app/providers/StorefrontProviders.jsx`
- `client/src/app/routes/AccountRoutes.jsx`
- `client/src/app/routes/AppRoutes.jsx`
- `client/src/app/routes/PublicRoutes.jsx`
- `client/src/features/account/account.css`
- `client/src/features/account/addressForm.js`
- `client/src/features/account/components/AccountSecurityForm.jsx`
- `client/src/features/account/components/AddressForm.jsx`
- `client/src/features/account/components/ProfileForm.jsx`
- `client/src/features/account/hooks/useAccountSecurity.js`
- `client/src/features/account/hooks/useAddresses.js`
- `client/src/features/auth/components/ForgotPasswordForm.jsx`
- `client/src/features/auth/components/LoginForm.jsx`
- `client/src/features/auth/components/RegisterForm.jsx`
- `client/src/features/auth/components/ResetPasswordForm.jsx`
- `client/src/features/auth/components/VerifyEmailForm.jsx`
- `client/src/features/auth/hooks/useAuthenticatedMutation.js`
- `client/src/features/auth/hooks/useForgotPassword.js`
- `client/src/features/auth/hooks/useLogin.js`
- `client/src/features/auth/hooks/useRegister.js`
- `client/src/features/auth/hooks/useRequireLogin.js`
- `client/src/features/auth/hooks/useResetPassword.js`
- `client/src/features/auth/hooks/useVerifyEmail.js`
- `client/src/features/cart/CartProvider.jsx`
- `client/src/features/cart/cart.css`
- `client/src/features/cart/components/CartItem.jsx`
- `client/src/features/cart/components/CartSummary.jsx`
- `client/src/features/cart/hooks/useCartPage.js`
- `client/src/features/catalog/catalog.css`
- `client/src/features/catalog/catalogQuery.test.ts`
- `client/src/features/catalog/catalogQuery.ts`
- `client/src/features/catalog/components/CatalogResults.jsx`
- `client/src/features/catalog/components/CatalogToolbar.jsx`
- `client/src/features/catalog/components/ProductDetail.jsx`
- `client/src/features/catalog/components/ProductFilters.jsx`
- `client/src/features/catalog/components/ProductGallery.jsx`
- `client/src/features/catalog/components/ProductPagination.jsx`
- `client/src/features/catalog/components/ProductSeo.jsx`
- `client/src/features/catalog/hooks/useCatalog.js`
- `client/src/features/catalog/productSeo.js`
- `client/src/features/checkout/checkout.css`
- `client/src/features/checkout/checkoutContracts.ts`
- `client/src/features/checkout/components/CheckoutForm.jsx`
- `client/src/features/checkout/components/CheckoutReview.jsx`
- `client/src/features/content/SiteContentProvider.jsx`
- `client/src/features/content/components/CmsSections.test.jsx`
- `client/src/features/content/content.css`
- `client/src/features/content/contentContracts.ts`
- `client/src/features/content/contentModel.js`
- `client/src/features/content/contentModel.test.js`
- `client/src/features/custom-design/builderDraft.js`
- `client/src/features/custom-design/builderDraft.test.js`
- `client/src/features/custom-design/components/BuilderControls.jsx`
- `client/src/features/custom-design/components/BuilderPreview.jsx`
- `client/src/features/custom-design/components/SavedDesignCard.jsx`
- `client/src/features/custom-design/customDesign.css`
- `client/src/features/custom-design/hooks/useBuilderQuote.js`
- `client/src/features/custom-design/hooks/useCustomBuilder.js`
- `client/src/features/custom-design/hooks/useProofReview.js`
- `client/src/features/custom-design/hooks/useSavedDesigns.js`
- `client/src/features/home/home.css`
- `client/src/features/orders/components/OrderItems.jsx`
- `client/src/features/orders/orderContracts.ts`
- `client/src/features/orders/orders.css`
- `client/src/features/payments/hooks/useCheckoutPayment.js`
- `client/src/features/payments/hooks/usePaymentReturn.js`
- `client/src/features/payments/paymentContracts.ts`
- `client/src/features/payments/paymentNavigation.js`
- `client/src/features/payments/paymentsApi.js`
- `client/src/features/reviews/components/ReviewCard.jsx`
- `client/src/features/reviews/components/ReviewForm.jsx`
- `client/src/features/reviews/hooks/useProductReviews.js`
- `client/src/features/reviews/reviews.css`
- `client/src/features/wishlist/WishlistProvider.jsx`
- `client/src/features/wishlist/components/WishlistItem.jsx`
- `client/src/features/wishlist/hooks/useWishlistPage.js`
- `client/src/features/wishlist/wishlist.css`
- `client/src/shared/components/feedback/EmptyState.jsx`
- `client/src/shared/components/feedback/ErrorState.jsx`
- `client/src/shared/components/feedback/Skeleton.jsx`
- `client/src/shared/components/feedback/ToastProvider.jsx`
- `client/src/shared/components/seo/JsonLd.jsx`
- `client/src/shared/components/ui/Button.jsx`
- `client/src/shared/components/ui/Media.jsx`
- `client/src/shared/components/ui/PageHeader.jsx`
- `client/src/shared/components/ui/Price.jsx`
- `client/src/shared/components/ui/Quantity.jsx`
- `client/src/shared/components/ui/SectionHeading.jsx`
- `client/src/shared/hooks/useMutation.js`
- `client/src/shared/styles/globals.css`
- `client/src/shared/styles/tokens.css`
- `client/src/shared/utils/formatCurrency.ts`
- `client/src/shared/utils/formatDate.ts`
- `client/src/shared/utils/publicBase.js`
- `client/src/shared/utils/quantity.ts`
- `client/src/shared/utils/safeJson.ts`
- `client/src/shared/utils/safeLink.ts`

## Dependency, asset and root review

- All installed dependencies have active source/tool/script consumers; package manifest and client lockfile versions are unchanged. `react-icons` is required by the centralized Icon module.
- `public/favicon.svg` is referenced by `index.html`. There are no duplicate local image/font directories. API/CMS/private URLs and configured Google Fonts were retained.
- The root lockfile contained no packages and had no root package manifest or workflow consumer; it was retired. Client tooling/configuration and the active client lockfile remain.
- `node_modules`, `dist`, coverage, screenshots, browser reports/results, private environments and runtime logs/backups are ignored. No generated files were staged.
