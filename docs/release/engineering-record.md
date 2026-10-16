# Amaara Creations Phase 7–9 engineering record

Current work: 16 October 2026, branch `develop`, starting HEAD `e88ce6a`. The existing Phase 6 implementation was preserved and is included as the prerequisite for the completed admin workflows. The owner's follow-up explicitly requires professional, meaningful commits and a push to `develop` after validation. Unrelated pre-existing model-edit helper scripts remain untouched and excluded from those commits. No production migration, production deployment, DNS change or real payment/refund was performed.

The new continuation request explicitly authorizes Phase 7, Phase 8 and non-secret Phase 9 work. It supersedes the historical “not ready for Phase 7” statements in ignored Phase 6 reports, without changing the blocked real-provider acceptance gate. Source inspection included git history/status/diff, the existing Phase 6 implementation/recovery reports, the custom-builder documentation and all existing operational controllers/routes. No separate master rebuild blueprint was present in the repository; this continuation request supplied the acceptance scope. `PROJECT_ANALYSIS.md` is an older historical analysis, not current implementation status.

## Implementation

| Area | Current engineering |
| --- | --- |
| Order detail | registered route; immutable item/address/coupon/shipping/totals/currency snapshots, payment attempts/events/refunds, fulfillment form, transition confirmation/reason and timeline |
| Customer detail | registered route; necessary profile/verification/lock state, addresses, order count/history, lifetime settled/refunded value separated by currency/mode, design and review history; bounded results |
| Production/proof | existing domain states retained; queue search/filter/paging, immutable specification, private artwork/preview/proof download, revisions/customer responses, paid-order/proof gate, validated production chain, actor/time/note audit, outbox notices |
| Inventory | base/variant/SKU balances with on-hand/reserved/available/threshold; paginated movement ledger, reasoned optimistic adjustment under the existing SQL commerce lock, negative/stale guards and atomic audit; reservation/release/consumption evidence |
| Payments/refunds | provider-neutral list/detail, mode/reference/Gateway/attempt/timestamps/event/refund visibility; separate financial permissions; confirmation and idempotency retained; real provider remains disabled/fail-closed |
| Shipments | existing business-neutral shipment model/transitions retained; tracking/carrier/notes/events and customer tracking; corrected record validation defect exposed by a real HTTP test |
| Promotions | normalized unique code, fixed/percentage value, dates, currency, min/max, global/customer limits, enabled/standard/custom flags, optimistic editing and redemption history; existing authoritative SQL checkout reservations retained |
| Reviews | server-derived eligible fulfilled paid order-item linkage, Pending/Published/Hidden/Deleted states, owner edit/delete/restore, reasoned moderation and audit; no public customer ID/email/order ID |
| Staff | SuperAdmin, legacy Admin, CatalogManager, ProductionStaff, CustomerSupport, Marketing and Analyst policy matrix; server enforcement; current-role refresh for valid JWTs, refresh-session revocation on staff change, self/last-SuperAdmin guards; guarded navigation |
| Audit | read/filter/page evidence; append-only through application persistence, secret-field redaction, correlation, actor/time/before-after metadata; no edit/delete admin endpoint |
| Reports | currency/environment-separated settled/refunded funds, order status, ordered items/custom volume, queue, stock movements and promotions; date filters and bounded CSV without customer data, proper quoting/formula protection |

Production proof policy is unapproved. Generic proof support uses existing configuration/snapshots and test-only fixtures; no business proof rules were selected. Shipping/tax/capture/refund rules and genuine CHF commercial prices remain owner inputs. Catalog creation now supports explicit currency and price, prevents changing the currency label of an existing product and directs balance changes to the inventory ledger.

## Schema and safety

Release contains 16 migrations: the previous 13 plus `Phase7AdminOperations`, `Phase7ReviewDefaults`, `Phase7NormalizeLegacyEvidence`. Applied Phase 6 migrations were preserved. New defaults/backfill retain legacy reviews privately as Pending and do not invent verified-purchase links. Audit/stock evidence is append-only through the normal DbContext path; production database credentials still require least privilege and DBA protection from direct SQL tampering.

Disposable SQL verification includes fresh migration, upgrade from `Phase6ZahlsTestCurrency`, stock/coupon/refund/idempotency races, manual stock race, backup/restore and controlled SQL HTTP workload. The business development database is not automatically migrated. Test code refuses non-disposable names. No financial rows are deleted during recovery.

## Quality and release preparation

Public URLs use BrowserRouter with a safe local redirect for old hash links. Route metadata/robots boundaries, canonical/Open Graph and Organization/Breadcrumb/Product JSON-LD use an approved HTTPS public URL; none is invented when the value is absent. The API provides active-product sitemap/robots endpoints. Production reverse-proxy fallback and indexing setup are in [the runbooks](../runbooks/release-cutover.md).

New axe WCAG 2.2 AA rule scans plus browser keyboard tests found/fixed category accessible-name, admin contrast and squeezed mobile navigation controls. Dialogs use native modal focus/restore, tables have captions/column headers and keyboard-scrollable regions, loading/errors use status/alert, and server permission rejection has a clear screen. Mobile navigation is exercised through opening, Escape and focus return. Automated checks are not a claim of universal WCAG certification.

Admin and builder routes stay lazily split. The current initial JS entry is within the 128,000-byte gzip budget; individual route chunks have a 40,000-byte gzip budget and total CSS a 100,000-byte budget. Images already specify dimensions/loading/decoding. Dashboard monthly revenue no longer runs a query per month. Reports aggregate server-side, exports are bounded, and operational lists use capped paging/indexes. A controlled eight-worker, 720-request HTTP workload exercises catalog/detail/quote/cart/orders/admin/production/inventory/reports without provider network calls. These are local rehearsal metrics, not production capacity guarantees.

Security work includes distinct financial/staff/settings/audit policies, current staff roles on authenticated requests, IDOR regressions, safe audit metadata, per-IP/account rate partitions, trusted proxy configuration, HTTPS/HSTS expectations, no-store private responses, safe headers, log redaction and release artifact exclusions. .NET packages were updated within .NET 8 and the SQL client to remove the vulnerability feed's reported transitive findings. npm's dependency audit is separate from source/authorization verification.

Observability uses structured scoped logs, correlation IDs, aggregate operational health, database/migration readiness and a local Meter. No vendor key or alert recipient is required or invented. Private local email capture is explicit Development-only; real SMTP credentials remain private. [Runbooks](../runbooks/release-cutover.md) cover build/topology, environments/secrets, DNS/TLS, migration, provider/email/storage cutover, smoke, backup/restore, forward-fix/rollback, first-day monitoring and the blocked cutover gate. Inactive templates are in `deploy/`.

## Verification record

| Check | Result and scope |
| --- | --- |
| Backend Release build | PASS; zero warnings/errors |
| Backend regression | PASS; 200 passed, zero failed, zero skipped with disposable SQL enabled |
| Phase 7 focused cases | PASS; 30 admin operation/policy cases, six review cases and two actual JWT/proof/production/shipping journeys, included in the 200 |
| SQL cases | PASS; seven included cases: three existing commerce races, migration upgrade, manual-stock race, backup/restore and SQL HTTP load |
| Frontend | PASS; 78 tests; ESLint, TypeScript and production build pass on Node 22.14.0 |
| Browser regression | PASS; all 57 cases in 10 files, including clean-route checkout/artwork race handling, admin operations, keyboard behavior and automated accessibility |
| Production-build browser | PASS; three cases cover deep-link reload, canonical/structured data/indexing boundaries, local performance and mobile/desktop accessibility/layout |
| Webhook ingress | PASS; three deterministic byte/signature/routing cases; no genuine provider callback |
| Accessibility | PASS for automated WCAG 2.2 AA rule scans on seven public and nine admin views, keyboard checks and production inventory at 375/1440 px; human certification is not claimed |
| SEO | PASS for route titles/descriptions, approved-base canonical/Open Graph/JSON-LD, active-product sitemap and private-route noindex; production host/Search Console acceptance remains blocked |
| Dependency audits | PASS; npm reports zero vulnerabilities and NuGet reports zero known vulnerable direct/transitive packages at execution |
| Source/artifact privacy | PASS; source scan checked 362 text files with zero candidates; published API excludes private settings, media, keys and development captures |
| EF/schema | PASS; 16 migrations, no pending model changes; Phase 7 and four-migration business-database forward scripts inspected without DROP TABLE/COLUMN, DELETE or TRUNCATE; full idempotent script generated for target-specific review |
| Cutover gate | BLOCKED as required; the unapproved example returns exit 2 and performs no deployment |

The final production-build browser sample recorded LCP 1,148 ms and CLS 0.00323. Initial JavaScript gzip was 116,948 bytes against 128,000, largest route gzip 3,938 against 40,000, and total CSS 64,939 against 100,000. The sample is local desktop lab evidence, not field Core Web Vitals. Mobile and desktop screenshots were visually inspected; the inventory tables scroll within their labelled regions without page overflow.

The final SQL-backed workload used eight workers for 720 requests: zero failures, zero provider network calls and p95 615.33 ms. This is a controlled local rehearsal, not a production capacity guarantee. The latest COPY_ONLY/CHECKSUM/VERIFYONLY backup and restore into a new disposable database, including validation, took 6.70 seconds. Rehearsal RPO was zero intervening fixture writes. It verified both LKR/CHF financial rows, stock, media references and migration state; these measurements are not production RPO/RTO commitments.

The business development database was inspected read-only: 12 applied migrations ending at `20261011173054_Phase6TransactionLifecycle`, 19 products, zero orders/payments and zero shipping zones/methods. It remains unchanged and four migrations behind the release. No pending model changes is a source/model check, not permission to migrate that business database.

No real Zahls credential, genuine provider callback/payment/refund, approved business policy or production infrastructure acceptance is represented by these internal tests. The CI workflows are prepared; hosted Linux CI execution and authorized staging/production smoke remain separate gates. Local artifacts under ignored `.runtime/` and test build directories retain rehearsal output without publishing private configuration.

## External blockers

- Zahls TEST instance, API key, signing key, configured CHF methods and authenticated provisional contract verification.
- Genuine provider success/decline/cancel/retry/reconciliation/refund and public signed callback evidence.
- Owner shipping, tax, capture, proof and refund/return policies.
- Approved production database migration, hosting/domain/TLS, distinct Zahls LIVE setup/contract activation, production email, chosen private object-storage adapter/account, monitoring/backup destinations and explicit launch authorization.

Do not report production launch or real-provider acceptance complete. Missing external inputs do not invalidate completed internal engineering, but final engineering status must reflect any still-unverified internal checks.

**ENGINEERING / RELEASE PREPARATION COMPLETE TO CURRENT AVAILABLE GATE**

**PRODUCTION CUTOVER BLOCKED — EXTERNAL SECRETS / BUSINESS APPROVALS REQUIRED**
