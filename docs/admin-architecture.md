# Admin frontend architecture

The admin application lives in `client/src/admin`. Existing `/admin/...` URLs are preserved.

**Pages compose; features own behavior; APIs own endpoints; shared components remain domain-neutral.**

## Ownership

| Location | Responsibility |
| --- | --- |
| `admin/app` | Admin route registration, route guard, landing behavior, navigation and permission metadata. |
| `admin/layout` | Sidebar, header, user display, responsive navigation, logout and the content outlet. No feature requests. |
| `admin/shared/components` | Tables, pagination, resource and mutation feedback, page/card/filter presentation, status badges and safe historical snapshots. |
| `admin/shared/hooks` | Generic mutation locking/state and independent list/filter parameters. No JSX or feature endpoints. |
| `admin/shared/utils` | File downloads, display dates and pagination calculations. |
| `admin/shared/styles` | Admin-scoped tokens and common styles. |
| `admin/features/<feature>` | Feature pages, cohesive sections/forms/tables, request coordination, named API operations and domain contracts. |
| `components/ui` | Dialog and confirmation primitives shared with the storefront. The original storefront exports remain available. |
| `services` | Existing authenticated HTTP transport, auth/session, error normalization, telemetry and storefront APIs. Admin endpoints belong to feature API modules. |

The 19 feature directories are dashboard, products, categories, collections, media, content, orders, customers, payments, shipping, production, builder-configuration, inventory, promotions, reviews, reports, audit, staff and settings.

## Page and feature interfaces

Pages read route parameters, call hooks, check action permissions and compose feature sections. Small page-level filters or confirmation composition may remain in a page. Long forms, tables and workflow implementations belong to the feature.

For example, `OrderDetailPage` loads its authoritative order through `useOrderDetail` and composes `OrderSummary`, `OrderItems`, `OrderPayments`, `OrderShipments`, `OrderTransitionForm` and `OrderTimeline`. Payment and shipment sections intentionally cross feature directories because they represent order subdomains. They consume the parent's order snapshot or payment summaries rather than fetching the same order again.

`ProductionJobPage` composes job/specification display, private assets, proof revisions/upload, production transitions, timeline and archive confirmation. `useProductionJob` owns requests and mutation coordination. Server-returned transitions and row versions remain authoritative.

Extract a section when it owns meaningful domain presentation, behavior or a useful test surface. Keep small one-off labels and wrappers inline. Do not add empty files to match a diagram or add a second request-lifecycle abstraction.

## Requests and mutations

All feature API modules import `services/api.js`; they do not configure another Axios client or change authentication behavior. Use named operations:

```js
ordersApi.getOrder(id, signal);
ordersApi.updateStatus(id, { status, reason });
inventoryApi.getMovements({ productId, page }, signal);
inventoryApi.adjustStock(productId, request);
productionApi.updateProductionStatus(id, { status, note, rowVersion });
```

Components and hooks must not construct backend endpoint strings. ESLint prevents importing the raw HTTP transport outside feature API modules, and prevents admin shared modules from importing features or pages.

`useResource` remains the single shared request lifecycle. Its cancellation, generation guard, key isolation and optional previous-data retention are reused rather than reimplemented. Product filters and product tools use keyed resources so earlier requests cannot replace a newer selection, even when a loader ignores cancellation.

`useAdminMutation` returns `pending`, `error`, `success`, `run` and `reset`. Its synchronous ref lock prevents duplicate submissions before React commits the pending state. Render `MutationFeedback` separately. Existing `customerError` messages remain the safe generic UI error contract; the transport and error-normalization modules are unchanged.

```jsx
const mutation = useAdminMutation(resource.reload);
await mutation.run(() => inventoryApi.adjustStock(productId, request));
// In the view:
<MutationFeedback {...mutation} />;
```

Financial operations use `usePaymentOperations`, which retains the stricter original workflow: confirmation, per-operation keys, refund-request identity and idempotency keys, pending-result reuse, terminal-result reset, provider availability, reconciliation and authoritative refreshes. HTTP success alone never establishes a completed refund. A refund reconciliation returning 204 triggers a read of the actual refund status. Failed refreshes preserve the last trusted payment snapshot and disable provider mutations until recovery. Each payment attempt has its own state and historical attempts remain visible.

## Pagination and resource state

`Pagination` is presentation only. It consumes server page size, count and total-page metadata when available, with explicit fallback sizes for older endpoint shapes. It never fetches data.

Inventory balance and movement history have independent state and request keys. Selecting or closing a product adjustment resets movement pagination. The existing movement endpoint returns `items` and `totalCount`, and has a fixed 50-row window; `movementPageSize` records that contract in the inventory feature. No backend change was needed. Balance counts describe products, so the UI uses the server count rather than the number of variant rows rendered.

`ResourceState` distinguishes initial loading, no-data failures, refreshes with data and refresh failures with data. Tables render an accessible empty state. Financial detail deliberately keeps its specific recovery messages and action-disabling behavior.

## Routes and permissions

`App.jsx` mounts the lazy `AdminRoutes` subtree at `/admin/*`. `AdminRoutes` maps the page registry to `adminNavigation` metadata. `adminNavigation.js` is the source of paths, titles, icons, permissions and navigation visibility, including detail routes. The layout filters that same metadata with `can`.

`AdminRouteGuard` composes application authentication with admin permission checks. `ProtectedRoute` owns session restoration and login redirection. `adminPermissions.js` owns role membership, the derived route-permission lookup and the existing landing preference. `utils/permissions.js` remains a small public facade for storefront consumers; it contains no duplicate definitions. Backend authorization remains authoritative.

To add a route, add its metadata and lazy page entry, then verify direct entry, its permitted roles and navigation visibility. Detail routes set `navigation: false`. Do not copy a second permission map into the shell or `App.jsx`.

## Styling and accessibility

`adminTokens.css` scopes variables to `.admin-app`. `adminBase.css` scopes every shared selector to that root, including tables, forms, buttons, scrollbars and motion overrides. Its animation names are admin-specific. Header/user display rules use `AdminLayout.module.css`; dashboard rules use `Dashboard.module.css`. Retain the current visual design when extracting a feature.

The action colors retain their existing hues with darker success/danger/warning tones to meet text contrast requirements. Extracted catalog, content and builder editors have explicit control names and table headings. Dialog focus trapping, Escape behavior and focus restoration remain shared. Mobile navigation retains inert/hidden behavior, its named toggle and Escape focus restoration. Status and error messages remain textual and accessible.

Use `SnapshotViewer` for historical JSON. It catches parsing failures, escapes any raw fallback text and keeps other page sections available. Never parse a historical order or production snapshot unguarded in rendering code.

## Incremental contracts

Orders, payments/refunds, inventory and production each have a small TypeScript contract file matching the response fields used by their admin workflows. Their JavaScript API modules opt into `// @ts-check` and reference those request/response types through JSDoc. `npm run typecheck` checks these seams without converting the frontend to TypeScript. These are compile-time contracts, not runtime schema validation or a generated backend client; keep them aligned when a DTO changes.

## Tests and verification

Focused presentation tests live beside their modules. They cover snapshot fallback/escaping, resource retention, pagination metadata/bounds, refund controls, order totals/timeline, inventory controls/ledger and server-approved production transitions. Existing refund-notification tests moved with payment ownership.

`client/e2e/admin` holds preserved operations/payment regressions and new architecture/catalog checks. The existing shared acceptance host and disposable database are used for financial, production, permissions, shipping and CRUD flows. Controlled list windows, deliberately delayed hook loaders and malformed snapshot fields test the frontend gaps explicitly. No real payment provider or business database is used.

```powershell
cd client
npm ci
npm run lint
npm run typecheck
npm test -- --run
npm run build
npm run test:e2e
```

Run focused tests after a migration group, then the full frontend/browser suite. Production-build browser checks remain available through `npm run test:e2e:production`. From the repository root, run `git diff --check` and `python scripts/scan-secrets.py`.
