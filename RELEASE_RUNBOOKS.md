# Amaara Creations release preparation

Prepared on 16 October 2026. These are reviewable procedures and inactive templates, not deployment authorization. No production host, domain, DNS record, provider credential, tax rule, shipping rate, capture rule, storage account, monitoring recipient or customer data has been supplied or invented.

The current implementation is a modular ASP.NET Core/SQL Server monolith with a React application. Preserve historical LKR amounts and Datatrans records. New CHF prices must be entered explicitly on separate products; catalog editing cannot relabel an existing currency. All financial comparisons and reconciliation are grouped by currency and provider environment.

## Environment boundary and safe configuration

Development uses local SQL plus either the existing privately configured SMTP provider or explicit `Email:Provider=DevelopmentCapture`. DevelopmentCapture writes a private inbox to `be/App_Data/development-email`; it does not contact recipients and is rejected outside Development. The ordinary log contains no confirmation/reset links or email body. Disposable acceptance uses a separate executable, in-memory database, generated accounts, private temporary artwork storage, fake payment adapter and captured email. Its `/__fixture/*` routes are absent from the deployed API.

Staging must use a separate database, private storage namespace, email test destination and Zahls TEST account. Restrict access and indexing; never restore production customer data into an unrestricted staging environment. Production configuration comes from an external secret store/environment or a private file outside the artifact. Do not copy the ignored development file. Keep encryption enabled on production SQL connections and validate the server certificate.

Use [the API example](deploy/appsettings.Production.example.json), [frontend environment example](deploy/frontend.production.env.example), [proxy template](deploy/nginx.conf.template) and [service template](deploy/amaara-api.service.template). Every placeholder needs an approved value. The API example deliberately cannot start until required SQL/JWT/CORS settings exist. `Zahls.Enabled` stays **false**. Its `Mode=LIVE` is an inactive future configuration example; the current adapter remains TEST-only and must not be represented as a verified LIVE integration. A merchant-authenticated contract and provider-approved LIVE activation change/review are required before enabling it. Never turn a TEST API key or webhook key into a LIVE key by changing a string.

Relevant keys:

| Area | Configuration |
| --- | --- |
| Database | `ConnectionStrings:DefaultConnection` (secret; never in frontend) |
| Authentication | `Jwt:Key/Issuer/Audience`, `Authentication:FrontendBaseUrl`, `RequireConfirmedEmail`, access/refresh lifetimes |
| Browser origin | exact `Cors:AllowedOrigins`; no wildcard credentials |
| Public metadata | `PublicSite:BaseUrl` and matching build-time `VITE_PUBLIC_SITE_URL` (approved HTTPS base including any path prefix) |
| Frontend | `VITE_PUBLIC_BASE_PATH`, `VITE_API_BASE_URL`, `VITE_MEDIA_BASE_URL`, `VITE_ROUTER_MODE=browser` |
| Reverse proxy | explicit `ReverseProxy:KnownProxies`; forwarded proto/for, one trusted hop |
| Session keys | `DataProtection:KeysPath`, persistent private volume, restricted ACL and backup |
| Email | `Email:Provider/Host/Port/Username/Password/FromEmail` |
| Checkout | `Currency`, approved tax/fee/capture/shipping settings; `AllowDevelopmentShipping=false` outside Development |
| Zahls | disabled until approval; separate instance/API/webhook keys, URLs, verified version/methods/mode |
| Historical payments | Datatrans settings retained privately only where historical reconciliation needs them |

No analytics vendor or destination has been approved. No external analytics tracker is enabled. Operational aggregate metrics and logs are available locally; a future analytics integration must exclude account identifiers, artwork, addresses and payment tokens and receive the necessary business/privacy approval.

## Build and deployment topology

Prefer one public HTTPS origin: static React assets at `/`, `/api/v1/*` proxied to the private API listener. Serve public product media separately from private artwork. Use a persistent SQL Server and persistent private storage/key volumes. The API process runs `CommerceMaintenanceService` for payment/refund reconciliation and the notification outbox; stopping every API instance also stops that worker. Do not scale background workers blindly without observing the existing database locking/outbox backoff.

Build on Node 22+ and .NET 8:

```powershell
dotnet restore tests/be.Tests/be.Tests.csproj
dotnet build tests/be.Tests/be.Tests.csproj --configuration Release --no-restore
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --no-build
dotnet publish be/be.csproj --configuration Release --no-restore --output .runtime/release/api
python scripts/check-release-artifacts.py .runtime/release/api
python scripts/scan-secrets.py
```

In `client`, run `npm ci`, lint, typecheck, tests, `npm run build`, `node scripts/check-performance.mjs` and Playwright. Set only the approved public Vite values before the production build. Vite values are public and compiled into the bundle. Verify artifacts contain no development/local settings, `App_Data`, user secrets, uploaded artwork, keys, test host or private database files. Publish checks explicitly exclude these paths.

Deploy versioned frontend/API artifact directories through the owner's chosen release system. Run under a dedicated service identity with only required directories writable. Preserve the key ring and public/private media independently of artifact swaps. Do not put the test host on a public listener. Health readiness must pass before traffic switches. No CI workflow in this preparation deploys the API or enables a payment provider.

## Domain, DNS, TLS and routing checklist (prepare only)

1. Owner supplies the canonical public host, hosting destination, DNS zone access and responsible operator. Record approved A/AAAA/CNAME targets and TTL; do not infer them from this repository or old GitHub Pages URLs.
2. Confirm staging isolation, certificate challenge method and renewal ownership. Issue/install the real certificate only after authorization. Verify hostname/SAN, chain, validity, expiry alerting and renewal rehearsal.
3. Render the nginx template, including certificate files, public media root, artifact root and private loopback upstream. Run `nginx -t` before any authorized reload. Trust forwarded headers only from the actual proxy addresses; the public client must not be able to forge HTTPS/IP context.
4. Verify HTTP redirects to the approved HTTPS canonical host without redirect loops. Start HSTS with the owner's reviewed lifetime. Do not add `includeSubDomains`/preload until every relevant subdomain is ready.
5. Confirm BrowserRouter fallback on a direct product/account/admin URL and refresh. Missing `/api/*` and `/assets/*` requests must return their real error, not SPA HTML. Legacy `/#/...` customer/bookmark/payment-return links redirect locally to clean paths.
6. Route public `/robots.txt` and `/sitemap.xml` to the API SEO endpoints. Set matching approved backend/frontend site URLs. Without a configured URL, robots disallows crawling and sitemap returns 503. Sitemaps include only active public products and public pages, never admin/account/payment/artwork endpoints. Account, admin, saved-design and payment pages carry `noindex,nofollow`.
7. Verify the public signed Zahls webhook URL is exact, HTTPS, reachable and not redirected. Proxy exact body bytes and headers; disable request/body logging and caching. A browser payment return is never proof of payment.

For a subpath deployment, update the public base, canonical base and proxy/fallback locations together. The nginx template describes root hosting; it must be rendered/reviewed for an approved subpath, not copied unchanged.

## Production database migration and validation

The business development database is intentionally not migrated by this work. All automatic SQL tests refuse databases without the `AmaaraPhase6Tests_` prefix. Current release schema adds `Phase7AdminOperations`, `Phase7ReviewDefaults` and `Phase7NormalizeLegacyEvidence` after `Phase6ZahlsTestCurrency`. The first adds review purchase linkage/moderation/rowversion, configurable low-stock thresholds, movement type/order references and operational indexes. Defaults/backfill retain legacy reviews as Pending and classify legacy adjustments without inventing verified purchases. Existing applied migrations were not edited. Down scripts can remove new columns/history; do not use them for routine financial rollback.

Before migration:

- Obtain explicit approval for the exact database, maintenance window, release hash, operator and reviewed SQL. Capture current `__EFMigrationsHistory`, product/user/order/payment/media counts, money by currency and mode, stock/reservations, outbox backlog and current artifact versions.
- Stop new checkout/admin mutations through the approved traffic plan. Decide how signed callbacks are durably retained during the window. Do not acknowledge an unpersisted callback as accepted.
- Create a protected `COPY_ONLY` database backup with CHECKSUM and retain the log-backup chain where the recovery model supports it. Run `RESTORE VERIFYONLY`, then a real restore to a new isolated database. Capture backup timestamps, checksum evidence, restore target, RPO/RTO and responsible owner. A backup that was never restored is insufficient evidence.
- Generate and inspect the exact forward SQL and idempotent script using the release EF tooling. Check table/column drops, defaults, constraints, rowversions, indexes, transaction scope and expected duration using a disposable copy. Preserve financial rows, currency values and historical providers.

Preparation commands (no production target is supplied here):

```powershell
dotnet ef migrations script 20261014112403_Phase6ZahlsTestCurrency --project be --configuration Release --output .runtime/phase7-forward.sql
dotnet ef migrations script 20261011173054_Phase6TransactionLifecycle --project be --configuration Release --output .runtime/business-db-forward-review.sql
dotnet ef migrations script --idempotent --project be --configuration Release --output .runtime/release-migrations.sql
dotnet ef migrations has-pending-model-changes --project be --configuration Release
```

The second script starts at the inspected business development database's current 12-migration state and includes its four pending migrations. Both forward boundaries and the full idempotent script were generated locally for review; none was executed against the business database. A production target must use its own verified starting migration.

For the authorized migration, inject the approved connection through the release secret mechanism, re-check its resolved database name, and execute the reviewed release migration/bundle with a dedicated migration identity. Do not run `dotnet ef database update` against whichever default connection happens to be configured. Deploy schema-compatible API before admitting new traffic; validate health/readiness, then the frontend. Never bootstrap a new administrator or auto-migrate production on startup.

After migration, compare the captured counts, money by currency/mode/provider, product currencies/prices, stock/reservation balances, media references and migration history. Check failed/pending payments, refunds and outbox items without issuing duplicate financial actions. Run authorized smoke tests, then reconcile the provider's settlement/refund evidence against stored records. Keep the old artifact and backup until the owner accepts the validation.

## Backup/restore and media rehearsal

`SqlReleaseRehearsalTests` creates isolated source/restore databases, takes COPY_ONLY/CHECKSUM backup, verifies it, restores to a newly named database without `REPLACE`, and checks migration history, currency-separated financial amounts, stock and media references. It retains identifiable disposable databases/backups for review. The measured fixture RPO/RTO is a development rehearsal, not a production recovery guarantee. A separate upgrade test starts at the verified Phase 6 schema, seeds LKR/CHF price/stock facts and verifies them after upgrading.

Run SQL tests only with a process-scoped connection to an approved disposable database:

```powershell
# Set AMAARA_TEST_SQL_CONNECTION privately to a disposable AmaaraPhase6Tests_* database.
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --filter "FullyQualifiedName~Sql"
```

Production backup planning must specify recovery model, full/differential/log schedule, off-host destination, encryption/key custody, retention, access, immutability and restore ownership. Retain the persistent data-protection key ring. Restore private artwork/proofs and public media to isolated storage, check a key-to-SHA256/size manifest, and verify authenticated downloads before releasing traffic. Do not print private file names or customer records in broadly accessible CI logs.

## Zahls external acceptance and payment cutover

**External acceptance is BLOCKED.** Deterministic payment/refund fixtures do not prove merchant acceptance. Owner must provide a TEST instance, API key, separate webhook signing key, approved CHF methods, provider setup and business shipping/tax/capture policy. Confirm the provisional HTTP version/request/response contract against authenticated merchant documentation before performing genuine operations.

Record a separate, private acceptance matrix for Gateway creation, authorized success, decline, cancellation, signed public callback, duplicate callback, verification/reconciliation, same-order retry, ambiguous response handling, full/partial refund and refund reconciliation. Retain safe provider references, timestamps, amounts/currencies/mode and expected versus actual state. Never retain card data, secrets or raw sensitive provider bodies in the report. A verified server event/API result must agree with the stored order and attempt; browser query parameters must not mark an order Paid.

Before LIVE: merchant KYC, production PSP, CHF methods, authenticated API version/contract, distinct LIVE keys, exact production webhook, shipping/tax/capture/refund/return/proof approvals, provider-approved success/decline/cancel exercise, signed callback, reconciliation and approved refund evidence must all be accepted. Review the currently TEST-only adapter for the authenticated LIVE contract as a separate activation change. Confirm historical Datatrans reconciliation remains provider-qualified. Do not auto-convert keys/mode, load-test Zahls, or issue a real refund from deterministic tests.

Leave `Zahls.Enabled=false` until explicit owner release authorization. Keep a fail-closed payment disable switch available through the protected deployment configuration. Disabling new payments must preserve callbacks, refunds under review, historical rows and reconciliation investigation.

## Email cutover

Owner supplies the sender domain, mail service, authenticated host/port, username/secret, From identity, permitted recipient/test address and monitoring destination. Configure SPF/DKIM using the actual provider values. Review DMARC alignment/reporting and move enforcement only after legitimate mail is accounted for; never invent DNS TXT values.

In staging verify verification and reset links, expiry/one-use behavior, order/payment confirmation, proof request/approval/revision and shipment notices. Ensure HTTPS public base links, correct language/content and no public artwork URL. Simulate outage/backoff and verify outbox delivery state is marked sent only after successful delivery. Review spam/bounce outcomes, sender alignment and provider logs. Production notification acceptance remains blocked until a real authorized delivery is confirmed. DevelopmentCapture is a local inbox only.

## Storage cutover

Owner chooses the object-storage provider, account, region, public/private containers, credentials, retention/backup and CDN. The existing `IMediaStorageService` is the adapter boundary; an authenticated, reviewed adapter for the chosen provider is required before cutover. Do not pretend that local storage is an accepted production object-storage integration.

1. Inventory product media, private artwork and private proofs by logical storage key, size/hash, content type, ownership and referenced entity. Keep the manifest private; do not make artwork public to simplify migration.
2. Provision separate public and private permissions. Public product media may use a CDN. Private containers prohibit anonymous listing/read; serve through the existing authorized download endpoints or narrowly scoped expiring signed access after ownership/staff checks. Restrict content types and attachment behavior.
3. Copy objects to a staging namespace, preserve keys or produce a reversible old-key/new-key mapping, compare counts/bytes/hashes, and reconcile missing/orphan references. Preserve originals until the owner accepts the mapping. Do not rewrite immutable order design specifications to point to unrelated assets.
4. Validate owner/staff authorized access, other-customer denial, proof privacy, cache headers, expiry/revocation, large/corrupt uploads and public CDN cache behavior. Verify object versions, lifecycle deletion, legal/business retention, backup and restore.
5. Freeze/copy deltas at the approved window, switch only the reviewed storage adapter/configuration, run smoke reads/writes, and retain a tested return path to the old storage mapping.

## Release smoke execution plan

Internal executable smoke: backend suite, SQL suite, frontend checks, `npm run test:e2e`, `node scripts/run-production-browser.mjs`, webhook ingress tests, artifact privacy check, source scan and disabled cutover-gate check. These use disposable/fake infrastructure only.

Authorized staging/production smoke must record case, operator, UTC timestamp, release hash, environment, safe evidence and PASS/FAIL/BLOCKED:

| Area | Cases |
| --- | --- |
| Public | home, catalog/search/filter/paging, active product, unavailable product, clean deep-link refresh, custom builder, cart, login/register/reset, private account/order/design |
| Payment | Gateway initialization, approved payment, signed callback, duplicate callback, authoritative status, cancellation/decline, same-order retry, reconciliation, approved refund |
| Admin | role-appropriate login; catalog/price; order snapshot/history; customer; production/proof; stock adjustment/ledger; shipment/tracking; payment/refund; moderation; coupon; reports/CSV; audit; staff separation |
| Operations | liveness/readiness, migration version, SQL, outbox/backoff, email delivery, authorized storage, signed webhook, safe correlation/logs, backup and restored-data checks |

Production-only financial transactions are not authorized by this runbook. Use an owner-approved operator and limits; reconcile/refund the smoke order only when the business explicitly permits it. Do not delete the financial history afterward.

## Rollback / forward-fix

Assign an incident lead, communications path and approved criteria before cutover. Capture the incident time, last good artifact, schema, traffic state and provider event window. Disable new checkout if financial consistency is uncertain; retain webhook ingress and durable event evidence. Do not erase orders/payments/refunds/audit rows or discard queued notifications to make balances look correct.

- Frontend: point the static release symlink/origin at the last compatible artifact; invalidate only the HTML entry, preserve hashed assets for open sessions, and verify current API route/DTO compatibility.
- API: switch to the last schema-compatible API artifact and keep private media/key mounts. Stop/drain workers consistently. An old API without new columns/features must not mutate records it cannot understand.
- Schema: prefer a reviewed forward fix for financial/history fields. Down migrations remove newly introduced history/columns and are not the default recovery mechanism. Rehearse any approved rollback on a disposable restored database first.
- Restore: consider only under incident-owner approval after determining the accepted RPO, all post-backup order/payment/refund events, provider settlement state, stock reservations, outbox delivery and media versions. Restore into a new database, reconcile external events, validate, then switch connections. Never overwrite the business database with a test restore.
- Webhooks during outage: retain exact signed bytes only in an approved private durable incident store if the application cannot persist them, or let the provider retry. Do not return success for an event that was dropped. Reprocess idempotently after service recovery and compare provider references/amounts/currency/mode.
- Notifications: retain the outbox, attempt/sent timestamps and event keys. Review sent versus uncertain deliveries before replay; external SMTP cannot guarantee exactly-once delivery after a process crash.
- Media: restore compatible object versions/key mappings and authorized access. Do not expose private containers as a fallback. Feature-disable checkout/new production transitions while reconciliation remains unresolved.

Re-run readiness, privileged permission checks, price/currency/stock/media comparisons and payment reconciliation before restoring traffic. Record every emergency action in the incident/audit evidence.

## First-day monitoring

No monitoring service, recipients or production thresholds are approved. Connect the `Amaara.Api` Meter (`amaara.http.duration`, `amaara.http.server_errors`), JSON console output and health endpoints to the chosen system. Request logs use route templates and omit query strings/bodies. Correlation IDs join operational/audit evidence. Protect logs and set an approved retention policy.

`/health/live` is process liveness; `/health/ready` checks database connectivity and pending migrations and returns 503 when unready. `/api/v1/admin/operational-health` requires report permission and returns aggregate outbox exhaustion, payment review/pending/refund counts, negative stock and old production jobs without customer identities. Production delay detection currently uses seven days as a diagnostic bucket, not an invented fulfillment SLA; the alert threshold requires owner approval.

| Signal | Proposed review trigger (approve/tune before launch) |
| --- | --- |
| API 5xx / latency | `<MAX_ERROR_RATE>`, `<WINDOW>`, `<P95_MS>`; compare route and release |
| DB readiness | any sustained 503 over `<READY_FAILURE_WINDOW>` |
| Frontend errors | `<ERROR_COUNT/WINDOW>` with sanitized route/build identifiers; no form values |
| Webhook failures / reconciliation mismatch | any confirmed mismatch requires payment review; retry alert threshold `<WEBHOOK_FAILURE_LIMIT>` |
| Pending orders/payments | investigate age buckets; use approved provider expiry, not automatic stock release after an uncertain payment |
| Production backlog | `<QUEUE_AGE>` / `<QUEUE_COUNT>` approved by production owner |
| Stock | any negative stock is an incident; low stock uses per-product/variant approved thresholds |
| Outbox/email | exhausted attempts > 0 or oldest unsent age `<OUTBOX_AGE>` |
| Storage | `<ERROR_RATE>` and missing reference/hash checks |
| Admin login | `<FAILED_LOGIN_THRESHOLD>`; verify lockout and IP/user abuse controls |
| Backups | latest successful age `<BACKUP_AGE>`, off-host copy age and scheduled restore drill |

Assign named on-call recipients and review cadence privately. Confirm alert delivery and escalation in staging. During day one record readiness, 5xx, money by currency/mode/provider, outstanding webhooks/refunds, queued notifications, stock and production backlog at the approved intervals. Do not use made-up recipients or treat local console output as accepted production monitoring.

## Final cutover gate

Copy the [gate manifest](deploy/cutover-gate.example.json) into a private release evidence location and fill each approval only from genuine evidence. Run:

```powershell
python scripts/check-cutover-gate.py deploy/cutover-gate.example.json
```

The committed example intentionally returns exit 2 and **PRODUCTION CUTOVER BLOCKED**. Missing database/hosting/domain/TLS, Zahls LIVE credentials/contract/methods/webhook, business shipping/tax/capture/proof/refund policy, email, private object storage, monitoring/backup destinations or explicit owner launch authorization prohibits deployment. The command never deploys, changes DNS, migrates production or enables Zahls.
