# Admin UI redesign inventory

The implementation lives in `client/src/features/admin/`, rather than `client/src/pages/admin/`. Route definitions consume `app/adminNavigation.js`; permission decisions remain in the existing `can()` helper and server-provided permissions. There are 21 sidebar destinations and three detail routes (orders, customers and production jobs).

The shared shell is `layout/AdminLayout`, `AdminSidebar`, `AdminHeader`, `AdminUserMenu` and `useAdminNavigation`. Existing `shared/components` provide table, pagination, resource state, page header, section card and mutation feedback primitives. `shared/styles/adminBase.css` contains overlapping legacy rules; `adminTokens.css` uses the old brown palette. The dashboard has a separate CSS module and the remaining inline styles. Icons already use the outline Feather set from `react-icons/fi`; no icon dependency is needed.

Existing native-dialog-based `Dialog` / `ConfirmDialog` handle focus trapping, Escape and restoration. Financial, inventory, production, review and staff operations already use these confirmations. The staff role and authenticator recovery review flows must remain intact. Table pagination and filter hooks, all API modules, auth/session operations, routes, server code and permission logic are preserved.

The seven priority pages are Dashboard, Payments, Promotions, Reports, Audit log, Staff and Privacy requests. Other destinations are Products, Categories, Collections, Media, Content, Settings, Custom Builder, Custom Designs, Orders, Customers, Reviews, Quote requests, Inventory and Shipping. Shared styling and table/state primitives will cover all destinations, with explicit two-column detail layouts.

Implementation order: tokens and primitives → grouped responsive shell → seven priority pages in the requested order → remaining forms/tables/detail pages → lint, builds, component tests, existing workflow regressions and keyboard/accessibility/layout checks at 1920, 1440, 1024, 768 and 390 pixels. Screenshots use synthetic acceptance fixtures, never business records. Existing payment simulator edits remain intact.

LKR values will use `LKR 1,234.50`. Historical CHF values retain their actual currency label; formatting never converts money. Report date filters continue to submit UTC boundaries; display dates use dd/mm/yyyy in Asia/Colombo. A global search and invented trend/attention counters will be omitted because the current APIs do not provide them.
