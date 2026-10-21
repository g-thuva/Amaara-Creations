# Amaara boutique redesign — implementation and release evidence

22 October 2026, Asia/Colombo. Branch `feature/boutique-redesign-security`, starting from clean `develop` at `8c7bf54`. [Draft PR](https://github.com/g-thuva/Amaara-Creations/pull/1). React/Vite, ASP.NET Core 8 Identity, EF Core and SQL Server are preserved. No production deployment, merge, real payment, external secret rotation or existing business-database migration was performed.

## Delivered scope

| Phase | Result | Implementation and evidence |
| --- | --- | --- |
| Discovery | Complete locally | Attached Markdown and all 18 PDF pages reviewed; baseline, threat model, route/permission audit, design direction and responsive before evidence recorded |
| Security | Implemented | Recent-MFA staff policies, session/stamp-bound JWT validation, immediate revocation, authentication origin protection, passphrases, optional offline compromised-password corpus, object authorization and isolated customer/staff artifacts |
| Foundation and storefront | Implemented | Ivory/navy editorial shell, local licensed fonts, accessible password controls, genuine-data product/search/filter/empty states, truthful help/contact/policy boundaries and responsive account navigation |
| Designer and media | Implemented | Correct shapes/proportions and server quotes; persisted text/artwork layout, crop/fit/position/rotation, transparent PNG layout export, low-resolution/substitution warnings, account-scoped draft/conflict recovery, private artwork restore and cancellable/retryable upload status |
| Commerce | Implemented | Selected variants persist as distinct cart lines with authoritative prices and transactional variant stock/reservation; cart drawer, protected artwork thumbnails, remove confirmation/restoration, existing validated checkout/payment/refund/proof workflows |
| Accounts | Implemented | Profile dirty protection and photo crop/retry, TOTP enrollment/replacement/replay protection, hashed one-use recovery codes, device/session revocation, reauthenticated export, verified email change and scoped quote/deletion-review requests |
| Staff | Implemented locally, rollout closed | Independent staff entry/build, capability gating before operational imports, existing modules plus permission-scoped quote/privacy queues, audit redaction and deny-only identity-gateway template |
| Hardening and release | Local checks passing; package verification in progress | Current observed checks below. Genuine provider, manual accessibility and protected production-host evidence remain explicit release gates |

The editor PNG is a layout preview at bounded output resolution. Physical bleed, cutpaths, exact production fonts/material finish and the final proof require production approval; the interface does not call a browser PNG a printer-ready file. Deletion requests request review and do not erase records or promise automatic erasure.

## Current verification

The final feature source is committed as `1f4c845`, following `bc5d7cd` (raster processing), `23ec2ca` (MFA/devices) and `54478a6` (editor/variants). Documentation-only updates may follow without changing this tested feature source.

| Check | Observed result |
| --- | --- |
| Backend, real SQL | **316 passed, 0 skipped, 0 failed**, 1m42s; a uniquely named new local `AmaaraPhase6Tests_Redesign_20261022_Verified_*` database, Windows authentication; no existing customer database used |
| Frontend unit | **125 passed in 23 files**, Vitest 4.1.12 on Node 22.23.4 |
| Lint / TypeScript | Passed |
| Focused browser regression | **19 passed**, artwork failure/ownership cases, editor restoration/export/conflict recovery, cart drawer/Escape focus/removal restoration and private thumbnails |
| Additional browser cases | Full customer lifecycle, profile crop/retry and cart checks passed together (4 cases); privacy export/email verification/request/reply checks passed (2 cases); upload cancellation/reconciliation/retry passed its focused case |
| Full development browser suite | Previous expanded run: 81 passed, 1 failed because a lifecycle assertion still expected variants to be unavailable. Assertion corrected for the implemented purchase path; full lifecycle passed afterward. A clean final aggregate run is pending; this is not represented as an 85-case pass |
| Production-build browser suite | **20 passed (4.3m)** on Node 22.23.4, including both built applications, axe, 320px accounts, private artwork, editor/export/conflicts, variants, MFA/devices, privacy/email, photo crop and upload cancellation |
| Secret scan | **852 text files, zero candidates**; ignored private configuration excluded |
| EF model | No pending model changes |
| Migration behavior | Both additive migrations exercised on disposable SQL: existing cart rows preserved; variant stock cannot oversell; rollback guards refuse to discard selected variants or customer requests |
| Release tools / webhook proxy | **10 Python tests and 3 Node tests passed** |
| Dependency audits | npm: **zero vulnerabilities**; NuGet: **zero known vulnerable packages** after retrying with feed access. Restricted attempts failed connectivity and are not counted as successful audits |
| Current artifacts | Both production builds passed; customer isolation passed (37 manifest entries, 38 assets, no staff modules/source maps). Initial JS **115,565 / 128,000** gzip bytes; largest route **8,321 / 40,000** gzip bytes; CSS **76,337 / 100,000** bytes |
| Production desktop lab | LCP **616ms**, CLS **0**, measured by the production browser case; fixture-based local evidence, not mobile Lighthouse or field performance |
| Current package rehearsal | Clean package/rehearsal pending |

The baseline was 241 backend passes plus 11 SQL skips and 111 frontend passes. Earlier feature revisions passed 268 real-SQL cases, 74 development browser cases and an expanded 9-case production run. Earlier hosted browser and SQL/API workflows at `1458849` passed, including a clean Linux package/HTTPS rehearsal. Those earlier passes do not substitute for the final source checks listed above.

Raw logs remain ignored under `.runtime`, including `master-prompt-backend-final.log`, `master-prompt-upload-cart-browser.log` and `master-prompt-production-final.log`. No credentials, enrollment secrets, recovery codes or full exports are committed as evidence. Windows sandbox teardown interrupted the focused upload runner after its case passed; the production run uses normal process permissions and fresh owned servers.

## Schema, configuration and operations

Two additive migrations are documented in [variant migration guidance](VARIANT_MIGRATION.md) and [account/request/media operations](ACCOUNT_REQUESTS_AND_MEDIA.md):

- `20261021225400_BoutiqueCartVariants`: nullable variant selection on existing cart records, restricted foreign key and option-aware uniqueness; rollback raises 51003 if variant cart lines exist.
- `20261021230822_BoutiqueCustomerRequests`: scoped quote/privacy-review records with restricted relationships and concurrency; rollback raises 51004 if any request exists.

Historical migrations are unchanged. The current model snapshot and reviewed compatibility fixture advance with the two additions. Existing financial/order data is preserved. Staging/production migration requires the backup, restore and approval process in the cutover runbook.

Email change, quote requests and privacy requests default off until their operational processes are approved. The optional HTTPS scanning adapter fails closed when configured/required and unavailable; no real antivirus service was contacted. The private offline password-corpus importer/checker is implemented and tested; selecting and refreshing an approved corpus remains operational work. No customer password or hash is sent to a third party. See the linked operations document for exact flags and adapter contracts.

Administration remains disabled by default. The separate `dist-admin` output has an `index.html` entry; it is not included in the public storefront package. The deny-only gateway example protects all staff HTML, deep links and assets until a real identity-aware adapter is configured. Static-asset confidentiality requires that protected host; a separate Vite bundle alone does not establish it.

## Visual and performance evidence

The existing 39 responsive before/after PNGs use disposable acceptance fixtures. They cover the original/redesigned shell and major routes, with product/profile/checkout/help/staff journey captures. They are local engineering evidence, not genuine customer work or final approved catalogue photography. Source screenshot attachments were not separately available; the supplied prompt/PDF informed the audit. All 27 reference-site browser/viewport attempts were blocked by sandbox network policy; the research record distinguishes retrieved web-reader observations from unavailable visual inspection.

The earlier font-optimized production revision had three Lighthouse mobile runs: performance **98/95/96** (median **96**), accessibility **100**, Best Practices **96**, SEO **100**. Median LCP **2.533s** narrowly missed 2.5s; CLS **0**, TBT **104ms**. [Retained summary](evidence/lighthouse-summary.json). These are prior-revision fixture measurements, not fresh final-source or field guarantees. Best Practices recorded anonymous refresh 401 and unpublished optional CMS 404 network responses. No response status or audit was disabled to improve a score. INP/p75 field vitals and manual screen-reader conformance were not measured.

## External release gates

[Owner decisions](OWNER_DECISIONS.md), [page acceptance](PAGE_ACCEPTANCE_CRITERIA.md), [security mapping](../security/ASVS_5_CONTROL_MAPPING.md) and [staff deployment](../security/ADMIN_ISOLATION_DEPLOYMENT.md) identify the remaining evidence:

- Genuine rights-cleared photographs, catalogue/options/prices, contacts, delivery rules and approved policy text.
- Approved Sri Lankan/LKR provider and genuine sandbox acceptance; actual transactional sender/delivery and security notification operations. Unconfigured methods stay disabled.
- Separate protected staff origin, approved identities/recovery, TLS/headers/CORS/cookies, private storage/scanner/corpus operation, encrypted backup/key custody and production restore evidence.
- Qualified retention/erasure policy and responsible request reviewers; manual screen readers/zoom and Sinhala/Tamil review, representative performance and field vitals.
- Explicit staging/production cutover approval and the reviewed release/rollback runbook.

The local implementation closes the earlier editor, MFA, media, variant and account-workflow gaps. Production definition of done remains dependent on the evidence above; no deployment or merge is implied.
