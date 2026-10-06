# Storefront — Storefront UI/UX

## 1. Objective

Storefront replaces the original prototype customer interface with a responsive storefront that uses the Phase 0–3 APIs as its source of truth. It covers the global customer layout, home, catalogue, product detail, cart, wishlist, authentication presentation, account pages, CMS pages, common states, accessibility basics, and browser regression tests. It does not implement custom-design persistence, server custom pricing, payment processing, shipping calculation, or production fulfilment.

## 2. Initial frontend state

The initial client used `HashRouter`, direct Axios services, CSS variables, and separate page CSS. Customer pages were functional but visually inconsistent, used glass/gradient prototype styling, contained native alerts in profile/checkout/custom flows, had hard-coded placeholder image URLs, and did not consume public categories, collections, CMS pages, or site settings. Product responses did not expose Catalogue Media media or variants. Catalogue operations supported only category text, search, and pagination. Three existing hook warnings were present in admin Orders, Customers, and Reviews.

## 3. Design direction

Revision 2 restores the original Amaara identity: navy and sky blue, Poppins headings, Inter body copy, pill actions, image-led merchandising, friendly rounded cards, subtle navy-tinted elevation, sticker/die-cut cues, and a navy footer. The implementation removes the interim cream, amber, serif editorial direction while preserving every Storefront service, route, session, commerce, CMS, and error-state contract.

## 4. Design tokens

| Token/category | Implementation | Purpose |
|---|---|---|
| Brand primary | `--brand-navy-900: #0f2d4a`; `--brand-navy-700: #1a365d`; `--brand-navy-600: #2c5282` | Footer, hero, headings, primary actions |
| Accent | `--brand-sky-500: #4299e1`; `--brand-sky-700: #2b6cb0`; `--brand-sky-200: #90cdf4` | Decorative bars, accessible links/focus, accents on navy |
| Background/surface | `--brand-tint: #e5edf6`; `--bg-page: #f6f9fc`; `--surface`; `--surface-muted` | Page, alternating bands, controls, cards |
| Text | `--text-primary`, `--text-secondary`, `--text-muted`, `--text-on-dark` | Primary, supporting, metadata, and dark-surface text |
| Feedback | `--success`, `--warning`, `--danger`, `--info` | Availability and customer feedback with text/icons |
| Shape | `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-pill` | Inputs, cards, sections, pill actions |
| Elevation | `--shadow-1`, `--shadow-2`, `--shadow-3` | Resting surfaces, headers, hover/dialog elevation |
| Container | `--container: 1200px` | Original Amaara content width with fluid page gutters |
| Typography | `--font-body`; `--font-heading`; fluid type tokens | Inter UI/body and Poppins heading hierarchy |
| Motion/layers | `--ease`; `--dur-fast`; `--dur`; named z-index tokens | Short interaction transitions and stable stacking |

Tokens are scoped below `.storefront`, keeping the admin visual system separate.

## 5. Component architecture

- `StoreProvider` owns public settings, cart/wishlist counts, mutation feedback, login redirects, and narrow cache refreshes.
- `useResource` follows the existing Axios/local-state approach, adds cancellation, and rejects stale responses.
- Shared storefront UI includes pill buttons, consolidated Feather icons through `react-icons/fi`, media fallbacks, price, quantity, skeleton, empty/error states, pagination, native-dialog drawers, confirmation dialogs, headings, and toasts.
- Home modules split the carousel, category cards, collection cards, custom CTA, and process steps into readable components.
- Product, CMS, account-layout, review, header, and footer components provide domain-level reuse without fragmenting every element into a component.
- An error boundary gives customer routes a recoverable render fallback.

## 6. Global layout

The customer layout contains a dismissible settings-driven announcement, sticky legible glass header with IntersectionObserver scroll state, inline search, active desktop navigation, account menu, keyboard-accessible mobile drawer, account/wishlist/cart shortcuts with API counts, main landmark/skip link, toast region, and a navy settings-driven footer. Admin routes keep their operational layout and styles.

## 7. Homepage

| Section | Source |
|---|---|
| Announcement | Public setting `announcement.text` |
| Hero carousel | Published `home` CMS `hero` JSON block, with an honest navy sticker-sheet fallback |
| Categories | `GET /api/v1/catalog/categories` |
| Custom sticker CTA | Truthful preview-only brand band linking to `/custom` |
| Featured products | Paginated product API with `sort=featured` |
| Collections | `GET /api/v1/catalog/collections`, with media-ready tiles and branded no-media surfaces |
| How it works | Four icon-led steps that make no payment/proof claims |
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

Login, registration, forgot/reset password, verification/resend, and security screens share storefront tokens and accessible feedback. Authentication in-memory access-token storage, HttpOnly refresh cookie, single-flight refresh, generic login errors, lockout semantics, and protected-route restoration remain intact. React Strict Mode session restoration now reuses the interceptor’s single-flight refresh promise.

## 13. Account/profile

Protected customer routes share a responsive account navigation. Profile editing uses only supported DTO fields and preserves the current avatar URL when saving. Avatar upload validates type/size for UX and continues to rely on backend validation. Success/errors use inline/toast feedback rather than browser alerts.

## 14. Addresses

The address book supports list, add, edit, automatic/default presentation, explicit set-default, delete confirmation, validation attributes, request disabling, loading/error/empty states, and mobile cards through the Authentication Address API.

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

The public product DTO now exposes customer-safe media and active variants. Product listing adds validated category/collection ID, price, availability, featured, and sort parameters while retaining existing query compatibility. Public categories include their selected media URL and collections count only active products. No schema change or migration was added for Storefront.

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
| `/custom` | Custom text preview | Public | Client-only preview | Storefront aligned; Custom Builder deferred |
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

- Vitest: 31 tests across API error/session helpers, money/date/quantity/catalogue URL utilities, safe links/media, CMS escaping/allowlists, hero JSON normalisation, carousel controls, category cardinalities, conditional footer content, ProductCard, quantity semantics, and protected-route restoration.
- Playwright public suite: 10 tests covering routes, deep links, 404/product-not-found states, mobile navigation/filter drawers, carousel controls and reduced motion, Escape/focus restoration, and overflow checks at 320, 375, 390, 430, 768, 1024, 1280, and 1440 px.
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
| `npm ci` | PASS after stopping a stale workspace Vite process that held `esbuild.exe`; 0 vulnerabilities after applying the compatible `source-map-js` lockfile patch |
| `npm run lint` | PASS, 0 warnings |
| `npm run typecheck` | PASS |
| `npm test` | PASS, 31 tests |
| `npm run build` | PASS |
| Playwright public suite | PASS, 10 tests for Revision 2 |
| Playwright protected suite | SKIPPED in Revision 2 because disposable credentials were not retained; Storefront baseline passed |
| Playwright commerce suite | SKIPPED in Revision 2 because the disposable fixture and customer credentials were not retained; Storefront baseline passed |
| Playwright live API suite | SKIPPED in Revision 2 because `STOREFRONT_FIXTURE` was not supplied; Storefront baseline passed |

The installed Node 20.20.2 runtime emits the expected engine warning because the project requires Node 22 or newer; it did not change validation results.

## 27. Browser verification

Chrome rendered the Revision 2 desktop/mobile home, catalogue, and split authentication design without horizontal overflow. The public route/carousel suite passed at all eight documented widths. Mobile dialogs close with Escape, trap focus, and restore focus. The protected, commerce, and live API Revision 2 suites were invoked and skipped because the cleaned disposable credentials and fixture were absent. Their Storefront baseline journeys previously passed; no new authenticated or cross-browser claim is made.

## 28. Lint warnings

The three pre-existing `react-hooks/exhaustive-deps` warnings in admin Orders, Customers, and Reviews were fixed with stable callbacks and effect dependencies. No lint rule was disabled.

## 29. Known limitations

- The Foundation cart schema/API has no product variant field, so variants are viewable but selected variants cannot be added to cart.
- The database currently has no live storefront products/categories/collections/settings/published About/Home content after QA cleanup; the implemented production UI therefore shows truthful empty/fallback states until admins publish content.
- CMS supports structured text/JSON blocks rather than rich HTML.
- Contact shows configured channels and intentionally has no fake form submission.
- Browser automation uses installed Chrome; formal cross-browser/accessibility certification is deferred.

## 30. Custom Builder handoff

Consume builder configuration, calculate authoritative server quotes, persist saved designs, upload/store artwork and previews, add a custom cart-item contract, snapshot immutable order designs, implement proof approval, and provide an admin production view. The Storefront preview must remain explicitly non-ordering until those contracts exist.

## 31. Phase 6 handoff

Implement payment gateway/webhooks, shipping-zone quotes, coupons, inventory reservation/concurrency, refunds, and the final payment/production/fulfilment state model. Replace the current explicit product-subtotal/order-record presentation only when those server contracts are authoritative.

## 32. Files changed

Storefront adds the storefront component/context/hook/type/utility modules, CMS and not-found pages, storefront stylesheet, favicon, Playwright configuration/specs, and this implementation record. Revision 2 adds dedicated home modules, an authentication shell, a shared icon adapter, the pending brand audit, and expanded tests. It removes superseded customer CSS and the original hot-linked slider implementation. No database migration was created.

## 33. Revision 2 — Brand-continuity pass

### Evidence and continuity decisions

No owner Instagram, logo, or product reference files were supplied. The audit is honestly recorded as pending in `brand-audit.md`; Instagram was not scraped or embedded. The original repository UI is the evidence base. Revision 2 restores its navy/sky palette, Inter/Poppins type, pill CTAs, image-led carousel, image category cards, hover-lift product cards, accent title bars, and navy footer while fixing its contrast, external-media, carousel, mobile-navigation, and fake-content defects.

| Original element | Revision 2 decision | Implementation |
|---|---|---|
| Navy + sky palette | Kept; interactive sky darkened | Scoped variables in `styles/storefront.css` |
| Inter + Poppins | Kept with limited weights and swap loading | Preconnected Google Fonts links in `index.html`; Sinhala/Tamil fallbacks in tokens |
| Pill actions | Kept and enlarged to 44–48px targets | Shared `.s-button*` primitives |
| Glass navigation | Improved with an opaque fallback and observer-based scroll state | `Navbar.jsx` and `.s-header` |
| Image hero slider | Restored with CMS data, pause/play, swipe, labelled controls, focus pause, and reduced-motion behavior | `components/home/HeroCarousel.jsx` |
| Image category cards | Restored with API media and a branded no-media fallback | `components/home/CategoryCards.jsx` |
| Product hover lift | Restored for fine pointers only | `ProductCard.jsx` and pointer media query |
| Four-column navy footer | Restored using settings-only contact/social content | `Footer.jsx` |
| Unsplash/texture hotlinks | Replaced | Catalogue Media media URLs plus CSS/SVG fallbacks |
| Font Awesome CDN | Replaced | One tree-shakable `react-icons/fi` source through `AppIcon.jsx` |
| Cream/amber/serif editorial system | Removed | New token system and deleted obsolete customer stylesheets |

### Token migration

| Revision 1 | Revision 2 |
|---|---|
| `--s-ink` | `--brand-navy-700` / `--brand-navy-900` |
| `--s-accent`, `--s-accent-ink` | decorative `--brand-sky-500`; interactive `--brand-sky-700` |
| `--s-bg` | `--bg-page` |
| `--s-surface`, `--s-muted` | `--surface`, `--surface-muted`, `--brand-tint` |
| `--s-text`, `--s-secondary`, `--s-text-muted` | `--text-primary`, `--text-secondary`, `--text-muted` |
| `--s-radius`, `--s-radius-lg` | `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-pill` |
| `--s-shadow` | `--shadow-1`, `--shadow-2`, `--shadow-3` |
| `--s-font`, `--s-display` | `--font-body`, `--font-heading` |

### Contrast pairs

| Foreground / background | Ratio | Use |
|---|---:|---|
| White / navy `#1a365d` | 12.14:1 | Primary buttons and dark panels |
| White / navy `#0f2d4a` | Greater than 12:1 | Hero/footer copy |
| Interactive sky `#2b6cb0` / white | 5.42:1 | Links, outlined actions, focus accents |
| Interactive sky `#2b6cb0` / tint `#e5edf6` | 4.59:1 | Links on tinted bands |
| Secondary text `#4a5568` / tint `#e5edf6` | 6.37:1 | Supporting copy |
| Decorative sky `#4299e1` | Not used for white-text buttons | Underlines, dots, borders, decorative marks |

Status colours are paired with text and icons. `prefers-contrast`, `forced-colors`, `prefers-reduced-motion`, and `prefers-reduced-transparency` receive explicit fallbacks.

### Hero CMS JSON contract

The existing single-object form remains valid:

```json
{
  "title": "Custom wedding stickers",
  "body": "Owner-written supporting copy",
  "ctaLabel": "Shop wedding stickers",
  "ctaUrl": "/products?category=wedding",
  "imageUrl": "/uploads/owner-media.jpg",
  "alt": "Wedding favour stickers arranged on a table",
  "align": "left",
  "objectPosition": "center center"
}
```

The carousel form uses up to five slides without a schema change:

```json
{
  "slides": [
    {
      "title": "Custom wedding stickers",
      "body": "Owner-written supporting copy",
      "ctaLabel": "Shop now",
      "ctaUrl": "/products",
      "imageUrl": "/uploads/owner-media.jpg",
      "alt": "Descriptive alternative text",
      "align": "left",
      "objectPosition": "center center"
    }
  ]
}
```

The Admin Content editor continues to store this as the existing JSON section content. Missing media produces a navy CSS sticker-sheet fallback. No product image, Instagram image, or external stock image is substituted.

### Performance, responsive, and accessibility verification

- First-slide media uses eager loading and high fetch priority; other carousel, category, collection, product, CMS, and thumbnail media are lazy.
- Explicit dimensions and aspect ratios reserve image space.
- Route lazy loading and request cancellation remain unchanged.
- Main production JS changed from 322.52 kB (106.02 kB gzip) to 346.27 kB (111.08 kB gzip): +23.75 kB raw and +5.06 kB gzip, primarily for the consolidated icon library and carousel UI.
- Chrome overflow checks passed at 320, 375, 390, 430, 768, 1024, 1280, and 1440 px.
- Keyboard checks cover skip navigation, drawer focus trapping/return, filter dialog, carousel buttons, pagination, native accordions, quantity controls, and forms.
- Carousel autoplay pauses for hover/focus/touch and starts paused under reduced motion. Inactive slides are inert.

Visual QA screenshots were generated and inspected locally, then removed before handoff: `revision2-home-desktop.png`, `revision2-home-mobile.png`, `revision2-shop-mobile.png`, and `revision2-login-desktop.png`.
