# Phase 4 — Storefront UI/UX

## 1. Objective

Phase 4 replaces the original prototype customer interface with a responsive storefront that uses the Phase 0–3 APIs as its source of truth. It covers the global customer layout, home, catalogue, product detail, cart, wishlist, authentication presentation, account pages, CMS pages, common states, accessibility basics, and browser regression tests. It does not implement custom-design persistence, server custom pricing, payment processing, shipping calculation, or production fulfilment.

## 2. Initial frontend state

The initial client used `HashRouter`, direct Axios services, CSS variables, and separate page CSS. Customer pages were functional but visually inconsistent, used glass/gradient prototype styling, contained native alerts in profile/checkout/custom flows, had hard-coded placeholder image URLs, and did not consume public categories, collections, CMS pages, or site settings. Product responses did not expose Phase 3 media or variants. Catalogue operations supported only category text, search, and pagination. Three existing hook warnings were present in admin Orders, Customers, and Reviews.

## 3. Design direction

The storefront uses a restrained editorial direction for a creative sticker/print business: deep ink, warm amber, warm off-white, flat surfaces, crisp borders, serif display copy, and compact sans-serif interface text. Layout and type provide the visual identity rather than decorative stock imagery, gradients, or excessive cards.

## 4. Design tokens

| Token/category | Implementation | Purpose |
|---|---|---|
| Brand primary | `--s-ink: #182c3c` | Navigation, primary actions, headings, footer |
| Accent | `--s-accent: #f1bc60`; `--s-accent-ink: #6b4713` | Warm brand detail and accessible accent text |
| Background/surface | `--s-bg: #faf9f6`; `--s-surface: #fff`; `--s-muted: #eeede7` | Page, controls, and quiet sections |
| Text | `--s-text`, `--s-secondary`, `--s-text-muted` | Primary, supporting, and metadata text |
| Feedback | `--s-success`, `--s-warning`, `--s-danger`, `--s-info` | Availability and customer feedback |
| Focus | `--s-focus: #246e9e` | Visible keyboard focus ring |
| Spacing | `--s-space-1` through `--s-space-24` | Consistent 4–96 px scale |
| Radius | `--s-radius: 6px`; `--s-radius-lg: 12px` | Restrained controls/dialogs |
| Container | `--s-container: 1240px` | Wide-screen content bound |
| Typography | `--s-font`; `--s-display`; responsive heading tokens | Interface and editorial hierarchy |
| Motion/layers | `--s-transition`; header/toast layers | Short interaction transitions and stable stacking |

Tokens are scoped below `.storefront`, keeping the admin visual system separate.

## 5. Component architecture

- `StoreProvider` owns public settings, cart/wishlist counts, mutation feedback, login redirects, and narrow cache refreshes.
- `useResource` follows the existing Axios/local-state approach, adds cancellation, and rejects stale responses.
- Shared storefront UI includes buttons, icons, media fallback, price, quantity, skeleton, empty/error states, pagination, native-dialog drawers, confirmation dialogs, headings, and toasts.
- Product, CMS, account-layout, review, header, and footer components provide domain-level reuse without fragmenting every element into a component.
- An error boundary gives customer routes a recoverable render fallback.

## 6. Global layout

The customer layout now contains an optional settings-driven announcement, responsive brand header, active desktop navigation, keyboard-accessible mobile drawer, account/wishlist/cart shortcuts with API counts, main landmark/skip link, toast region, and settings-driven footer. Admin routes keep their existing layout and styles.

## 7. Homepage

| Section | Source |
|---|---|
| Announcement | Public setting `announcement.text` |
| Hero copy/media/CTA | Published `home` CMS `hero` JSON block, with an honest brand fallback |
| Categories | `GET /api/v1/catalog/categories` |
| Custom sticker CTA | Static Phase 4 brand layout linking to the preview |
| Featured products | Paginated product API with `sort=featured` |
| Collections | `GET /api/v1/catalog/collections` |
| How it works | Static explanatory layout that makes no payment/proof claims |
| Optional CMS blocks/FAQ | Published `home` page sections |

Each optional request fails independently. Empty catalogue data produces composed empty states rather than fabricated products, testimonials, metrics, or contact information.

## 8. Catalogue

The catalogue uses server-side pagination, search, category/collection IDs, price range, availability, and safe sort keys. State is stored in URL query parameters. Major filter and sort changes reset the page; pagination preserves filters. Desktop filters and a native-dialog mobile drawer share the same controls. Loading skeletons, result count, active chips, filter errors, API errors, and no-results states are included.

## 9. Product detail

Direct product URLs load from the API. The page includes breadcrumbs, a responsive media gallery, real description/category/price/stock, quantity controls, cart/wishlist mutations, real review presentation and owner create/edit/delete actions. Active variants update displayed price and availability. The current cart model has no `ProductVariantId`, so selected variants are clearly presented as contact-only and cannot silently add a base product to cart.

## 10. Cart

The cart remains protected and server-authoritative. It renders product media, unit prices, responsive rows, stock problems, accessible quantity controls, remove/clear confirmation dialogs, item counts, product subtotal, and cache/badge refresh. The summary explicitly avoids presenting shipping/payment as calculated totals.

## 11. Wishlist

The protected wishlist renders server items, media fallbacks, price and availability, remove, and add-to-cart. Counts refresh after each mutation. Anonymous wishlist actions preserve the intended destination through the existing login redirect state.

## 12. Authentication UI

Login, registration, forgot/reset password, verification/resend, and security screens share storefront tokens and accessible feedback. Phase 2 in-memory access-token storage, HttpOnly refresh cookie, single-flight refresh, generic login errors, lockout semantics, and protected-route restoration remain intact. React Strict Mode session restoration now reuses the interceptor’s single-flight refresh promise.

## 13. Account/profile

Protected customer routes share a responsive account navigation. Profile editing uses only supported DTO fields and preserves the current avatar URL when saving. Avatar upload validates type/size for UX and continues to rely on backend validation. Success/errors use inline/toast feedback rather than browser alerts.

## 14. Addresses

The address book supports list, add, edit, automatic/default presentation, explicit set-default, delete confirmation, validation attributes, request disabling, loading/error/empty states, and mobile cards through the Phase 2 Address API.

## 15. Orders

Order history and detail use the existing order DTO: order number/date/status, item count, immutable item snapshot/media, prices, total, and available shipping address. Raw internal lifecycle concepts are not introduced.

## 16. CMS pages

`/about`, `/contact`, and `/pages/:slug` load published content. Text is rendered through React escaping. Structured JSON supports a small allowlist (`text`, `hero`, `cta`, `image`, `faq`); invalid JSON and unknown block types are omitted. CMS links reject unsafe protocols. No arbitrary database HTML is rendered.

## 17. Site settings integration

Public settings can supply site name, announcement, footer summary, contact email/phone/address, and social URLs. Missing values are hidden. External links are protocol-validated and opened with appropriate protections.

## 18. Responsive behaviour

The layout uses CSS Grid, bounded containers, mobile-first navigation/filter drawers, resilient text wrapping, consistent media ratios, responsive commerce rows, horizontally wrapping account navigation, and a one-column fallback at 320 px where needed. Reduced-motion preferences disable animations/transitions.

## 19. Accessibility work

Implemented semantic landmarks/headings, skip link, active navigation, visible focus, native dialog semantics, Escape close, explicit focus wrapping/restoration, form labels, error/status regions, descriptive/fallback image labels, disabled states, accessible quantity controls, review rating labels, and keyboard-operable filters/pagination. This is a practical baseline, not a formal WCAG certification.

## 20. API integration changes

The public product DTO now exposes customer-safe media and active variants. Product listing adds validated category/collection ID, price, availability, featured, and sort parameters while retaining existing query compatibility. Public categories include their selected media URL and collections count only active products. No schema change or migration was added for Phase 4.

| Frontend feature | API route | Query/mutation | Auth |
|---|---|---|---|
| Products | `GET /api/v1/products` | search/filter/sort/page | Public |
| Product | `GET /api/v1/products/{id}` | detail/media/variants | Public |
| Categories/collections | `GET /api/v1/catalog/*` | read | Public |
| CMS/settings | `GET /api/v1/content/*` | read | Public |
| Reviews | `/api/v1/products/{id}/reviews`, `/api/v1/reviews/{id}` | read/create/update/delete | Read public; writes protected |
| Cart | `/api/v1/cart` | read/add/update/remove/clear | Protected |
| Wishlist | `/api/v1/wishlist` | read/add/remove/add-to-cart | Protected |
| Profile/avatar | `/api/v1/users/profile`, `/api/v1/upload/avatar` | read/update/upload | Protected |
| Addresses | `/api/v1/addresses` | CRUD/default | Protected |
| Orders | `/api/v1/orders` | list/detail/create | Protected |

## 21. Routing decision

`HashRouter` is retained because the current Vite build targets GitHub Pages (`base: /Amaara-Creations/`) without an established SPA rewrite. Customer compatibility redirects add `/account/*` aliases while existing `/profile`, `/addresses`, and `/orders` links continue to work.

## 22. Route table

| Route | Purpose | Access | Data source | Status |
|---|---|---|---|---|
| `/` | Home | Public | CMS/settings/catalogue | Implemented |
| `/products` | Catalogue | Public | Products/categories/collections | Implemented |
| `/products/:id` | Product detail | Public | Product/reviews | Implemented |
| `/custom` | Custom text preview | Public | Client-only preview | Phase 4 aligned; Phase 5 deferred |
| `/cart` | Cart | Protected | Cart API | Implemented |
| `/wishlist` | Wishlist | Protected | Wishlist API | Implemented |
| `/checkout` | Existing order capture | Protected | Cart/order API | Presentation aligned; Phase 6 deferred |
| `/profile` | Profile | Protected | User API | Implemented |
| `/addresses` | Address book | Protected | Address API | Implemented |
| `/orders`, `/orders/:id` | Order history/detail | Protected | Order API | Implemented |
| `/account/security` | Password/sessions | Protected | Auth API | Implemented |
| `/account/*` | Compatibility aliases | Protected | Redirects | Implemented |
| `/login`, `/register` | Authentication | Public | Auth API | Implemented |
| `/forgot-password`, `/reset-password`, `/verify-email` | Recovery/confirmation | Public | Auth API | Implemented |
| `/about`, `/contact`, `/pages/:slug` | CMS content | Public | Content/settings API | Implemented |
| `/admin/*` | Operations | Admin protected | Admin APIs | Preserved |
| `*` | Not found | Public | None | Implemented |

## 23. Media handling

`resolveMediaUrl` remains the single media resolver and now rejects script/data/protocol-relative/backslash URLs. Product media use backend URLs, fixed aspect ratios, lazy loading below the fold, `object-fit: contain`, descriptive alt text, and a neutral inline fallback. No external hotlinks or placeholder services are used.

## 24. Loading, empty, and error patterns

Reusable skeletons keep page structure during reads. Empty catalogue/cart/wishlist/order/address/CMS states provide relevant next actions. Errors distinguish offline, authentication, authorization, validation, not-found, conflict, throttling, and general failures without exposing server exception bodies.

## 25. Tests

- Vitest: 27 tests across API error/session helpers, money/date/quantity/catalogue URL utilities, safe links/media, CMS escaping/allowlists, ProductCard, quantity semantics, and protected-route restoration.
- Playwright public suite: routes, deep links, 404/product-not-found states, mobile navigation/filter drawers, Escape/focus restoration, and overflow checks at 320, 375, 390, 430, 768, 1024, and 1440 px.
- Playwright protected suite: profile, addresses, orders, wishlist, cart, security, and admin dashboard; responsive checks at 375, 430, 768, 1024, and 1440 px.
- Real local API journeys: registration plus confirmation-token flow, unconfirmed login rejection, product media/variants, cart, wishlist, review create/edit/delete, catalogue pagination/sort/search/empty state, profile update, address add/edit/default/delete, checkout order creation, order detail/history.
- QA products/categories/collections were archived, QA CMS pages unpublished, uploaded QA media deleted, and all disposable QA customers plus dependent data removed after verification.

## 26. Build results

Baseline: backend build and 14 tests passed; frontend install, lint, typecheck, 4 tests, and build passed with three existing admin hook warnings.

Final:

| Command | Result |
|---|---|
| `dotnet restore` | PASS |
| `dotnet build --configuration Release --no-restore` | PASS, 0 warnings / 0 errors |
| `dotnet test ... --configuration Release --no-build` | PASS, 14 tests |
| `npm ci` | PASS after stopping a stale workspace Vite process that held `esbuild.exe`; 0 vulnerabilities |
| `npm run lint` | PASS, 0 warnings |
| `npm run typecheck` | PASS |
| `npm test` | PASS, 27 tests |
| `npm run build` | PASS |
| Playwright public suite | PASS, 8 tests |
| Playwright protected suite | PASS, 1 test |
| Focused real checkout journey | PASS, 1 test |

The installed Node 20.20.2 runtime emits the expected engine warning because the project requires Node 22 or newer; it did not change validation results.

## 27. Browser verification

Chrome rendered the desktop/mobile home design without page exceptions or horizontal overflow. Public and protected route suites passed at the documented widths. Mobile dialogs close with Escape, trap focus, and restore focus. Real DB mutations were verified in focused journeys. No authenticated cross-browser claim is made; expansion remains Phase 8 work.

## 28. Lint warnings

The three pre-existing `react-hooks/exhaustive-deps` warnings in admin Orders, Customers, and Reviews were fixed with stable callbacks and effect dependencies. No lint rule was disabled.

## 29. Known limitations

- The Phase 1 cart schema/API has no product variant field, so variants are viewable but selected variants cannot be added to cart.
- The database currently has no live storefront products/categories/collections/settings/published About/Home content after QA cleanup; the implemented production UI therefore shows truthful empty/fallback states until admins publish content.
- CMS supports structured text/JSON blocks rather than rich HTML.
- Contact shows configured channels and intentionally has no fake form submission.
- Browser automation uses installed Chrome; formal cross-browser/accessibility certification is deferred.

## 30. Phase 5 handoff

Consume builder configuration, calculate authoritative server quotes, persist saved designs, upload/store artwork and previews, add a custom cart-item contract, snapshot immutable order designs, implement proof approval, and provide an admin production view. The Phase 4 preview must remain explicitly non-ordering until those contracts exist.

## 31. Phase 6 handoff

Implement payment gateway/webhooks, shipping-zone quotes, coupons, inventory reservation/concurrency, refunds, and the final payment/production/fulfilment state model. Replace the current explicit product-subtotal/order-record presentation only when those server contracts are authoritative.

## 32. Files changed

Phase 4 adds the storefront component/context/hook/type/utility modules, CMS and not-found pages, storefront stylesheet, favicon, Playwright configuration/specs, and this implementation record. It updates customer pages, routing, metadata, API services, public product/catalogue DTOs/queries, relevant admin hook dependencies, package manifests, and ignore rules. No database migration was created.
