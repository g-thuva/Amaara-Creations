# Phase 8 quality result

Verification date: 2026-10-20. Gate A baseline: `ae6e4cc`, with its four coherent refactor/currency/architecture/documentation commits. See the [refactor result](full-repository-refactor-result.md) and [quality control guide](../quality/release-hardening.md).

Status: **local acceptance passed; GitHub Actions verification pending the reviewed develop push**. Phase 9 has not started at this snapshot.

## Changes and defects

- Reproduced concurrent refresh reuse on real SQL Server: two replacement tokens were issued. Added per-user SQL locking and atomic rotation/revocation; the regression now verifies one replacement followed by family revocation. Added expiry, unconfirmed, locked and revoked session checks.
- Reproduced avatar acceptance of executable text disguised as PNG/JPEG/GIF/WebP and acceptance of understated stream lengths. Shared bounded format validation now checks the exact bytes written for avatars and product/private media. Avatar requests gained the existing per-user upload limiter and a 6 MiB HTTP ceiling; only that endpoint's compatibility fixture entry was intentionally changed.
- Restricted the sitemap to active storefront-currency products and published CMS content. Product/CMS pages become indexable only after a public resource loads; missing resources keep noindex and no canonical/schema. Published CMS metadata and missing-page/product behavior are verified in the built browser.
- Bounded legacy customer/order pagination before offset arithmetic; removed customer-ID materialization and tracking from read paths; collapsed per-status customer order-statistic queries into one SQL aggregate. SQL HTTP regressions preserve currency-labelled totals and extreme-paging behavior.
- Verified process liveness, database readiness and recovery by taking only a newly generated disposable SQL database offline, restoring it in finally, and polling through SqlClient's reconnect blocking interval. Optional payment unavailability does not fail readiness.
- Extended database backup recovery to reconcile custom order/design snapshots, proof-to-artwork references, inventory ledger quantities and both order/payment currency totals. A separate fixture copy restores the private image's exact original bytes after the source file is deliberately changed.
- Corrected the shared image-fallback accessible label. Existing keyboard/modal/form/financial-action semantics were reviewed alongside automated accessibility checks.
- Enabled both deterministic quality workflows on develop pushes and retained safe SQL/browser diagnostics. No pull request, main change or production deployment is needed to run these workflows.

## Local final regression

| Check | Result |
| --- | --- |
| Backend restore / Release build | PASS; zero warnings/errors |
| Backend tests | **235 passed, 0 failed, 0 skipped** |
| Real SQL tests, included above | **10 passed**, including stock/idempotency/adjustment races, refresh reuse, outage/recovery, currency totals, schema upgrade, load and backup/restore |
| Architecture/route/policy/history checks | PASS; included in backend total |
| EF migrations / pending model | 16 IDs; no pending changes; no migration added |
| Node 22 npm ci / lint / TypeScript | PASS |
| Frontend unit/component tests | **21 files, 111 passed** |
| Full Playwright application journeys | **70 passed** |
| Built production-browser checks | **3 passed**, including metadata, accessibility and lab performance |
| Webhook ingress | **3 passed** |
| npm / NuGet audits | PASS; zero known vulnerabilities |
| Source graph | 361 source modules; 339 runtime reachable; zero missing imports, orphans or export shells |
| Secret scan / whitespace | PASS; zero candidates / diff errors |

The final full backend run follows all backend changes. The browser run covers the substantive SEO/security behavior; its built production checks include the final image-label correction. Full final release regression is required again after Phase 9 implementation.

## Measurements

Initial gzip JavaScript: **115705 / 128000 bytes**. Largest route: **4659 / 40000 bytes**. CSS: **78731 / 100000 bytes**. Local built-app LCP: **432 ms**; CLS: **0.00193**. These are local lab observations, not production field claims.

The eight-worker SQL workload issued 720 measured HTTP requests with zero failures and zero provider calls. One recorded run completed in 3.06 seconds with p95 77.62 ms; the final run's TRX/load artifact retains its own timing. Stock/order/payment invariants passed. No external provider was load-tested.

Backup CHECKSUM, VERIFYONLY, restore, migration and row/total/stock/reference/media checks passed. The strengthened rehearsal additionally checks custom snapshots, proof references and private-media bytes; its JSON artifact records the final timing. The earlier SQL-only fixture took 0.80 seconds. No database writes occur between the selected backup and its verification; private-file corruption is deliberately reversed from the fixture copy. These are not approved production RPO/RTO values.

## Gate review and limits

No known internal P0/P1 defect remains from this review. Relevant behavior, accessibility, SEO, performance, security, observability, recovery and concurrency checks pass locally. The [quality guide](../quality/release-hardening.md) records coverage, implementation ownership and limits, including static-SPA HTTP status semantics and media validation without full raster decoding.

External Zahls merchant acceptance, real email delivery, actual hosting/monitoring/storage destinations and owner business-policy approvals are not claimed by this gate. They do not replace any deterministic test. Remote CI results must be attached before advancing to Phase 9.
