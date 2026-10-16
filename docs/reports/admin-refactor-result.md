# ADMIN REFACTOR RESULT

Historical record of commit `4952565`. The subsequent whole-frontend migration is documented in [Frontend refactor result](frontend-refactor-result.md); current paths are in [Frontend architecture](../architecture/frontend.md).

Date: 16 October 2026. Verdict: **COMPLETE** for the requested frontend architecture refactor.

## BEFORE

- Admin pages mixed request lifecycles, form state, endpoint construction, mutations and large rendering sections.
- Order detail embedded payment/refund/idempotency operations and shipment forms. Production detail combined assets, proofs, transitions and historical JSON parsing.
- Inventory movement history had no frontend pager. Product list responses could arrive out of order.
- OperationsUI mixed reusable presentation with a mutation hook returning JSX. Route/permission/navigation declarations were maintained separately.
- Admin and dashboard styles included broad selectors; raw historical JSON parsing could crash detail pages.

Inspection began with a clean `develop` working tree at `01156fd`; status, branch, recent history, diff statistics and whitespace checks were recorded before edits. Ownership classification: keep shared transport/auth/useResource and storefront behavior; move admin routing/layout and feature endpoints; split order/payment/shipping/production and long forms/tables; rename legacy pages; remove the retired stock API, obsolete services and temporary migration shims. No enduring deprecated endpoint aliases were added.

## NEW ARCHITECTURE

| Area | Result |
| --- | --- |
| app | AdminRoutes, AdminRouteGuard, AdminLanding, shared route/navigation metadata and role/permission definitions. |
| layout | Shell, header, sidebar, navigation item, user display and a responsive navigation hook. |
| shared | Domain-neutral presentation, mutation/list hooks, download/date/pagination helpers and scoped tokens/base styles. |
| features | 19 directories own 22 page compositions, meaningful forms/tables/sections, coordination hooks and named feature APIs. |
| services | Existing authenticated HTTP, auth/session, normalized errors and telemetry remain shared. Storefront APIs retain their public operations; admin methods moved to their features. |
| tests | Colocated focused tests plus preserved/new admin Playwright suites under client/e2e/admin. |

The maintainers' guide is [admin-architecture.md](../architecture/admin.md), linked from README. It records ownership, dependency direction, financial safeguards, routing, CSS, contracts and test placement.

## FILES MOVED

The following page responsibilities migrated and were split into feature modules; these are logical migrations, not claims that every destination is a verbatim Git rename.

| Previous file under client/src/pages/admin | New page under client/src/admin/features |
| --- | --- |
| `Dashboard.jsx` | [`dashboard/DashboardPage.jsx`](../../client/src/features/admin/features/dashboard/DashboardPage.jsx) |
| `AdProducts.jsx` | [`products/ProductsPage.jsx`](../../client/src/features/admin/features/products/ProductsPage.jsx) |
| `Categories.jsx` | [`categories/CategoriesPage.jsx`](../../client/src/features/admin/features/categories/CategoriesPage.jsx) |
| `Collections.jsx` | [`collections/CollectionsPage.jsx`](../../client/src/features/admin/features/collections/CollectionsPage.jsx) |
| `Media.jsx` | [`media/MediaPage.jsx`](../../client/src/features/admin/features/media/MediaPage.jsx) |
| `Content.jsx` | [`content/ContentPage.jsx`](../../client/src/features/admin/features/content/ContentPage.jsx) |
| `Settings.jsx` | [`settings/SettingsPage.jsx`](../../client/src/features/admin/features/settings/SettingsPage.jsx) |
| `AdOrders.jsx` | [`orders/OrdersPage.jsx`](../../client/src/features/admin/features/orders/OrdersPage.jsx) |
| `OrderDetail.jsx` | [`orders/OrderDetailPage.jsx`](../../client/src/features/admin/features/orders/OrderDetailPage.jsx) |
| `Customers.jsx` | [`customers/CustomersPage.jsx`](../../client/src/features/admin/features/customers/CustomersPage.jsx) |
| `CustomerDetail.jsx` | [`customers/CustomerDetailPage.jsx`](../../client/src/features/admin/features/customers/CustomerDetailPage.jsx) |
| `Payments.jsx` | [`payments/PaymentsPage.jsx`](../../client/src/features/admin/features/payments/PaymentsPage.jsx) |
| `Shipping.jsx` | [`shipping/ShippingPage.jsx`](../../client/src/features/admin/features/shipping/ShippingPage.jsx) |
| `CustomDesigns.jsx` | [`production/ProductionQueuePage.jsx`](../../client/src/features/admin/features/production/ProductionQueuePage.jsx) |
| `CustomDesignDetail.jsx` | [`production/ProductionJobPage.jsx`](../../client/src/features/admin/features/production/ProductionJobPage.jsx) |
| `CustomBuilderConfig.jsx` | [`builder-configuration/BuilderConfigurationPage.jsx`](../../client/src/features/admin/features/builder-configuration/BuilderConfigurationPage.jsx) |
| `Inventory.jsx` | [`inventory/InventoryPage.jsx`](../../client/src/features/admin/features/inventory/InventoryPage.jsx) |
| `Promotions.jsx` | [`promotions/PromotionsPage.jsx`](../../client/src/features/admin/features/promotions/PromotionsPage.jsx) |
| `Reviews.jsx` | [`reviews/ReviewsPage.jsx`](../../client/src/features/admin/features/reviews/ReviewsPage.jsx) |
| `Reports.jsx` | [`reports/ReportsPage.jsx`](../../client/src/features/admin/features/reports/ReportsPage.jsx) |
| `Audit.jsx` | [`audit/AuditPage.jsx`](../../client/src/features/admin/features/audit/AuditPage.jsx) |
| `Staff.jsx` | [`staff/StaffPage.jsx`](../../client/src/features/admin/features/staff/StaffPage.jsx) |

Additional migrations:

- `pages/admin/AdminLayout.jsx` -> `admin/layout` shell modules.
- `pages/admin/AdminStyles.css` -> scoped shared tokens/base CSS and layout/dashboard CSS modules.
- `pages/admin/OperationsUI.jsx` -> shared presentation and state-only mutation hook.
- `services/adminApi.js`, `checkoutAdminApi.js` and `operationsApi.js` -> named feature APIs; admin operations were also removed from mixed storefront service files.
- `services/refundNotice.js` and its tests -> `admin/features/payments`.
- Shared Dialog/ConfirmDialog implementations -> `components/ui`; storefront UI keeps its existing exports.
- `e2e/operations.spec.ts` -> `e2e/admin/operations.spec.ts`; `e2e/refund.spec.ts` -> `e2e/admin/payments.spec.ts`. Existing assertions remain.

## FILES CREATED

Complete destination/new-file inventory, grouped by directory. Some files replace the migrated sources listed above.

- `client/e2e/admin/`: `architecture.spec.ts`, `catalog.spec.ts`, `operations.spec.ts`, `payments.spec.ts`.
- `client/src/admin/app/`: `AdminLanding.jsx`, `AdminRouteGuard.jsx`, `AdminRoutes.jsx`, `adminNavigation.js`, `adminPermissions.js`.
- `client/src/admin/features/audit/`: `AuditFilters.jsx`, `AuditPage.jsx`, `AuditTable.jsx`, `auditApi.js`, `useAudit.js`.
- `client/src/admin/features/builder-configuration/`: `BuilderConfigurationPage.jsx`, `BuilderOptionsEditor.jsx`, `BuilderPricingEditor.jsx`, `BuilderVersionSummary.jsx`, `builderConfigurationApi.js`, `builderForm.js`, `useBuilderConfiguration.js`.
- `client/src/admin/features/categories/`: `CategoriesPage.jsx`, `CategoriesTable.jsx`, `CategoryForm.jsx`, `categoriesApi.js`, `categoriesForm.js`, `useCategories.js`.
- `client/src/admin/features/collections/`: `CollectionForm.jsx`, `CollectionsPage.jsx`, `CollectionsTable.jsx`, `collectionsApi.js`, `collectionsForm.js`, `useCollections.js`.
- `client/src/admin/features/content/`: `ContentForm.jsx`, `ContentPage.jsx`, `ContentPagesTable.jsx`, `contentApi.js`, `contentForm.js`, `useContent.js`.
- `client/src/admin/features/customers/`: `CustomerActivity.jsx`, `CustomerAddresses.jsx`, `CustomerDetailPage.jsx`, `CustomerSummary.jsx`, `CustomersPage.jsx`, `CustomersTable.jsx`, `customersApi.js`, `useCustomers.js`.
- `client/src/admin/features/dashboard/`: `Dashboard.module.css`, `DashboardPage.jsx`, `DashboardSummary.jsx`, `RecentOrdersTable.jsx`, `TopProductsList.jsx`, `dashboardApi.js`, `useDashboard.js`.
- `client/src/admin/features/inventory/`: `InventoryAdjustmentForm.jsx`, `InventoryMovementsTable.jsx`, `InventoryPage.jsx`, `InventorySections.test.jsx`, `StockBalancesTable.jsx`, `inventoryApi.js`, `inventoryContracts.ts`, `useInventory.js`.
- `client/src/admin/features/media/`: `MediaPage.jsx`, `MediaTable.jsx`, `mediaApi.js`.
- `client/src/admin/features/orders/`: `OrderDetailPage.jsx`, `OrderFilters.jsx`, `OrderItems.jsx`, `OrderSections.test.jsx`, `OrderSummary.jsx`, `OrderTimeline.jsx`, `OrderTotals.jsx`, `OrderTransitionForm.jsx`, `OrdersPage.jsx`, `OrdersTable.jsx`, `orderContracts.ts`, `ordersApi.js`, `useOrderDetail.js`, `useOrders.js`.
- `client/src/admin/features/payments/`: `OrderPayments.jsx`, `PaymentActions.jsx`, `PaymentActions.test.jsx`, `PaymentAttemptPanel.jsx`, `PaymentFilters.jsx`, `PaymentsPage.jsx`, `PaymentsTable.jsx`, `RefundForm.jsx`, `RefundHistory.jsx`, `paymentContracts.ts`, `paymentsApi.js`, `refundNotice.js`, `refundNotice.test.js`, `usePaymentOperations.js`, `usePayments.js`.
- `client/src/admin/features/production/`: `ArchiveJobForm.jsx`, `PrivateAssetsList.jsx`, `ProductionFilters.jsx`, `ProductionJobPage.jsx`, `ProductionJobSummary.jsx`, `ProductionQueuePage.jsx`, `ProductionQueueTable.jsx`, `ProductionTimeline.jsx`, `ProductionTransitionForm.jsx`, `ProductionTransitionForm.test.jsx`, `ProofRevisionHistory.jsx`, `ProofUploadForm.jsx`, `productionApi.js`, `productionContracts.ts`, `useProductionJob.js`, `useProductionQueue.js`.
- `client/src/admin/features/products/`: `ProductForm.jsx`, `ProductMediaEditor.jsx`, `ProductTools.jsx`, `ProductVariantsEditor.jsx`, `ProductsPage.jsx`, `ProductsTable.jsx`, `productForm.js`, `productsApi.js`, `useProductTools.js`, `useProducts.js`.
- `client/src/admin/features/promotions/`: `PromotionForm.jsx`, `PromotionRedemptionsTable.jsx`, `PromotionsPage.jsx`, `PromotionsTable.jsx`, `promotionForm.js`, `promotionsApi.js`, `usePromotions.js`.
- `client/src/admin/features/reports/`: `ReportDateFilters.jsx`, `ReportTables.jsx`, `ReportsPage.jsx`, `reportsApi.js`, `useReports.js`.
- `client/src/admin/features/reviews/`: `ReviewModerationForm.jsx`, `ReviewsPage.jsx`, `ReviewsTable.jsx`, `reviewsApi.js`, `useReviews.js`.
- `client/src/admin/features/settings/`: `SettingForm.jsx`, `SettingsList.jsx`, `SettingsPage.jsx`, `settingsApi.js`, `settingsForm.js`, `useSettings.js`.
- `client/src/admin/features/shipping/`: `OrderShipments.jsx`, `ShipmentForm.jsx`, `ShippingMethodForm.jsx`, `ShippingPage.jsx`, `ShippingZoneForm.jsx`, `shippingApi.js`, `shippingForm.js`, `useShippingConfiguration.js`.
- `client/src/admin/features/staff/`: `StaffPage.jsx`, `StaffRoleForm.jsx`, `StaffTable.jsx`, `staffApi.js`, `useStaff.js`.
- `client/src/admin/layout/`: `AdminHeader.jsx`, `AdminLayout.jsx`, `AdminLayout.module.css`, `AdminNavigationItem.jsx`, `AdminSidebar.jsx`, `AdminUserMenu.jsx`, `useAdminNavigation.js`.
- `client/src/admin/shared/components/`: `AdminPage.jsx`, `DataTable.jsx`, `EmptyState.jsx`, `FilterBar.jsx`, `MutationFeedback.jsx`, `PageHeader.jsx`, `Pagination.jsx`, `Pagination.test.jsx`, `ResourceState.jsx`, `ResourceState.test.jsx`, `SectionCard.jsx`, `SnapshotViewer.jsx`, `SnapshotViewer.test.jsx`, `StatusBadge.jsx`.
- `client/src/admin/shared/hooks/`: `useAdminListParams.js`, `useAdminMutation.js`.
- `client/src/admin/shared/styles/`: `adminBase.css`, `adminTokens.css`.
- `client/src/admin/shared/utils/`: `downloadFile.js`, `formatDateTime.js`, `pagination.js`.
- `client/src/components/ui/`: `ConfirmDialog.jsx`, `Dialog.jsx`.
- `docs/`: `admin-architecture.md`, `admin-refactor-result.md`.

## FILES REMOVED

- `client/e2e/operations.spec.ts`
- `client/e2e/refund.spec.ts`
- `client/src/pages/admin/AdOrders.jsx`
- `client/src/pages/admin/AdProducts.jsx`
- `client/src/pages/admin/AdminLayout.jsx`
- `client/src/pages/admin/AdminStyles.css`
- `client/src/pages/admin/Audit.jsx`
- `client/src/pages/admin/Categories.jsx`
- `client/src/pages/admin/Collections.jsx`
- `client/src/pages/admin/Content.jsx`
- `client/src/pages/admin/CustomBuilderConfig.jsx`
- `client/src/pages/admin/CustomDesignDetail.jsx`
- `client/src/pages/admin/CustomDesigns.jsx`
- `client/src/pages/admin/CustomerDetail.jsx`
- `client/src/pages/admin/Customers.jsx`
- `client/src/pages/admin/Dashboard.jsx`
- `client/src/pages/admin/Inventory.jsx`
- `client/src/pages/admin/Media.jsx`
- `client/src/pages/admin/OperationsUI.jsx`
- `client/src/pages/admin/OrderDetail.jsx`
- `client/src/pages/admin/Payments.jsx`
- `client/src/pages/admin/Promotions.jsx`
- `client/src/pages/admin/Reports.jsx`
- `client/src/pages/admin/Reviews.jsx`
- `client/src/pages/admin/Settings.jsx`
- `client/src/pages/admin/Shipping.jsx`
- `client/src/pages/admin/Staff.jsx`
- `client/src/services/adminApi.js`
- `client/src/services/checkoutAdminApi.js`
- `client/src/services/operationsApi.js`
- `client/src/services/refundNotice.js`
- `client/src/services/refundNotice.test.js`

The old admin page tree and OperationsUI have no remaining consumers. No compatibility page shims remain. The retired `adjustProductStock` method was deleted without a replacement alias; stock adjustment is exposed through `inventoryApi.adjustStock` only.

## PAGES SIMPLIFIED

| Page | Remaining responsibility |
| --- | --- |
| `DashboardPage` | Load the dashboard hook and compose summary, recent orders and top products. |
| `ProductsPage` | Compose filters, guarded product results, pagination, the product form and keyed product tools. |
| `CategoriesPage` | Compose category count, resource/mutation feedback, the category table and form. |
| `CollectionsPage` | Compose collections, product-selection form and feedback; the hook owns the two-step save. |
| `MediaPage` | Load the media resource and compose the media table and feedback. |
| `ContentPage` | Compose CMS page count, page table and section editor; mutations live in useContent. |
| `SettingsPage` | Compose settings count, settings list and setting form. |
| `OrdersPage` | Compose order filters, resource state, OrdersTable and Pagination. |
| `OrderDetailPage` | Read the order ID, load one authoritative order, check action permissions and compose order/payment/shipment sections. |
| `CustomersPage` | Compose customer search, customer table and pagination around useCustomers. |
| `CustomerDetailPage` | Read the customer ID and compose summary, saved addresses and recent activity from one response. |
| `PaymentsPage` | Compose payment/environment filters, attempts table and pagination. |
| `ShippingPage` | Compose shipping configuration feedback and the zone/method forms. |
| `ProductionQueuePage` | Compose production filters, jobs table and pagination. |
| `ProductionJobPage` | Read the job ID and compose specification, assets, proofs, transition, timeline and archive confirmation. |
| `BuilderConfigurationPage` | Compose configuration version/actions, limits/pricing, options and version history. |
| `InventoryPage` | Compose balance search/table, adjustment confirmation and an independently paged movement table. |
| `PromotionsPage` | Compose promotion editing, rules, redemption history and pagination. |
| `ReviewsPage` | Compose moderation filters, table, feedback and confirmation; useReviews owns the request. |
| `ReportsPage` | Compose date/export controls and currency/environment-separated report tables. |
| `AuditPage` | Compose audit filters, evidence table and pagination. |
| `StaffPage` | Compose staff table, role form and confirmation; useStaff owns the role-replacement request. |

The 22 formatted feature pages are 22-84 lines, used as an inspection aid rather than a line-count rule. They contain no raw HTTP calls, backend endpoint construction, DataTable column implementations or JSON parsing.

## FEATURE API MIGRATION

Each module below uses the existing authenticated HTTP client and exposes named operations. No new transport, auth mechanism or backend route was introduced.

- `audit/auditApi.js`: `listEntries`.
- `builder-configuration/builderConfigurationApi.js`: `getConfig`, `updateDraft`, `testQuote`, `publish`.
- `categories/categoriesApi.js`: `getCategories`, `createCategory`, `updateCategory`, `archiveCategory`, `reactivateCategory`.
- `collections/collectionsApi.js`: `getCollections`, `createCollection`, `updateCollection`, `replaceCollectionProducts`, `archiveCollection`, `reactivateCollection`.
- `content/contentApi.js`: `getPages`, `createPage`, `updatePage`.
- `customers/customersApi.js`: `getOperations`, `getAllCustomers`, `getCustomerDetails`, `getCustomerOrders`, `getCustomerStats`.
- `dashboard/dashboardApi.js`: `getDashboardStats`, `getRevenueStats`, `getOrderStats`, `getProductStats`, `getRecentOrders`.
- `inventory/inventoryApi.js`: `getBalances`, `getMovements`, `adjustStock`.
- `media/mediaApi.js`: `getMedia`.
- `orders/ordersApi.js`: `listOrders`, `getOrder`, `updateStatus`.
- `payments/paymentsApi.js`: `listPayments`, `getPayment`, `reconcilePayment`, `refundPayment`, `reconcileRefund`, `settlePayment`, `reverseAuthorization`.
- `production/productionApi.js`: `listJobs`, `getJob`, `updateProductionStatus`, `uploadProof`, `archiveJob`, `downloadAsset`.
- `products/productsApi.js`: `getProducts`, `getProduct`, `createProduct`, `updateProduct`, `archiveProduct`, `reactivateProduct`, `uploadProductMedia`, `getProductMedia`, `updateProductMedia`, `getProductVariants`, `createProductVariant`, `updateProductVariant`.
- `promotions/promotionsApi.js`: `listPromotions`, `getRedemptions`, `createPromotion`, `updatePromotion`.
- `reports/reportsApi.js`: `getReports`, `exportOrders`.
- `reviews/reviewsApi.js`: `listReviews`, `moderateReview`.
- `settings/settingsApi.js`: `getSettings`, `saveSetting`.
- `shipping/shippingApi.js`: `getConfiguration`, `saveZone`, `saveMethod`, `updateShipment`.
- `staff/staffApi.js`: `getStaff`, `replaceRoles`.

## PAYMENT EXTRACTION RESULT

`OrderDetailPage` mounts `OrderPayments` with the parent's payment summaries and reload callback. Each PaymentAttemptPanel uses usePaymentOperations and separate action/refund presentation. Provider details, event/reconciliation history and all historical attempts remain visible.

Preserved: financial confirmation, synchronous duplicate-action lock, stable settlement/reversal keys, refund identity/idempotency-key reuse during ambiguous results, terminal-result key reset, provider availability, pending mutation guards and authoritative refreshes. A 204 refund reconciliation reads the real refund status; HTTP 200 Failed/Pending responses never announce completion. Failed refreshes retain the previous trusted payment snapshot and block unsafe mutations until recovery. The existing refund regressions all pass, including identical sequential partial refunds, full refunds, pending reconciliation, HTTP rejection, stale requests and refresh recovery.

## SHIPPING EXTRACTION RESULT

OrderShipments owns shipment display, tracking corrections and update coordination; ShipmentForm owns the full fulfilment form. ShippingPage composes independently owned zone/method forms and the configuration hook. State transitions, pickup behavior, historical values, permissions and recovery feedback remain. The real proof-to-production-to-shipment browser flow passes.

## PRODUCTION EXTRACTION RESULT

ProductionJobPage composes job/specification display, assets, proof history/upload, production transition, timeline and archive confirmation. useProductionJob owns private downloads and mutations; productionApi owns endpoints. Server-approved transitions, rowVersion concurrency, customer-visible messages and proof gating remain intact. No fabricated admin request-revision API was added: customer proof decisions retain their existing storefront/server ownership.

## INVENTORY PAGINATION RESULT

Balance and movement page state/request keys are independent. Selecting or closing a product adjustment resets movement history to page 1. Balance pagination consumes server metadata; movement pagination uses the existing server totalCount and fixed 50-row contract. Adjustment reasons, expectedAvailable concurrency and confirmation remain. Unit tests and controlled multi-page browser windows verify bounds, independence and filter reset; the existing ledger-write test verifies the actual backend.

## PRODUCT STALE-REQUEST RESULT

useProducts and keyed product tools reuse useResource cancellation/generation guards. An older/slower response cannot overwrite newer filters or a different product selection. A deliberately uncancellable browser loader confirms the filter result guard. Real product create/edit/archive/reactivation, variant creation and media upload pass. New catalog tests restore the shared category and archive generated products, preserving existing fixture invariants and storefront results.

## ROUTING / PERMISSION RESULT

All 22 existing admin paths are present in the new metadata; role memberships and landing preferences match the baseline. The shell filters the same metadata used by the route registry/guard. Direct entry, all 19 navigation targets, detail routes, reload, support restrictions, backend 403s and mobile navigation pass. Authentication/session restoration and backend policies remain authoritative and unchanged.

## CSS REFACTOR RESULT

Every shared admin selector is scoped to `.admin-app`; layout and dashboard have CSS modules, and animation names are admin-specific. Existing structure/spacing is retained. Confirmed contrast defects were corrected with darker existing success/danger/warning hues. Extracted catalog/content/builder controls gained accessible names and explicit table headings. WCAG checks and mobile Escape/focus behavior pass. The CSS parser scope audit finds no leaking shared selector.

## SNAPSHOT SAFETY RESULT

SnapshotViewer catches malformed historical JSON, formats valid data and escapes fallback text. Order item snapshots, production specifications and production timeline metadata use it. Focused tests and full detail-page browser tests confirm malformed snapshots do not remove the surrounding page or execute HTML.

## TYPE SAFETY RESULT

Four small contract modules cover orders, payments/refunds, inventory and production. Their JavaScript API seams opt into checked JSDoc, including response shapes, refund idempotency requests and concurrency inputs. Typecheck passes. This is incremental compile-time checking, not a whole-project TypeScript conversion or runtime schema validator.

## TEST RESULTS

Validation used the existing Node 22.14.0 runtime for the final checks.

| Check | Baseline | Final result |
| --- | --- | --- |
| npm ci | Existing installed baseline | PASS; lockfiles unchanged, 0 audited vulnerabilities |
| npm run lint | PASS | PASS, including new import-ownership restrictions |
| npm run typecheck | PASS | PASS with checked high-risk seams |
| npm test -- --run | 78 tests / 10 files passed | 96 tests / 17 files passed |
| npm run build | PASS | PASS |
| Playwright targeted baseline | 34 passed | Full suite: 64 passed |
| Production-build browser checks | Prior repository coverage retained | 3 passed |
| git diff --check | PASS | PASS |
| Source secret scan | Clean starting tree | PASS; no candidates |
| Route/permission/CSS/ownership audits | Risks identified | PASS |

The seven added browser tests cover movement pagination, product stale responses, mutation locking/state-only interface, all admin navigation targets, malformed order/production snapshots, real catalog writes and extracted editor accessibility. Seven new colocated test files add 18 test cases to the existing unit/component suite; refund-notification tests moved with their feature. No backend code or fixture implementation changed. Browser tests exercise the actual local API integration with the disposable acceptance database and fake payment gateway.

## REGRESSIONS

None found in the final automated checks. No payment/refund, order-transition, proof/production, inventory, permission, shipping, promotion, review, report or storefront regression remains identified.

## GIT STATUS

Started clean on `develop` at `01156fd`. Only the scoped frontend architecture, existing shared UI/auth-routing seams, lint rules, tests and documentation were included. Backend, schema, dependencies, lockfiles, private configuration and business rules are unchanged. No history was rewritten.

## COMMITS CREATED

This report is included in the reviewed local implementation commit, `refactor(admin): establish feature-oriented frontend architecture`. The final chat reports its resulting hash; `git log -1 --oneline` identifies it locally. The working tree is checked after committing.

## REMAINING MAINTAINABILITY ISSUES

- Contracts deliberately cover the high-risk seams first. Other feature responses remain JavaScript; future DTO changes need corresponding contract updates, and runtime schema validation/generated clients remain optional future work.
- The shared scoped stylesheet retains much of the existing visual vocabulary. Further CSS migration can be done feature by feature without changing design.
- Builder draft editing and shipping configuration retain their cohesive feature-specific state/feedback lifecycles. A generic state library was not needed.
- `utils/permissions.js` and storefront dialog exports remain public facades for existing storefront consumers; all actual definitions have one owner.

These are documented incremental choices rather than remaining acceptance failures.

## FINAL ARCHITECTURE VERDICT

**COMPLETE.** Pages compose; meaningful feature modules own workflow state and rendering; named APIs own every admin endpoint; shared UI is domain-neutral; transport/auth remain shared. The confirmed pagination, stale-product and snapshot defects are fixed. Financial safeguards, URLs and permissions are preserved and validated.
