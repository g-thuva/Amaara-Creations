# Frontend architecture

The complete frontend lives under `client/src/app`, `client/src/features` and `client/src/shared`. Features own behavior and endpoint operations; pages compose feature sections; shared modules contain reusable infrastructure and presentation. Existing customer and admin URLs remain unchanged.

## Ownership

| Location | Responsibility |
| --- | --- |
| `app/App.jsx` | Storefront/admin shell, metadata, error recovery and suspense. |
| `app/routes` | Public, protected account/commerce and admin route registration. |
| `app/providers` | Router/session/provider composition and enabling storefront caches outside admin. |
| `app/guards` | Session restoration, login redirection and staff permission UX. |
| `app/config` | Storefront/account navigation and the single role/permission definition. |
| `app/layout`, `app/seo` | Header/footer composition and route metadata/indexing. |
| `features/<feature>` | Pages, state/action hooks, forms, sections, named endpoint operations, contracts and tests. |
| `features/admin` | Navigation metadata, layout, shared admin presentation and 19 operational workflows. |
| `shared/api` | One authenticated HTTP client, request/media configuration, memory-only tokens, refresh coordination, safe errors and sanitized telemetry. |
| `shared/components` | Domain-neutral UI, dialogs, feedback, notifications and structured-data rendering. |
| `shared/hooks` | Common request and mutation lifecycles. |
| `shared/utils`, `shared/styles` | Formatting, safe parsing/links, downloads, tokens, reset and shared UI/accessibility rules. |

There are no root `pages`, `components`, `contexts`, `hooks`, `services`, `types` or `utils` directories and no compatibility re-export layer. Import the owner directly through `@/`; Vite/Vitest and TypeScript resolve the same alias. Do not add barrels just to shorten imports.

## Feature map

| Feature | Owns |
| --- | --- |
| `auth` | Session provider; login, registration, recovery and verification pages/forms/hooks; authenticated mutation feedback. |
| `home` | Hero/carousel configuration and merchandising sections. |
| `catalog` | Products/categories/collections, URL filters, results, cards, gallery/purchase presentation and product metadata. |
| `cart` | Shared cart cache, actions, items and server subtotal display. |
| `wishlist` | Shared wishlist cache, toggling, items and moving products into the cart. |
| `checkout` | Address/delivery/coupon selection, quote, atomic order/session request, review and server totals. |
| `payments` | Initiation, hosted redirects, return polling, same-order retry and shared payment DTOs. |
| `custom-design` | Controls, preview, drafts, debounced quotes, persistence/artwork, saved designs and customer proofs. |
| `account` | Profile/avatar, address book, security and account layout. |
| `orders` | Customer history/detail and the order DTO shared with admin. |
| `reviews` | Published/own reviews, moderation-state presentation, editing and deletion. |
| `content` | Public settings, CMS pages and structured block parsing/rendering. |
| `admin` | Dashboard, catalog/content editors, customers, orders, payments, shipping, production, inventory, promotions, moderation, reports, audit, staff and settings. |

Small cohesive features do not need every subdirectory. Extract a module for a meaningful concern or test surface; keep simple labels and one-off wrappers inline.

## Requests and state

Only feature API modules import `shared/api/httpClient.js`. Components call named operations; they never build endpoint strings or create another HTTP client. Shared transport owns credentials, in-memory tokens and single-flight refresh/retry. Features own payloads and response meaning.

```js
import { catalogApi } from '@/features/catalog/catalogApi.js';
import { useResource } from '@/shared/hooks/useResource.js';

const product = useResource(signal => catalogApi.getProductById(id, signal), id);
```

`useResource` is the single general read lifecycle. It aborts superseded requests, rejects stale generations, isolates keys, exposes loading/error/reload state and optionally retains a trusted snapshot during refresh. A different key never displays the previous resource's data.

`useMutation` provides synchronous duplicate locking and state/actions without JSX. `useAuthenticatedMutation` adds login/toast behavior; `useAdminMutation` adds admin success/error presentation. Financial and production workflows retain stricter feature-specific orchestration.

Mutation state stays local to a page/form. Cart and wishlist caches are global because the header, catalog, detail and commerce journeys share their counts and refreshes. Settings, authentication and notifications also have cross-page consumers. `StorefrontProviders` disables customer-resource reads under `/admin` and contains no endpoints. There is no catch-all store context or new state library.

## Checkout and payments

`useCheckout` owns selection and quote identity; `CheckoutForm` and `CheckoutReview` render server values. `useCheckoutPayment` owns the attempt, duplicate lock and idempotency key. `usePaymentReturn` owns bounded verification polling and retry. Hooks return state/actions, never JSX.

The existing `/checkout/session` atomically creates the order and payment session on the server. Its endpoint remains in `checkoutApi`; payment orchestration deliberately calls that named operation. Retry uses `paymentsApi.retry` for the existing order.

- Prices, currency, tax, shipping, discounts and fees come from the server. Formatters never convert currencies or recompute totals.
- CHF/LKR remain explicit. Cart compatibility, quote `canPay`, provider availability and backend validation remain authoritative.
- Attempt identity includes selection, quote/version and notes. Ambiguous failures retain the idempotency key; changed checkout responses invalidate the attempt and reload the quote.
- Only approved provider-controlled HTTPS URLs can redirect the browser.
- Query strings never prove payment. Server-verified states alone show confirmation; pending/review/error states stay distinct.
- Admin refunds retain confirmation, idempotency, reconciliation and trusted-snapshot recovery.

`formatCurrency` preserves storefront LKR presentation. `formatIsoCurrency` preserves explicitly ISO-labelled checkout/operational presentation. Both display values without converting money.

## Custom designs

`CustomBuilderPage` composes `BuilderControls` and `BuilderPreview`. `useCustomBuilder` coordinates authoritative loading, saved identity, persistence, artwork and cart insertion. `artworkActions` owns mutation generations and reconciliation. Saving a new design preserves the artwork input as its URL becomes `/custom/:id`.

`useBuilderQuote` debounces changes and reuses `useResource`. Changed forms immediately hide old quotes and disable quote-dependent actions. Draft storage tolerates malformed data and denied access. Preview object URLs are revoked; saves and cart insertion have synchronous duplicate locks.

Proofs and saved designs have focused hooks and sections. Private downloads use authenticated operations and the shared blob helper. Customer historical snapshots use safe version-aware parsing; admin snapshots catch malformed JSON and escape raw fallback text.

## Routing and authentication

`AppRoutes` composes `PublicRoutes` and `AccountRoutes` and lazy-loads `AdminRoutes`. Metadata in `features/admin/app/adminNavigation.js` drives the same 22 admin patterns and navigation. `app/config/permissions.js` is the single permission/landing definition. Backend authorization remains authoritative.

Keep the four account aliases and emailed hash-link conversion. Browser routing retains its configured base; hash mode remains available for static preview. Guards wait for restoration and retain intended destinations. No guest-only restriction was introduced.

Logout revokes the cookie before caller navigation and local session clearing. Its optional completion callback and session update share the router's transition priority, preventing a guard from overriding the intended destination. Navigation choices remain with callers.

Route/product metadata retain canonical host validation, actual currency, safe JSON-LD and private/noindex rules.

## Styling and assets

`shared/styles/tokens.css` contains root/storefront tokens; `globals.css` contains reset, common UI, application sizing and accessibility. Layout/feature styles live beside their owners. Customer feature selectors use `:where(.storefront)` so scoping adds no specificity. Admin CSS stays under `.admin-app`, with CSS modules for shell/dashboard. Auth motifs are scoped too.

The `.s-*` classes form an intentional UI vocabulary for buttons, media, summaries, dialogs and feedback. Preserve branding/responsive behavior; avoid unscoped generic `.card`, `.btn` and `.form-control` rules.

`public/favicon.svg` is the sole repository image asset and is referenced by `index.html`. Fonts remain external resources. Product, CMS, avatar and private-artwork URLs are runtime data. No empty asset folders were created.

## Contracts and verification

JavaScript remains the implementation language. Checkout, catalog, CMS and customer/admin orders/payments use JSDoc `// @ts-check` with TypeScript contracts. Inventory/production keep checked concurrency fields. Common order/payment DTOs have one owner; admin filters/mutation requests stay local. These provide compile-time checking, not runtime schema validation.

Tests live beside behavior. Browser journeys are grouped under `e2e/storefront`, `checkout`, `account`, `admin` and `production`, with shared disposable fixtures and accessibility assertions at the E2E root. Controlled HTTP fixtures cover selected UI states; real journeys use the acceptance host. Neither proves an external merchant transaction.

Use Node 22+ and the committed lockfile:

```powershell
cd client
npm ci
npm run lint
npm run typecheck
npm test -- --run
npm run build
npm run test:e2e
npm run test:e2e:production
npm run check:performance
```

From the root, run `git diff --check` and `python scripts/scan-secrets.py`. Builds, coverage, browser artifacts, screenshots, logs and private configuration remain ignored. Existing production resolver/build compatibility settings and package versions are preserved.

## Adding a feature

1. Choose its owner and put named endpoints and checked contracts there.
2. Reuse the shared lifecycle hooks; add a feature hook for meaningful orchestration.
3. Keep pages focused on parameters, navigation and composition; extract cohesive forms/tables/workflows.
4. Register URLs in `app/routes` and update navigation/permissions where needed. Preserve bookmarks.
5. Scope styles, colocate tests and verify direct entry, roles, loading/error/empty states and the relevant journey.

See [admin architecture](admin.md) and [overview](overview.md).
