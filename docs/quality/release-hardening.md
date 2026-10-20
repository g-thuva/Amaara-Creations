# Quality and release hardening

Phase 8 builds on the [completed repository refactor](../reports/full-repository-refactor-result.md). Use the [test guide](../architecture/testing.md) for commands. All payment/browser/load fixtures are deterministic; merchant acceptance and production capacity require separate evidence.

## Coverage and ownership

| Requirement | Executable evidence / owner |
| --- | --- |
| Authentication, expiry, lockout, replay and revocation | Account/auth Playwright journeys; RefreshSessionStateTests; real-SQL RefreshSessionConcurrencyTests. |
| Catalog, variants, archive and public currency | ChfCatalogTests, ReleaseSecurityAndSeoTests, admin catalog/browser journeys. |
| Custom quote versions, artwork ownership, proof and production transitions | CustomDesignWorkflowTests, StaffAndProductionTests, storefront artwork and admin production journeys. |
| Authoritative checkout, stale prices, address/shipping validation and immutable snapshots | CheckoutLifecycleTests and SQL commerce tests. |
| Promotion disabled/expiry/future/minimum/usage/customer/currency/eligibility rules | CommerceRulesTests; checkout integration and marketing browser journeys. |
| Order, shipment, production and payment transitions | Domain policies, CommerceRulesTests, CheckoutLifecycleTests, production/refund HTTP/browser journeys. |
| Provider mapping, signed callbacks, amount/currency/reference checks, retries and reconciliation | ZahlsAdapterTests, DatatransAdapterTests, PaymentHttpSecurityTests, RefundHttpTests and webhook ingress tests. |
| Last stock, overlapping checkout/idempotency and competing adjustments | SqlCommerceTests and SqlReleaseRehearsalTests using real SQL Server. InMemory results do not establish concurrency. |
| IDOR and staff permissions | PaymentHttpSecurityTests, AdminOperationsTests, ReviewModerationTests; private artwork and role-aware browser journeys. |
| Frontend resources, tables, pagination, malformed snapshots and stale requests | Colocated feature/shared tests and admin architecture browser journeys. |
| Standard/custom purchase and payment failure/cancel/retry/review states | Checkout and customer lifecycle browser journeys against the fake gateway. |
| Accessibility and responsive critical paths | Browser axe checks plus keyboard/focus/navigation assertions at mobile and desktop widths. |
| Public SEO and missing resources | Sitemap API assertions, SEO unit rules and built production-browser metadata checks. |
| Recovery and load | CHECKSUM/VERIFYONLY SQL backup/restore reconciliation; eight-worker, 720-request HTTP/SQL workloads. |
| Liveness/readiness and outage recovery | HealthSemanticsTests changes only a newly generated disposable SQL database; liveness stays 200, readiness becomes a safe 503 and recovers. |
| Architecture/history compatibility | ModuleBoundaryTests and CompatibilityContractTests. The avatar contract deliberately gained a rate limit and 6 MiB HTTP body ceiling in Phase 8; its single fixture entry records that reviewed change. |

## Security controls

API policies cover all protected operations; the compatibility fixture tracks all 163 endpoint contracts and 15 policy-role assignments. Ownership checks protect customer orders, payment operations, proofs and private artwork. JWT validation reloads current roles and lockout state. Refresh cookies remain HttpOnly, SameSite Lax and Secure outside development. Logout/revocation prevents subsequent refresh; already issued access tokens retain their short expiry and current database authorization checks.

Refresh-session creation, rotation, logout and family revocation use a per-user SQL application lock and transaction. Concurrent reuse produces one replacement, then revokes that family. Token hashes, never raw refresh values, are persisted. The lock covers local database work only and does not share the global commerce lock.

Avatar and product/private uploads share bounded byte reads and format validation. File extensions, declared MIME, detected format, container structure and dimensions must agree. PNG validation checks chunk boundaries/CRC/end; JPEG/WebP retain existing format checks; avatars additionally validate GIF block boundaries, canvas and trailer. Bytes written are exactly the bytes checked. Randomized filenames, path containment and private storage authorization remain enforced. The maximum defaults are 5 MiB and 24 million canvas pixels; avatar HTTP multipart requests also have a 6 MiB ceiling and per-user upload rate limiting. These checks do not claim full raster decoding or malware scanning.

Sensitive auth endpoints and custom quotes/uploads have named rate policies. CORS uses explicit origins with credentials; forwarded headers trust only configured proxies. Production uses HTTPS/HSTS. API responses apply nosniff, frame denial, restrictive CSP and referrer policy; sensitive responses use no-store. Redirect/provider references, signature input, TEST/LIVE separation and financial ambiguity remain fail closed. Dependency and secret scans run independently of external provider availability.

## Accessibility review

The target is WCAG 2.2 AA. Automated axe results cover sampled critical screens, not a blanket certification. The code/keyboard review covers shared native dialogs, tab wrapping, Escape and focus restoration; storefront skip link, landmarks, navigation and mobile drawers; form labels/error announcements in auth/account/checkout; labelled admin refund controls and confirmation dialogs; table/pagination semantics; builder labels; image alternatives/dimensions and reduced motion. Existing controls use explicit text as well as status colors. A malformed image-fallback accessible label was corrected during this review.

Keyboard browser assertions exercise the mobile navigation/filter dialog, admin navigation, builder upload/removal and checkout controls. Responsive journeys cover 320–1440 px. Continue manual assistive-technology and content review when real production copy/media changes.

## SEO

BrowserRouter public URLs use the approved HTTPS base from configuration. Unknown/private routes have no canonical link and noindex. Product and dynamic CMS routes opt into indexing only after a public resource loads; missing products/pages retain noindex and show an explicit unavailable/not-found state. Published content gets its own title, description, Open Graph metadata and canonical URL. Product structured data uses actual price/currency/availability; Organization and Breadcrumb data use the configured origin. No domain, currency or organization contact fact is invented.

The API sitemap includes active products in the configured storefront currency, static public routes and published CMS pages. It excludes drafts, inactive products, other-currency products, accounts, admin, checkout, orders, callbacks and private artwork. XML nodes and CMS path segments are encoded. Robots disallows private paths and blocks all indexing when the canonical host is absent. Public deployment must route `/robots.txt` and `/sitemap.xml` to the SEO endpoints.

The public API returns actual 404 responses for missing resources. The static SPA serves its routing shell on deep links; a browser-rendered not-found state and noindex are verified. This is not a claim of server-rendered HTTP 404 responses for every unknown frontend URL.

## Performance budgets and query review

| Measure | Local engineering guard |
| --- | --- |
| Initial storefront JavaScript gzip | At most 128000 bytes. |
| Largest route JavaScript gzip | At most 40000 bytes. |
| Total built CSS | At most 100000 bytes. |
| Desktop built-app LCP / CLS | Less than 4000 ms / 0.1 in the local browser lab. |
| Controlled HTTP workload p95 | Less than 5000 ms under eight local workers; a regression ceiling, not a production SLO. |
| Admin list sizes | Default feature sizes; hard maxima normally 100. Customer/order legacy paging is bounded before offset arithmetic. |
| Upload payload / dimensions | Default 5 MiB / 24 million canvas pixels; separate avatar HTTP ceiling above. |

Public entry loading, admin and custom-builder route chunks were measured. Existing lazy routing already isolates admin/builder code; no broad memoization or dependency replacement was justified. Shared media reserves width/height, uses lazy decoding/loading and eagerly prioritizes intentional hero content. Actual build and browser measurements are in the Phase 8 result report; field performance depends on hosting, real media and geography.

Catalog/PDP use read-only filtered projections. Checkout remains transactional and authoritative. Orders, inventory and production use their existing scoped filters and list limits. Customer listing now joins role membership in SQL instead of materializing all customer IDs; customer histories use no-tracking projection; order statistics use one grouped SQL query instead of repeated per-status round trips. Currency totals remain separate. No measured query pattern justified a new index/model migration for this small-store workload.

## Observability and health

JSON console logging includes correlation scopes. `ReleaseTelemetryMiddleware` records `Amaara.Api` duration histograms and server-error counts with route templates, method and status. It logs neither query values nor bodies/tokens. Webhook/reconciliation/checkout/storage failures are attributable by safe route, status and correlation ID. Background maintenance/email logs use record IDs and exception types, excluding message payloads and credentials.

The protected operational-health endpoint reports unsent/exhausted outbox entries, old pending payments/refunds, payment-review orders, negative stock and delayed production. It contains counts and timestamps, not customer payloads. A production collector/alert destination can consume the structured logging/metrics and this endpoint after its credentials and access policy are supplied.

Frontend telemetry goes through the central `clientTelemetry` boundary. Events contain allowlisted scope, status and error kind; request configuration, response bodies and arbitrary exception messages are excluded. Connecting a real monitoring sink remains deployment configuration, not an invented integration.

`/health/live` establishes process liveness; optional payment/email outages do not fail it. `/health/ready` checks database access and pending migrations, returning only safe ready/not_ready data. SQL outage/recovery tests include the SqlClient reconnect blocking interval; this is not a production recovery guarantee.

## Backup, recovery and load

Disposable SQL rehearsal creates representative records, backs up with CHECKSUM, runs VERIFYONLY, restores to a separate target, checks migrations and reconciles counts, currency-labelled totals, stock and order/payment/custom-design/media references. Recorded timings describe this local fixture only. A real backup also needs encrypted off-host database copies, keys, private/public media, storage metadata, object versions and a manifest tying them to an application/schema version. See the [cutover runbook](../runbooks/release-cutover.md).

Controlled workloads warm up then exercise catalog/product reads, cart, quote, orders, admin orders, inventory, reporting and production queue. They record requests, failures, elapsed time and p95 while asserting zero provider initialization/refund calls and unchanged financial/stock state. Separate SQL stress cases verify last-stock, idempotency and competing adjustment semantics. No external payment service is load-tested.

## CI and evidence

Both quality workflows run on develop pushes, pull requests and explicit dispatch. Node 22 and .NET 8 builds use fake providers and disposable SQL; no private Zahls secret is required. SQL CI checks migrations/model, all backend tests, publish privacy, dependencies, secrets and the deliberately blocked unapproved cutover gate. Browser CI performs clean install, lint/types/unit tests, all journeys and built-app accessibility/metadata/performance. Relevant TRX, SQL load and browser lab evidence is retained for 14 days. New runs cancel superseded runs for the same ref.

Record local and remote results separately. A local pass is never labelled as a successful GitHub Actions run. Phase 9 begins only after the Phase 8 gate has passed.
