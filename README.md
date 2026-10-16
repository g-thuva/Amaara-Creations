# Amaara Creations

## Current Phase 7–9 continuation

The 16 October 2026 continuation authorizes internal Phase 7/8 engineering and non-secret Phase 9 preparation. Historical Phase 6 “not ready for Phase 7” statements below describe an earlier external acceptance gate and are superseded for engineering scope. Real Zahls acceptance and production cutover remain blocked.

Admin operations now include order/customer detail, paid-order/proof-gated production, inventory ledger/adjustments, provider-neutral payments/refunds, tracking, promotions, verified-purchase review moderation, staff policies, audit browsing and currency-separated reports/CSV. The release adds three Phase 7 migrations after Phase6ZahlsTestCurrency (16 total). Only disposable databases were migrated/rehearsed; the business database remains unchanged. JWT staff roles are checked against current assignments; financial/settings/staff/audit operations have distinct server policies.

See [the tracked release engineering record](RELEASE_READINESS.md) and [production preparation runbooks](RELEASE_RUNBOOKS.md), plus inactive templates under `deploy/`. These cover configuration keys, assumptions, migrations, exact validation evidence, backup/restore, deployment, DNS/TLS, payment/email/storage acceptance, smoke, rollback, monitoring and the explicit cutover checklist. Do not put keys in Vite configuration. Public canonical URLs require matching approved `PublicSite:BaseUrl` / `VITE_PUBLIC_SITE_URL`; clean BrowserRouter URLs require the documented reverse-proxy fallback. Use `VITE_ROUTER_MODE=hash` only for the historical static GitHub Pages preview.

Current verified results include 200 backend tests with SQL enabled (including seven SQL concurrency/migration/backup/load cases), 110 frontend tests, 70 browser acceptance tests, three production-build browser checks and three webhook ingress checks. The SQL-backed HTTP workload completed 720 requests without failures or provider calls. Exact evidence and the limits of local rehearsals are maintained in the release record. Node 22+ remains required. API publish excludes local settings, private media/key folders and development captures; the artifact privacy and source-secret checks are executable under `scripts/`.

Zahls TEST instance/API/signing key/methods and authenticated contract verification are unavailable. Genuine success/decline/cancel/retry/webhook/reconciliation/refund acceptance remains BLOCKED. Shipping/tax/capture/proof/refund policy and production database/hosting/domain/TLS/email/object-storage/monitoring/backup approvals remain required. `Zahls.Enabled=false`; the current payment adapter is TEST-only. No production deployment, financial transaction, DNS change or business-database migration is authorized or executed by these procedures.

Amaara Creations is a React/Vite frontend with an ASP.NET Core Web API backend using ASP.NET Core Identity, JWT authentication, EF Core, and SQL Server.

## Prerequisites

- Node.js 22 or newer (`.nvmrc` is provided)
- npm 10+
- .NET SDK 8.0.x (`global.json` pins the SDK line)
- SQL Server or SQL Server Express for full API/data workflows

## Frontend

The complete application uses the feature-oriented structure documented in [Frontend architecture](docs/frontend-architecture.md). Routes, guards and providers live in `client/src/app`; customer and admin workflows live in `client/src/features`; generic infrastructure/UI lives in `client/src/shared`. See [Admin architecture](docs/admin-architecture.md) for operational details and the [complete refactor result](docs/frontend-refactor-result.md) for current verification evidence.

```powershell
cd client
npm ci
copy .env.example .env
npm run dev
```

Frontend configuration:

- `VITE_API_BASE_URL` defaults to `http://localhost:5192/api/v1`
- Application APIs use canonical `/api/v1/*` routes. Legacy unversioned `/api/*` aliases are not exposed.
- `VITE_MEDIA_BASE_URL` defaults to `http://localhost:5192`

Quality checks:

```powershell
cd client
npm run lint
npm run typecheck
npm test
npm run build
npm audit --audit-level=high
```

## Backend

```powershell
cd be
dotnet restore
dotnet build
dotnet test ..\tests\be.Tests\be.Tests.csproj
dotnet ef migrations list
dotnet run --urls http://localhost:5192
```

`dotnet run` uses the `be` launch profile to select Development, which loads ASP.NET user secrets and the ignored `be/appsettings.Development.Local.json` file. Use user secrets for local private values:

```powershell
cd be
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AmaaraCreationsDB;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;"
dotnet user-secrets set "Jwt:Key" "replace-with-a-local-32-character-minimum-secret"
dotnet user-secrets set "AdminBootstrap:Enabled" "true"
dotnet user-secrets set "AdminBootstrap:Email" "admin@example.test"
dotnet user-secrets set "AdminBootstrap:Password" "replace-with-a-strong-local-password"
```

`AdminBootstrap` is disabled by default and never falls back to a hard-coded password.

`Encrypt=false` is a local-development compatibility setting for the verified default SQL Server instance. Production connection encryption must remain enabled and environment-managed.
If an older user-secret connection string omits `Encrypt`, update that local secret to match your SQL Server's TLS configuration. Windows integrated authentication also requires a working local Windows/SQL identity; an SSPI failure is a separate SQL login problem, not a missing application setting. Never paste a connection string or password into the issue report.

## Implementation Notes

See `docs/implementation/phase-00-stabilisation.md` for the Phase 0 stabilisation record, command results, current API/route inventory, security changes, and remaining manual infrastructure steps.

See `docs/implementation/foundation.md` for the Foundation foundation/data-model implementation record and migration notes.

See `docs/implementation/authentication-accounts.md` for the Authentication authentication, account, refresh-session, and saved-address implementation record.

See `docs/implementation/catalog-media-cms.md` for the Catalogue Media catalogue, media, CMS, settings, Swagger, admin UI, and migration record.

See `docs/implementation/storefront-ui-ux.md` for the Storefront storefront design system, customer routes, API integration, responsive/accessibility work, and verification record.

See `docs/implementation/brand-audit.md` for the Storefront Revision 2 brand-evidence status, adopted continuity decisions, and the owner references still required.

See `docs/implementation/custom-builder.md` for the server-backed custom sticker builder, versioned pricing/configuration, private artwork, cart/order snapshots, proof workflow, admin operations, and verification record.

See `docs/implementation/api-routing-v1-cleanup.md` for the canonical API v1 routing cleanup, removed legacy aliases, Swagger verification, and final endpoint list.

## Swagger / OpenAPI

Swagger is enabled in Development only.

```powershell
cd be
dotnet run --urls http://localhost:5192
```

- Swagger UI: `http://localhost:5192/swagger`
- Swagger direct page: `http://localhost:5192/swagger/index.html`
- OpenAPI JSON: `http://localhost:5192/swagger/v1/swagger.json`
- API base: `http://localhost:5192/api/v1`

Use the Swagger **Authorize** button with a JWT access token from `POST /api/v1/auth/login`. Enter the raw token value; Swagger applies the Bearer scheme.


## Phase 6 Zahls CHF TEST checkout (current integration)

New checkout payments use the Zahls hosted Gateway adapter behind the existing payment services. The integration is disabled until a private Zahls TEST instance, API key, webhook signing key, a configured TEST payment provider, confirmed CHF support, and merchant-verified payment-method identifiers are supplied. The business owner confirmed Swiss merchant registration; account verification and PSP eligibility still require confirmation in the real account. The previous Datatrans integration and payment records remain for historical verification.

The public Zahls site currently illustrates the `v1.16` Gateway endpoint. The full authenticated API reference was inaccessible during implementation; the adapter's version and API base are explicit configuration, and its request/response contract must be confirmed with the merchant's TEST instance before real acceptance. The [Gateway guide](https://www.zahls.ch/en/developers/guides/gateway/build) says to persist the returned link and Gateway ID and verify payment through webhook or API, never the browser return. The [webhook guide](https://www.zahls.ch/en/developers/guides/webhook) specifies `X-Webhook-Signature`: lowercase hex HMAC-SHA256 over exact raw body bytes with the UTF-8 signing key.

Merge the `Zahls` and `Checkout` sections of [the local example](be/appsettings.Development.Local.example.json) into ignored `be/appsettings.Development.Local.json`; never replace existing SQL, JWT, email, or other private settings. Keep `Enabled=false` until all prerequisites are verified. Set `Checkout:Currency=CHF` only with explicitly CHF-priced TEST products/custom-builder configuration in an isolated development database. Existing catalog prices are LKR and cannot be relabeled or converted automatically. Development-only CHF shipping fixtures can exercise checkout in an isolated database, but actual destinations, rates, thresholds, estimates and pickup rules still require owner approval. No business shipping values have been invented.

Configure Zahls `Mode=TEST`, `InstanceName`, `ApiKey`, `WebhookSigningKey`, `FrontendBaseUrl`, a public HTTPS `WebhookUrl` ending `/api/v1/payments/zahls/webhook`, and `PaymentMethods` using only identifiers enabled by the merchant's TEST provider. `TestProviderConfigured` and `ChfSupportedByTestProvider` must represent actual account checks. Do not put credentials or tunnel URLs in tracked files or the React environment. The hosted link is validated against provider-controlled HTTPS hosts before storage and browser navigation. Browser success/failure/cancel returns only start server-side verification; signed webhook and reconciliation determine financial state. An uncertain Gateway-create response retains the pending reservation for investigation rather than opening another payable session.

The Zahls TEST account and all three private keys are **not yet available**. Swiss registration is confirmed by the owner, but PSP configuration, CHF method support, genuine Gateway creation, signed HTTPS callback, payment/decline/cancel/retry/reconciliation/refund acceptance, and business shipping/tax/capture approvals remain open. This integration is not live payment readiness or Phase 7 authorization. See the [current Phase 6 runbook](docs/implementation/phase-06-checkout-payment-shipping.md) and [final status report](docs/implementation/phase-06-final-report.md).

## Phase 6 Datatrans sandbox (historical record)

The following section documents the earlier Datatrans decision and its test results. Zahls is the selected provider for new checkout payments.

Checkout requires .NET 8, Node 22+, SQL Server, saved customer addresses and administrator-configured shipping zones/methods. Review the database backup and migration SQL before applying changes. The Phase 6 migration is already applied in the verified local database; do not rerun its raw SQL blindly.

Payments are disabled by default. A Datatrans sandbox merchant account with confirmed support for the existing LKR catalogue is required. Setting a currency name does not convert prices. Do not change the store to CHF without approved repricing.

Keep provider credentials in ASP.NET user secrets or deployment-managed secrets. Add these keys to the existing configuration without overwriting Email, JWT or SQL settings:

- Datatrans:MerchantId
- Datatrans:ApiPassword
- Datatrans:HmacKey (hexadecimal key supplied by Datatrans)
- Datatrans:Enabled
- Datatrans:Environment (Sandbox only in this phase)
- Datatrans:ApiVersion (v1)
- Datatrans:VerifiedCurrencies:0 (LKR only after merchant verification)
- Datatrans:FrontendBaseUrl (http://localhost:5173/Amaara-Creations for local testing)
- Datatrans:WebhookUrl (public HTTPS tunnel plus /api/v1/payments/datatrans/webhook)
- Datatrans:PaymentMethods:0 (at least one merchant/acquirer-approved method code, such as `VIS`, is required)
- Datatrans:AutoSettle (development default true; capture policy needs business approval)

Safe placeholders appear in [the local configuration example](be/appsettings.Development.Local.example.json). If using an ignored local JSON configuration, merge the Datatrans section into the existing file. Do not copy the example over existing settings. Never place these secrets in client .env files, source control, Swagger examples or logs. Restart the API after changing payment configuration.

For real sandbox callbacks, expose the local API through an HTTPS tunnel and configure its exact webhook URL. The callback uses a Datatrans signature, without a customer JWT. The frontend return URL only displays the server's verification result.

Run the app in separate terminals:

~~~powershell
cd be
dotnet restore
dotnet run --urls http://localhost:5192
~~~

~~~powershell
cd client
npm ci
npm run dev
~~~

Open http://localhost:5173/Amaara-Creations/. In the admin Shipping screen, configure approved destinations, phone/postcode requirements, rates, estimates and pickup details. Development-only methods are excluded unless Checkout:AllowDevelopmentShipping is explicitly enabled in Development. No delivery rates or coupons are seeded into the business database.

Additional checks:

~~~powershell
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release
cd client
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
~~~

The SQL concurrency test requires AMAARA_TEST_SQL_CONNECTION pointing to a disposable database whose name starts with AmaaraPhase6Tests_. It migrates that database and retains identifiable test rows; it refuses a business database. The completed verification used AmaaraPhase6Tests_20261011. npm run test:e2e starts disposable API/frontend fixtures on ports 5198/5178 with generated customer/admin accounts. The current deterministic suite has 49 browser cases and no external credential dependency. The CI acceptance workflow uses the same test-only host. Browser checkout fixtures do not prove a real sandbox payment.

See [the Phase 6 implementation and sandbox runbook](docs/implementation/phase-06-checkout-payment-shipping.md) and [the Phase 6B A–AM acceptance report](docs/implementation/phase-06-final-report.md). Implementation documents remain ignored by the repository's existing Git policy. Real sandbox acceptance is pending merchant credentials, confirmed LKR compatibility, webhook connectivity and business-approved delivery settings.

Current internal Phase 6 regression: 107 backend tests (including isolated SQL races), 60 frontend tests and 49 browser tests pass. Checkout now fails closed in both the server quote and final order operation when Datatrans is disabled or missing a verified currency or payment method. The earlier Phase 6B Gmail confirmation and clearly marked test-order outbox message were delivered and found in the configured inbox; that order fixture used the test gateway.

Email keys are Email:Username, Email:Password and Email:FromEmail in the existing ignored local settings. The current service uses smtp.gmail.com:587 with STARTTLS and an App Password. No OAuth client secret is required. The optional external SMTP acceptance runner requires AMAARA_RUN_SMTP_ACCEPTANCE=1 and a fresh SQL database named AmaaraPhase6Tests_Smtp_*; its exact command and retained evidence are in the runbook.

No tunnel binary is installed. After installing an approved cloudflared binary, the optional node tests/webhook-tunnel-proxy.mjs ingress can restrict a quick tunnel to the existing signed webhook. Start cloudflared tunnel --url http://127.0.0.1:5199 and keep its temporary URL in ignored local configuration. Two ingress tests verify exact UTF-8 bytes/signature-header forwarding and endpoint isolation.

Real Datatrans success/failure/cancellation/retry/reconciliation/refunds remain unexecuted because MerchantId, ApiPassword, HmacKey, FrontendBaseUrl and WebhookUrl are absent, and the verified currency/payment-method allowlists are empty. Merchant/acquirer LKR support and public HTTPS callback delivery remain unverified. Business shipping zones/methods remain empty and require owner-approved values. AutoSettle=true and zero tax/fees remain development settings requiring owner approval.

**NOT READY FOR PHASE 7.**
