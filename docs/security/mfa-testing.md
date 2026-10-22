# Authentication and MFA verification

Verification uses .NET 8 Identity/JWT, React/Vite, SQL Server and Chrome. No real account credentials, phone enrollment, owner recovery approval or production deployment were used.

## Observed results

| Check | Result |
| --- | --- |
| Full backend suite with local SQL test connection | 367 passed, zero failures or skips |
| Frontend unit suite | 181 passed in 26 files |
| Development browser coverage | 101 distinct scenarios passed across the full run and focused corrective reruns |
| Production-build browser gate | All 36 passed, including guided MFA and owner recovery approval |
| ESLint and TypeScript | Passed |
| EF model consistency | No pending model changes; no new migration |
| Release/configuration Python tests | 15 passed |
| Storefront and management production builds | Passed |
| Storefront artifact isolation | Passed: 38 manifest entries / 39 assets, no operational modules or source maps |
| Performance budgets | Initial JS 116,262 / 128,000 gzip bytes; largest route 8,322 / 40,000; CSS 78,767 / 100,000 bytes |
| Dependency checks | npm runtime audit: zero vulnerabilities; NuGet: no vulnerable packages from configured feeds |
| Source secret scan | 898 text files passed, zero candidates; no candidate values printed or committed |
| Actual local anonymous Chrome smoke | Passed: shared login, management redirect, API liveness/Development, validation 400, no pending access 401, storefront staff exchange 403, zero page errors |

The broad development browser run exercised 100 cases. Existing tests assumed missing MFA returned 401, a second management login form, or staff navigation to `/profile`; those expectations were replaced with pending-transaction/API-denial and shared-flow assertions. Browser checks also identified and fixed slow anonymous restoration blocking sign-in, recovery input mode persisting into enrollment, sidebar contrast and sign-out navigation ordering. Updated operational cases verify inventory, catalog, support, marketing, production, reports/audit, proof approval, refunds and role denials through real endpoints.

Development accounting: 77 broad-run cases plus 21 corrective cases plus the final two regressions cover the original 100; the new owner-recovery case adds one. Focused guided reruns also cover the corrected keyboard assertions and expanded accessibility checks. Production's complete selected gate reported 36 passes in one run. Private logs are under `.runtime/guided-mfa-*`.

## Security and user-experience evidence

| Area | Evidence |
| --- | --- |
| Password-only customers, registration/reset, refresh/logout | `SingleSignInTests`, existing Identity/customer lifecycle tests, `guided-mfa.spec.ts`, `auth-session.spec.ts` |
| Pending cookies are not JWT/session grants; no dashboard/API access | `GuidedSignInTests`, `ManagementAuthenticationTests`, guided enrollment/verification browser tests |
| Unknown roles, unconfirmed/locked accounts, demotion, changed stamps/passwords | Guided sign-in, management authorization and redesign security API tests |
| Stable QR, bad enrollment code, acknowledgment, native verification | Guided sign-in API/SQL tests and `guided-mfa.spec.ts` |
| Existing raw keys remain compatible; corrupt data fails closed | Guided key-format integration tests; legacy logical secret retained while ciphertext is written |
| SQL durability and multi-instance replay protection | `GuidedSignInSqlTests`: new API instances restore the same QR, protected key and final session using one persistent key ring; concurrent instances consume one challenge exactly once |
| One-use TOTP and recovery, lockout, cancellation, expiry | Guided, second-factor journey and SQL concurrency tests; copied pending cookie rejected after sign-out |
| Old limited/expired sessions cannot mint a staff or shopping JWT | Six guided refresh cases covering missing assurance, expired assurance and incomplete enrollment for both refresh paths |
| Verified lost-phone replacement and unchanged active key until confirmation | Guided recovery API/browser journeys; old sessions revoked and replacement codes invalidate the old set |
| Owner-only recovery approval, no self-approval, five-minute MFA, audit/notification | `StaffMfaRecoveryTests` and `admin/mfa-recovery.spec.ts` |
| Independent cookies/tokens, hostile origins and no operational module flash | Management API tests, authentication races, `origin-boundary.spec.ts`, dashboard handoff test |
| QR compatibility | Actual rendered QR pixels decoded locally with `jsQR`; issuer/account checked; independently generated RFC 6238 code verified by native Identity |
| Responsive/accessibility | Login, verification, enrollment and recovery tested at 360, 390, 768, 1024 and 1440 px; keyboard focus, password visibility, formatted paste, inline errors and automated WCAG 2.2 AA-tagged rules |
| Recovery usability | Download filename/content, copy and print actions, saved-code checkbox, no second password/code after enrollment |

Browser authentication and recovery journeys call real Identity services using disposable test accounts; successful MFA responses are not fabricated. The browser host uses an isolated in-memory Identity store, while backend integration additionally uses fresh name-guarded SQL databases. Existing payment-state UI mocks remain explicitly separate from authentication evidence. The HTTPS management test destination is represented by a redirect to the actual local management artifact; this is not a test of an externally deployed hostname.

## Reproducing checks

Use Node 22+, .NET 8 and the existing test frameworks:

```powershell
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release
dotnet ef migrations has-pending-model-changes --project be --configuration Release --no-build
npm --prefix client run lint
npm --prefix client run typecheck
npm --prefix client test
npm --prefix client run test:e2e
npm --prefix client run test:e2e:production
npm --prefix client run check:isolation
npm --prefix client run check:performance
python -m unittest discover -s tests/release_tools -p 'test_*.py'
python scripts/scan-secrets.py
```

Full SQL tests need a privately supplied `AMAARA_TEST_SQL_CONNECTION` targeting a disposable catalog whose name starts with `AmaaraPhase6Tests_`. Tests create/migrate/delete only guarded synthetic catalogs; do not point that variable at the business database. Without it, SQL-specific facts are skipped and that run cannot establish the full SQL result above. Tests never require production secrets or the owner's credentials. Keep generated Data Protection keys, logs, screenshots and downloads private under ignored runtime/test-output directories.

The browser harness owns ports 5178/5179/5198. Pause only an identified agent-owned management listener before those tests, then restore it. Leave a user's storefront/API process alone unless an explicitly necessary local restart has been authorized. Real local application ports are 5173/5179/5192.

## Remaining acceptance boundaries

A physical phone scanning Google Authenticator and genuine external production TLS/origin/cookie/key-ring hosting were not verified. Automated local QR decoding and native TOTP verification establish interoperability at the protocol level, not a real-device or deployed-environment claim. The official app links were checked against their primary store pages. Automated accessibility and keyboard checks are not a complete manual accessibility certification; clipboard/print tests use browser instrumentation and do not claim a physical printer was tested.

The owner's business database was left unchanged: no copying, migrations, role/password edits, MFA enrollment or recovery reset. Two pre-existing business migrations remain pending and are separate from this schema-free authentication change. Production still needs approved hosting, protected management assets, persistent encrypted Data Protection key storage, monitored clock synchronization, tested email delivery, operational recovery controls and explicit deployment approval. Code remains local.

After verification, the agent-owned Development API and management frontend were restored with the tested source; the existing storefront process was preserved. A final read-only check confirmed the configured identity remains email-confirmed, `Admin`/`SuperAdmin`, MFA not yet enrolled, administration enabled and bootstrap disabled. The owner can now perform their own first-time phone setup through shared sign-in. No real credentials were submitted by the agent.
