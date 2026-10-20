# Production cutover execution guide

Prepared 21 October 2026. Engineering procedures and local rehearsals do not authorize a production deployment. The [readiness inspector](../../scripts/check-cutover-readiness.py) reports availability without printing values. The current inspection found all 17 prerequisites unavailable, including genuine Zahls TEST credentials. Actual production execution is **EXTERNAL CUTOVER BLOCKER**.

Read this with the [integration/recovery runbook](../runbooks/release-cutover.md), which defines provider acceptance, storage migration, email DNS and financial rollback. The current Zahls adapter deliberately implements TEST only. Even complete credentials require a merchant-authenticated LIVE contract and a reviewed activation implementation before LIVE can be enabled. No flag or checklist converts this implementation into LIVE support.

## Immutable release preparation

Use a clean `develop` checkout, Node 22+, npm and .NET 8. The default mode produces a **rehearsal** package with a reserved example canonical host and both providers disabled. No command here deploys or migrates a business database.

```powershell
python -m unittest discover -s tests/release_tools -v
python scripts/prepare-release.py --public-config deploy/frontend.rehearsal.json --output .runtime/releases/rehearsal-unique-id
python scripts/verify-release.py .runtime/releases/rehearsal-unique-id
# Set AMAARA_TEST_SQL_CONNECTION privately to local AmaaraPhase6Tests_* SQL.
dotnet run --project tests/be.ReleaseRehearsal/be.ReleaseRehearsal.csproj --configuration Release -- .runtime/releases/rehearsal-unique-id
```

Only an explicitly dirty local rehearsal may use `--allow-dirty-rehearsal`; its manifest records that fact. CI builds clean rehearsals for pushes/PRs, never production. A release manifest identifies source SHA/branch, source cleanliness, tool versions, lockfile hash, all 16 migration IDs, exactly five public build settings and every payload SHA-256. Verification rejects modified/missing/extra files, secrets, private/development settings, private-media directories, test executables, backup/key files and loopback deployment URLs. Reverify after transfer and before switching a release. Keep evidence outside the immutable package.

The builder uses `npm ci`, deterministic Release compilation, an idempotent migration script and an isolated empty Vite env directory. It never reads developer `.env` files. Published defaults contain no DB/JWT/email/provider secrets, disable integrations/bootstrap and pause payment/upload/admin writes. Two dependency URL-parser fallback literals are bound to the declared public browser origin; no application endpoint or installed dependency is rewritten. Source maps and development servers are excluded. Deterministic build settings and locked dependencies are recorded; byte-for-byte equivalence across different operating systems/SDK patches is not asserted.

For a future approved production package, render [public configuration](../../deploy/frontend.production.json.example) privately, identify the exact clean develop SHA and provide the complete private [approval evidence](../../deploy/cutover-gate.example.json). Then use `--mode production --public-config <private-public-json> --approvals <private-evidence-json> --output .runtime/releases/<unique-release>`. This only builds; it cannot activate LIVE, migrate or deploy. Keep secrets exclusively in the host secret store/environment/private runtime settings.

## Configuration and topology

Use the [application schema](../../deploy/appsettings.Production.example.json), [operator inventory](../../deploy/operations.example.json), [nginx template](../../deploy/nginx.conf.template), [header snippet](../../deploy/security-headers.conf.template), [systemd unit](../../deploy/amaara-api.service.template) and [staging restriction](../../deploy/staging-access.conf.template). The operator inventory documents external provisioning; these keys are deliberately not pretend application adapters.

The topology is HTTPS nginx → same-origin static `web` and private loopback API → SQL Server. systemd runs the API and its durable maintenance worker. Docker is used only for disposable CI SQL; no production Docker/Kubernetes topology is introduced.

1. Create a dedicated service identity, an immutable versioned package directory and a separate persistent runtime directory. Copy the sanitized `api/appsettings.json` into that runtime directory, preserving artifact immutability. Supply private overrides through the protected environment file/secret store. systemd points `--contentRoot` at this runtime directory and the executable at the versioned `api/be.dll`.
2. Mount public media at runtime `wwwroot/uploads`, private artwork/proofs at runtime `App_Data/private-media`, and keys at the explicit private `DataProtection:KeysPath`. `PUBLIC_MEDIA_DIRECTORY` and `PRIVATE_ARTWORK_DIRECTORY` in systemd/nginx must resolve to these exact mounts. Never mount keys or artwork under nginx's public root. Only those persistent directories are writable. nginx requires read access only to public media, despite systemd `UMask=0077`; approve dedicated public-media group/default ACLs. Never grant nginx access to private artwork or keys.
3. Supply the exact SQL identity with encryption/certificate validation, strong JWT key/issuer/audience, valid explicit hosts, exact credentialed CORS origins, HTTPS auth frontend and trusted proxy IPs. Production rejects missing/unsafe host, key path or enabled SMTP settings, unsupported storage/email adapters and development shipping. `DevelopmentCapture` remains Development-only.
4. SMTP is optional until explicitly enabled. `Email.Enabled=false` sends nothing and leaves durable outbox work unsent/retryable; financial commits do not depend on SMTP success. Monitor exhaustion before enabling. An enabled SMTP provider requires real approved settings. Existing notification/auth templates and acceptance tests use captured infrastructure.
5. Only the Local media adapter exists. It keeps public product media separate from authorized private artwork and is covered by published-host and recovery checks. A chosen object provider still needs its authenticated adapter and access/restore acceptance; setting `Uploads.Provider=S3` fails startup rather than silently writing to a fallback.
6. `ReleaseControls.AcceptNewPayments=false` pauses checkout/session and same-order retry; `AcceptUploads=false` pauses multipart API writes; `AllowAdminChanges=false` pauses admin writes. Existing secure callbacks remain verified, reads/auth remain available. These switches are not a global database write freeze: customer profile/cart and maintenance workers may still write. For schema/backup windows use the explicit quiescence plan below. Environment changes require a service restart; private reloadable JSON can update options.
7. Session refresh has its own bounded 60/IP/minute default; password-sensitive operations retain 10/IP/minute and account lockout. Tune capacity for the actual shared-IP topology without disabling protection. Do not log raw cookies/tokens or partition telemetry by token.

Render all placeholders outside tracked files. Review same-origin payment redirects and CSP requirements against the authenticated provider contract. Include the shared security headers in every nginx location that declares `add_header`, including hashed assets and HTML, to avoid nginx inheritance gaps. gzip, immutable hashed-asset caching, no-cache HTML, SPA fallback, no SPA API fallback, HTTPS redirect, body ceiling, private path denial and forwarded headers are provided. Preserve old hashed assets while open sessions drain.

On the **approved host**, run `nginx -t` and `systemd-analyze verify <rendered-unit>`, inspect file permissions/listeners, then perform the separately authorized installation/reload. These host checks have not run here; neither nginx nor a production host/certificate is supplied. Never treat loopback self-signed rehearsal TLS as real hostname/chain/renewal verification.

## Database cutover and financial reconciliation

The application does not auto-migrate on startup. Identify the exact production server/database, current migration, compatible old/new artifacts, migration identity, maintenance owner and recovery window privately. The 16 historical migrations are unchanged by this work. Never use a default development connection as a cutover target.

1. Reverify package integrity and the approved DB identity. Capture UTC start time, release SHA, schema IDs, SQL recovery model and job state. Run [aggregate reconciliation](../../deploy/reconcile-database.sql) using `sqlcmd -b -d <exact-database> -v EXPECTED_DATABASE="<same-exact-database>" -i deploy/reconcile-database.sql`; supply server/auth through the private runner environment, never a password in arguments. Store output privately. It verifies DB identity, lists all table counts/migrations and groups financial totals by currency/status/provider/mode. It also checks stock/ledger, historical snapshot JSON, proof references and outbox backlog.
2. Quiesce mutations with the approved proxy/service plan. Pause new payments, uploads and admin changes. Drain API work; stop maintenance workers where required. For a migration incompatible with ongoing callbacks, return a retryable unavailable response without acknowledging dropped events, or use an approved durable store retaining exact signed bytes. Coordinate provider retry duration. Do not blanket return 200 or erase callback evidence. Record the event window.
3. Render [encrypted backup SQL](../../deploy/backup-database.sql.template) from approved identifiers and server-side private paths. `sqlcmd -b` must succeed. Each run chooses a unique timestamp/GUID filename, takes COPY_ONLY/CHECKSUM/AES256 backup and verifies that exact set. Protect/export its SQL encryption certificate/private key separately. Preserve the full/log backup chain when required by the approved recovery model; COPY_ONLY alone is not point-in-time recovery.
4. Perform an actual restore into a **new isolated database**, never `REPLACE` the business DB. Verify key availability, CHECKSUM, all migrations, row counts, currency/provider totals, immutable custom/order/shipping snapshots, stock/reservations, proof references and public/private file versions/hashes. Capture measured RPO/RTO and compare with approved targets. `VERIFYONLY` alone is insufficient. Do not use production data in unrestricted staging.
5. Review the package `migrations.sql` for exact starting schema, transaction behavior, destructive operations and supported API versions. Test it on the isolated restored copy before authorizing execution. Run `sqlcmd -b` against the explicit approved target using the dedicated migration identity; any nonzero exit stops deployment. Do not start the new API or admit new traffic until expected history is verified.
6. Re-run aggregate reconciliation before resuming writes. Compare every table, orders/payments/refunds **per currency and provider/mode**, item prices, immutable snapshots, media and stock. Never add CHF and LKR or relabel legacy rows. Provider attempt amounts are not settled revenue. Account for approved normalization of blank legacy review/movement values. Investigate every unexplained difference. Run before/after while writes are quiescent; otherwise record legitimate auth/outbox/callback deltas explicitly.
7. Start only schema-compatible API/worker instances, verify readiness, then switch static frontend traffic. Reconcile callback retries and queued notifications idempotently. Record release/operator/timestamps privately. Keep the prior compatible artifact and backup through acceptance.

Automated SQL evidence covers fresh schema, Phase6 upgrade, CHECKSUM backup/VERIFYONLY/restore, row/total/stock/custom proof/private-byte reconciliation, and a deliberate transactional final-migration failure. The failure occurs before history insertion after normalization work: SQL rolls back both data changes, the migration remains pending, and retrying forward preserves LKR/CHF totals, historical Datatrans/Zahls attempts and immutable snapshots. No destructive Down migration or financial-record deletion is used. A production-data copy/production restore has not been performed.

## Storage, email and provider acceptance

Follow the detailed [integration runbook](../runbooks/release-cutover.md) for public/private object inventory, owner/reference/hash manifests, isolated staging copy, reversible key mapping, deltas, CDN/private access, retention/versioning and restoration. Existing local adapters and backup fixtures exercise these semantics; cloud ACLs, lifecycle rules and off-host copies require the chosen provider. Never rewrite immutable design specifications to simplify migration. Unknown/unsupported storage fails explicitly.

SMTP acceptance requires a real permitted sender/recipient, HTTPS confirmation/reset links, order/payment/proof/shipment templates, SPF/DKIM/DMARC, bounce monitoring, outage/backoff and outbox replay review. No production message was sent here. Captured test mail is not real-delivery acceptance.

Zahls TEST requires actual instance/API/signing configuration and authenticated contract confirmation. Record gateway creation, success/decline/cancel, signed and duplicate callbacks, reconciliation, same-order retry, full/partial refund and ambiguous outcomes against actual provider evidence. None is claimed as genuine acceptance without credentials. Historical Datatrans routing remains provider-qualified. LIVE additionally needs KYC/PSP/CHF methods, distinct secrets, exact public webhook, authenticated contract, the reviewed LIVE activation code, owner shipping/tax/capture/proof/refund policies and explicit launch authorization. Keep `Enabled=false` meanwhile; TEST cannot masquerade as LIVE. No real charge/refund or provider load test is automatic.

Shipping has zones/methods, CHF rates, limits/thresholds, ETA and address requirements in existing operations, but no approved rates were supplied. Keep production rates unavailable. Do not create Swiss VAT, authorization/capture semantics, returns or proof policy from assumptions. The approval checklist remains false until genuine business evidence exists.

## Smoke, staging and rollback

The published rehearsal verifies checksums **before SQL access**, creates only a newly named local `AmaaraPhase6Tests_*` DB, applies migrations, executes the operator reconciliation script and seeds fictitious accounts/catalog/CMS/artwork. It starts the actual packaged `be.dll` in **Production** with a fresh runtime root and proxies the packaged Vite files over HTTPS with a disposable in-memory loopback certificate. It checks public image cold-start, private owner/staff/other-user behavior, real JWT login/secure cookie reload, account/cart/orders and admin reads, unavailable writes, and zero created orders/payments. No acceptance fixture routes, fake auth handler or fake payment provider are deployed. Browser network access is restricted to the local origin.

For actual staging, provision isolated DB/media/secrets, TEST-only integrations and private access controls; use the production topology with [staging access restrictions](../../deploy/staging-access.conf.template), `X-Robots-Tag` and disabled public indexing. Review a narrow genuine signed-webhook exception if basic authentication would block provider callbacks. Re-run the full release smoke and real integration acceptance. No external staging host is identified; local rehearsal does not claim staging deployment.

For authorized live **read-only** probes:

```powershell
python scripts/smoke-release.py --api-origin https://APPROVED_API_HOST --site-origin https://APPROVED_APP_HOST --product-id APPROVED_ID --content-slug APPROVED_SLUG
# Optional --authenticated / --admin uses a privately injected short-lived AMAARA_SMOKE_TOKEN.
```

The script validates TLS normally, never follows redirects with privileged tokens and performs GET only. Login, checkout quote, storage writes, email, callbacks and financial smoke require the separate authorized acceptance matrix. Record UTC, SHA, environment and safe evidence for every PASS/FAIL/BLOCKED. Do not delete a real smoke order/payment afterward.

Rollback starts by disabling new financial activity, recording incident/event windows and choosing the last **schema-compatible** artifact. Reverify its hashes, switch frontend and API versioned paths under the approved drain plan, preserve runtime media/key mounts and check readiness. A source/alias switch is not a SQL rollback. If compatibility is uncertain, stop writes and prefer a reviewed forward fix. Restore into a new DB only after incident-owner approval and reconciliation of every post-backup settlement/refund/stock/outbox/media delta. Preserve signed callbacks, idempotency keys and sent/uncertain outbox evidence. Never erase orders, attempt history, refunds, audits or queued notifications. See the full [rollback procedure](../runbooks/release-cutover.md#rollback--forward-fix).

## Monitoring, backups and first day

Export existing `Amaara.Api` HTTP duration/error meters, sanitized route/correlation JSON logs, process/SQL readiness and authorized aggregate operational-health data through the approved collector. No external destination/recipient is configured. Before launch, name an incident lead, payment/reconciliation operator, production/stock operator and on-call backup; verify a real test alert and escalation. Never put customer values, cookies, URLs with secret queries, artwork or raw provider bodies in telemetry.

| Alert definition | Trigger/action before traffic is approved |
| --- | --- |
| Availability/readiness/DB | Sustained probe failure over approved window; isolate SQL/connectivity/migration cause, stop new checkout |
| 5xx and latency | Approved error-rate/p95 window by route/release; inspect safe correlation evidence |
| Webhook/reconciliation | Any confirmed provider/amount/currency/mode mismatch is a payment incident; review retry-failure threshold and pending-age policy |
| Pending orders/production delay | Approved payment expiry and fulfillment-age/count thresholds; do not auto-release uncertain paid stock |
| Inventory | Any negative balance or ledger mismatch is an incident; approved per-SKU low-stock thresholds |
| Outbox/email | Exhausted attempts > 0 or approved oldest-unsent age; investigate before replaying uncertain deliveries |
| Storage | Missing referenced asset/hash mismatch is an incident; approved storage error-rate/window |
| Security | Approved repeated admin login/lockout threshold; inspect proxy/IP and account events without logging credentials |
| Backup | Failed job/verification/off-host copy or approved maximum successful-backup age; verify certificate/key access and restore drill |

The [backup service](../../deploy/amaara-backup.service.template) and [timer](../../deploy/amaara-backup.timer.template) remain inactive until private destination, service account, server-side SQL path, certificate, schedule, retention, RPO/RTO and alerting are approved. SQL Server itself needs write access to the backup destination; systemd access alone is insufficient. Schedule private-media/object versions and key-ring backups together with the DB reference window, encrypted off-host copies and separate key escrow. Apply approved lifecycle cleanup only after restore verification; never delete originals speculatively. Run a recurring isolated restore drill, including DB, public/private media and authentication keys. A failed backup must alert; a timer without off-host/restore evidence is not production protection.

| Time from authorized traffic switch | Required review and owner |
| --- | --- |
| T+0, before traffic | Release operator verifies SHA/checksums/schema, TLS/redirects, readiness, private access, probes, disabled/enabled controls and rollback artifact |
| T+5 minutes | On-call verifies 5xx/latency/DB, frontend errors, signed webhook delivery, outbox and storage errors |
| T+15 and T+30 minutes | Payment operator reconciles provider/mode/currency totals and exceptions; production operator checks pending orders, proof queue and stock/ledger |
| T+1 hour, then hourly through T+8 | On-call checks all alert classes; payment/production owners review aged work and audit/security events; record UTC evidence |
| First scheduled backup and off-host copy | Backup operator verifies job/checksum/copy/key access and latest successful age; escalate missing evidence immediately |
| T+12 and T+24 hours | Incident lead reviews errors, payment/refund exceptions, order/production/shipment pipeline, inventory, email/outbox, media, security and backup evidence; record accept/hold decision |

Thresholds/contacts remain explicit external inputs, not fabricated SLAs. A confirmed financial/privacy inconsistency triggers the incident plan immediately, regardless of this schedule.

## Release checklist

Copy this checklist into private release evidence and record actual observations; the tracked checklist describes work and does not approve it.

- [ ] Exact clean develop SHA and green SQL/browser CI; no known internal P0/P1 defect; secret/dependency scans pass.
- [ ] Artifact manifest, lockfile/migration IDs and transfer SHA-256 verified; previous compatible artifact retained.
- [ ] Exact DB/host/domain/real TLS/canonical routes/trusted proxy approved; rendered nginx/systemd checks pass on that host.
- [ ] Runtime secrets, JWT/session keys, public/private mounts, file ACLs, email and storage adapters accepted.
- [ ] Quiescence/callback/worker plan, encrypted verified backup, separate key escrow, real isolated restore and RPO/RTO accepted.
- [ ] Exact migration boundary reviewed/rehearsed; pre/post rows, currency/provider money, stock, snapshots, references and outbox reconciled.
- [ ] Genuine Zahls TEST contract/scenarios complete; LIVE activation code/contract/KYC/PSP/CHF/webhook/refund evidence accepted; historical routing preserved.
- [ ] Owner shipping/tax/capture/proof/refund/return policies and values approved; no unconfigured fallback masquerades as available checkout.
- [ ] Staging and public/auth/admin/commerce/email/storage/payment smoke accepted for the identified environment.
- [ ] Monitoring destinations, actual alert delivery, named on-call/escalation and first-day schedule accepted.
- [ ] Database/media/key backup schedule, retention/off-host copy, latest successful job and restore drill accepted.
- [ ] Rollback/forward-fix compatibility and incident criteria accepted; all 25 gate approvals supported by genuine evidence.
- [ ] Explicit owner launch authorization; only then perform approved cutover, post-deploy validation and first-day reviews.

Current result: preparation can be verified locally; **actual production cutover remains blocked**. No DNS, certificate issuance, external deployment, business DB migration, real provider call, production message or live backup schedule has been executed.
