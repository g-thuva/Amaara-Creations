# Zahls verification report

Verified on 27 October 2026. **Local implementation and automated contracts are
verified; a genuine external Zahls payment has not been completed.**

## Actual results

| Check | Result |
|---|---|
| Full backend suite with disposable SQL Server enabled | **433 passed, 0 failed, 0 skipped**. Final TRX: `.runtime/zahls-results/backend-final.trx`. |
| Retained CHF/LKR SQL lifecycle | Passed; final focused rerun also passed (16.35 seconds). Real SQL persistence, authoritative total, ownership, duplicate checkout/event, pending/decline/cancel/retry, reconciliation, stock and partial/full/failed refunds use a test-only gateway. |
| Frontend unit suite | **271 passed in 34 files**. Removed simulator-only tests are not counted. |
| Selected Chrome regressions | **34 unique cases verified**: 30 passed initially; four stale copy assertions were corrected and all four reruns passed. Customer Identity sign-in/logout/restoration/races, no synthetic login, staff MFA/permissions, checkout, payment results and refund UI. |
| Live normal storefront smoke check | Real email/password form, normal API URL, zero simulator requests and zero uncaught browser script errors after restarting cached Vite. Screenshots in `.runtime/zahls-results/normal-login.png` and `normal-catalogue.png`. |
| Business catalogue media smoke | Normal catalogue rendered its empty state (0 products), so no business product-image assertion was possible. Media configuration was preserved; do not treat this as proof of every S3 object/artwork URL. |
| Frontend lint and TypeScript | Passed. |
| Storefront and staff production builds | Passed. Storefront isolation: 38 manifest entries / 39 assets. |
| API Release publish | Passed to `.runtime/zahls-release`; no deployment performed. |
| Release artifact checks | Passed for API, storefront and staff bundles. No private local configuration or test/simulator executable included. |
| Release-tool unit suite | **16 passed**. |
| Webhook proxy tests | **4 passed**: exact raw bytes/signature, route/method restrictions, unsigned/non-JSON/oversized rejection, explicit legacy provider boundary. |
| Public Cloudflare ingress | Real HTTPS tunnel: Swagger **404**, unsigned callback **401**, correctly shaped invalid HMAC **401**. Proxy and tunnel stopped afterward. Evidence: `.runtime/zahls-results/public-webhook-checks.json`. |
| Normal API | Restarted verified repository process on `http://localhost:5192`; `/health` **200**. Normal storefront restarted on 5173 to clear the previous in-memory Vite simulator plugin. |
| Normal authentication CORS | Sign-in preflight **204** with exact `http://localhost:5173` allow-origin and credentials enabled; anonymous refresh **401** retains the allow-origin header. |
| Actual Zahls authentication | Real `v1.16/SignatureCheck/` and `PaymentProvider/` GETs **200**, successful envelopes. Existing private API key accepted. |
| Actual provider methods | PayPal listed; **0 methods / 0 active methods**. Read-only script reproduced this result. |
| NuGet production dependency audit | `be` reported no vulnerable packages in current sources. Initial test restores emitted NU1900 advisory-network warnings; a separate network-enabled audit and Release restore succeeded. |
| npm production dependency audit | `npm audit --omit=dev --audit-level=moderate`: **0 vulnerabilities**. This does not claim a development-dependency audit. |
| Credential/artifact hygiene | Existing Zahls keys preserved privately; shared JSON sanitized. Staged real-merchant credential comparison passed. No card data introduced. |
| Historical records | No production migration, business payment rewrite or historical simulator database deletion. Tests create their own isolated catalogs. |

Browser suites intentionally mix real Identity/API acceptance tests and mocked
payment transport UI contracts. SQL lifecycle tests use deterministic provider
doubles. These establish local behavior and security boundaries, **not** a real
Zahls-hosted payment, real signed merchant delivery or genuine refund.

## Reproduce

From `C:\Nexaura`, with Node 22+ and local SQL Server available:

```powershell
$env:AMAARA_TEST_SQL_CONNECTION = 'Server=localhost;Database=AmaaraPhase6Tests_Zahls_' + [Guid]::NewGuid().ToString('N') + ';Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test tests/be.Tests/be.Tests.csproj --configuration Debug --artifacts-path .runtime/zahls-verification-build --logger 'trx;LogFileName=backend.trx' --results-directory .runtime/zahls-results
npm.cmd --prefix client test
npm.cmd --prefix client run lint
npm.cmd --prefix client run typecheck
npm.cmd --prefix client run build
npm.cmd --prefix client run build:admin
npm.cmd --prefix client run check:isolation
node --test tests/webhook-tunnel-proxy.test.mjs
python -m unittest discover -s tests/release_tools
dotnet publish be/be.csproj --configuration Release --artifacts-path .runtime/zahls-publish-build --output .runtime/zahls-release
python scripts/check-release-artifacts.py .runtime/zahls-release
python scripts/check-release-artifacts.py client/dist
python scripts/check-release-artifacts.py client/dist-admin
dotnet list be/be.csproj package --vulnerable --include-transitive
npm.cmd --prefix client audit --omit=dev --audit-level=moderate
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-zahls-test-readiness.ps1
```

The selected browser command below uses the checked-in Playwright configuration
and real test harness. Its ports are 5178/5179/5198; free those **test** ports
first. The verification run used an ignored `.runtime/zahls.playwright.config.ts`
that changes only the acceptance-host build location to an isolated artifact
directory, avoiding locked binaries from running services.

```powershell
Set-Location C:\Nexaura\client
npm.cmd run test:e2e -- account/normal-auth.spec.ts account/auth-session.spec.ts account/authentication-races.spec.ts account/staff-login-redirect.spec.ts checkout/commerce.spec.ts checkout/checkout.spec.ts admin/checkout-payments.spec.ts admin/payments.spec.ts admin/authentication.spec.ts
```

## Files changed

Payment changes: `be/Infrastructure/Payments/Zahls/ZahlsPaymentGateway.cs`,
`ZahlsOptions.cs`, `be/Application/Features/Checkout/CheckoutPricingService.cs`,
`be/appsettings.json`, `be/appsettings.Development.Local.example.json`,
`tests/be.Tests/Unit/Payments/ZahlsAdapterTests.cs`,
`tests/webhook-tunnel-proxy.mjs`, its test, and
`scripts/check-zahls-test-readiness.ps1`.

Runtime removal/auth restoration: `client/vite.config.js`, `client/package.json`,
`CheckoutForm.jsx`, ordinary auth/checkout browser regressions, new
`client/e2e/account/normal-auth.spec.ts`, backend retired-route/database guard
tests, `ProductionSettings.cs` and its test, release-artifact checks, test-project
embedded-resource removal, and the moved SQL fixtures/tests. Earlier legitimate
LKR SQL fixture corrections were retained. Ignored `client/.env.development.local`
was selectively restored; private User Secrets were updated outside Git.

Deleted runtime paths: `client/dev/*`, `client/e2e-simulator/payments.spec.ts`,
`client/playwright.simulator.config.ts`, simulator-only frontend unit tests,
`tests/be.PaymentSimulator/*`, `tests/be.Tests/Simulation/SimulatorServer.cs`,
`tests/be.Tests/Simulation/Ui/*`, and `scripts/*payment-simulator.ps1`.
Useful `Simulation` fixture code was moved to
`tests/be.Tests/Fixtures/Payments`; `LocalPaymentSimulatorSqlTests.cs` became
`PaymentWorkflowSqlTests.cs`. Old activation guides were removed, with a
retirement notice in `LOCAL_PAYMENT_SIMULATOR.md`.

Documentation: this report, `ZAHLS_INTEGRATION_AUDIT.md`, `ZAHLS_TEST_SETUP.md`,
`ZAHLS_WEBHOOK_SECURITY.md`, `LOCAL_PAYMENT_SIMULATOR.md`, `CHF_SETUP.md` and README.

## External blockers and remaining acceptance

1. Zahls currently exposes no active PayPal method for this merchant. Resolve
   eligibility/activation in Zahls; listing PayPal alone is insufficient.
2. Genuine provider TEST/sandbox mode and CHF acceptance remain unverified.
3. The merchant webhook URL, event format/version, signing-key correspondence
   and real delivery/retry behavior must be configured and verified in Zahls.
4. Obtain the provider's approved TEST payment details and verify actual Gateway
   POST, returned hosted-link shape and transaction responses.
5. Complete real success/decline/cancel/pending, replay a genuine notification,
   verify SQL stock/order state once, and test supported refunds with staff MFA.

The payment button therefore remains safely unavailable under current merchant
readiness. Removing the unnecessary `pm` requirement cannot manufacture an active
method. No readiness flag was set true to bypass these blockers, no fake Zahls
response was used in normal runtime, and LIVE remains disabled. Follow
[local setup](ZAHLS_TEST_SETUP.md) for the exact next steps and commands.
