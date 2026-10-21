# Boutique redesign — implementation and release report

Date: 22 October 2026 (Asia/Colombo). Starting commit `8c7bf54`, clean `develop`; fetched `origin/develop` matched. Working branch `feature/boutique-redesign-security`. Stack, existing history, business data and migrations preserved. No external deployment, secret rotation, real payment or live-data change performed.

## Implementation map

| Phase | Result | Evidence / remaining scope |
| --- | --- | --- |
| 0 discovery | Implemented | Markdown + all 18 PDF pages read, branch/remotes/baseline verified, reference access recorded, local before evidence, threat/design/IA docs |
| 1 security | Implemented local controls; administrative rollout closed | MFA policies, stamp/family-bound JWTs, origin checks, password policy/toggles, no shop staff imports. Replay-safe MFA enrollment/recovery and broader upload processing remain release gaps |
| 2 foundation/shell | Implemented | Storefront theme/tokens, shared password control, nav/account/footer/help, focus and reduced motion; genuine business content remains owner supplied |
| 3 storefront | Implemented local redesign | Split hero, genuine-data sections, separate empty-inventory/no-match states, published-count contract, existing product journey preserved. Real photos/catalogue and unsupported variant purchase remain limitations |
| 4 custom designer | Implemented core corrections | Shape/ratio helper, quantity grammar, server quote, private artwork reload and visible session draft. Advanced production editor/export and new quote workflows are not claimed |
| 5 commerce | Existing validated workflows preserved | Configured server checkout/payment/refund boundaries; no new unapproved Sri Lankan gateway, guest path, COD or transfer workflow |
| 6 account | Implemented scoped improvements | Initial-based avatar, readable nav, dirty/unload state, security form/toggles and real logout revocation. Email change, customer MFA, privacy tools/crop are still product work |
| 7 staff | Separate deployable implemented | Existing operational modules preserved, backend capability/MFA gate and noindex. Production host/gateway/provisioning remain disabled/unapproved |
| 8 hardening | Local verification complete; release not approved | Actual results below; independent/manual/provider/production gates remain |

## Observed checks

| Check | Observation |
| --- | --- |
| Baseline .NET | 241 passed, 11 skipped, 0 failed |
| Baseline frontend | 111 tests passed in 21 files |
| Current .NET without SQL | 257 passed, 11 skipped, 0 failed (268 total) |
| Current .NET with real SQL | **268 passed, 0 skipped, 0 failed**, 58 seconds. A uniquely named new local `AmaaraPhase6Tests_Redesign_20261022_*` database was used via Windows authentication; existing development/production databases were not migrated |
| Frontend unit tests | 121 passed in 22 files under Node 22.23.4 after geometry cases added |
| ESLint / TypeScript | Both passed on the checked source; staff output is excluded as generated build output |
| Production storefront build | Passed; 215 transformed modules in the recorded run |
| Separate staff build | Passed; 237 transformed modules in the recorded run |
| Customer artifact isolation | Passed: 33 manifest entries, 34 emitted assets; no staff-only modules/clients or source maps found by checker |
| Bundle budgets | Initial JS 115,152 gzip bytes / 128,000 budget; largest route 5,185 / 40,000 gzip bytes; total CSS 74,705 / 100,000 bytes in recorded build |
| Secret scan | Passed: 796 text files, zero candidates at recorded run; ignored private config excluded |
| npm audit | Zero vulnerabilities from current feed |
| NuGet audit | No vulnerable packages reported by current configured feed |
| EF model | No pending model changes; no migration added or applied to an existing database |
| Focused browser checks | Six cases individually passed: public axe, all password controls, empty catalogue distinction, shape/private-art reload, absent shop staff navigation, separate staff builder |
| Full browser / production browser | 74 development cases passed under Node 22.23.4 (6.7 minutes); 3 production-build cases passed (24.8 seconds), followed by a final expanded 9-case production run (58.1 seconds) covering 320px accounts, public axe and password/designer/isolation journeys. An earlier comprehensive Node 20 run passed 77 cases; the final configuration correctly reserves its 3 lab cases for production mode |
| Lighthouse mobile lab | Three production-build runs: performance 98/95/96 (median 96), accessibility 100, Best Practices 96, SEO 100; median LCP 2.533s, CLS 0, TBT 104ms. See exceptions below |
| Published package/rehearsal | Seven-step package build and checksum/privacy verification passed (105 files). The Production API passed real-SQL, HTTPS loopback, MFA-positive/negative, private-file, paused-payment and browser-isolation checks. This was an explicitly labeled dirty local rehearsal, not a deployable production approval |
| Dependency inventory | Frontend CycloneDX SBOM generated locally from the unchanged lockfile; current npm/NuGet vulnerability feeds reported no project vulnerabilities |
| Other regression checks | 3 webhook proxy tests and 8 release-tool tests passed; final JWT-type restriction additionally passed all 15 focused authentication/security tests |
| External reference browsers | All 27 reference/viewport attempts blocked by sandbox network policy; several web-reader pages retrieved |
| Manual screen reader / genuine gateway / field vitals | Not performed; no certification or production payment claim |

The first focused browser run found a low-contrast custom CTA and staff route nesting regression. Both were corrected before the passing focused run. Two new test locators were made unambiguous. Windows sandbox process teardown left a test runner alive after cases completed; only the verified owned process tree was terminated, then full verification was rerun with normal process permissions. Interrupted runs are not counted as completed-suite passes.

## Evidence and approvals

Source paths and repro details: CURRENT_UX_AUDIT.md. Screenshots: `evidence/before` and `evidence/after`; they use disposable acceptance content, not live customer information. 39 responsive PNG captures cover baseline and redesigned routes; the added journey captures include product, profile, checkout, help and the separate staff app. Raw test logs/TRX remain ignored under `.runtime` and test output directories. This report records concise reproducible results rather than committing synthetic account credentials or full logs.

Release gates: owner-approved real imagery/catalogue/prices/contact/policies; production-ready media decoding/metadata/scanning; replay-safe staff MFA enrollment/recovery and protected gateway; genuine configured payment/email sandbox; privacy/export/deletion scope; manual accessibility; exact production header/CORS/cookie/backup custody and explicit deployment approval. See OWNER_DECISIONS.md, PAGE_ACCEPTANCE_CRITERIA.md and ADMIN_ISOLATION_DEPLOYMENT.md. The full master-prompt definition of production readiness is **not** declared achieved.


## Performance evidence and exceptions

The first exploratory Lighthouse probe used an unapproved preview origin and hit CORS rejection; it is not a valid baseline or release sample. The three retained runs use the configured storefront origin and font-optimized production artifact. A render-blocking external font stylesheet was replaced with two local licensed subsets (48,256 + 8,000 bytes), retained OFL licenses, font-display swap and a preload for the body font. No business imagery was invented to improve a benchmark.

The 90 performance / 100 accessibility / 95 SEO lab floors were met. Best Practices remains **96**, due to expected anonymous refresh (401) and optional unpublished home CMS (404) responses appearing as browser network errors. Median LCP **2.533s** narrowly misses the 2.5s target; it is not rounded into a pass. CLS is 0. INP and p75 field vitals were not measured. Automated accessibility scores do not establish WCAG conformance or replace screen-reader review. The small fixture images are not representative of future owner photographs. Full Lighthouse traces are local; the committed summary contains only scores and timings.

## Reviewable changes

- `c7e7cc6`: backend staff MFA/policies, session binding and revocation, password rules, private-file policy checks, circular-dimension validation, published-count metadata and regression fixtures/tests. No schema migration.
- `6fecdc6`: separate staff build, boutique storefront, password controls, help/account/empty states, preview/draft/private-art restoration, build isolation and browser project updates.
- `2d94c2e`: JWT token-type restriction, self-hosted font CSP boundary and published-release MFA/isolation rehearsal.
- `b06fa6f`: local licensed font delivery, 320px account coverage, responsive footer tracks and clearer designer copy. Documentation/evidence accompany the same feature branch.

No production deployment or merge is included. The requested work is substantial local implementation with documented incomplete production/editor/privacy functionality, not a declaration that every master-prompt acceptance outcome is finished.

The first package attempt overlapped test-server shutdown and hit a Windows esbuild file lock. The failed artifact was retained separately; after all owned servers stopped, the new package and published rehearsal passed. No failed or incomplete artifact was deployed.
