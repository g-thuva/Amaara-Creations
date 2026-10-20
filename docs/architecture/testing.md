# Testing and architecture checks

Use Node 22+ and the .NET SDK selected by `global.json`. Run from the repository root unless noted.

```powershell
dotnet tool restore
dotnet restore tests/be.Tests/be.Tests.csproj
dotnet build tests/be.Tests/be.Tests.csproj --configuration Release --no-restore
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --no-build
dotnet ef migrations list --project be --configuration Release --no-build --no-connect
dotnet ef migrations has-pending-model-changes --project be --configuration Release --no-build
npm --prefix client ci
npm --prefix client run lint
npm --prefix client run typecheck
npm --prefix client test -- --run
npm --prefix client run build
npm --prefix client run test:e2e
npm --prefix client run test:e2e:production
npm --prefix client run check:performance
node --test tests/webhook-tunnel-proxy.test.mjs
python scripts/scan-secrets.py
git diff --check
```

SQL integration, concurrency, migration, recovery and load tests require `AMAARA_TEST_SQL_CONNECTION`. Its database name must begin `AmaaraPhase6Tests_`; provide an isolated test connection through the environment. Never use a business database. Tests also create uniquely named disposable databases for separate recovery/load scenarios. SQL-less runs explicitly skip these tests and do not prove persistence concurrency.

| Suite | Purpose |
| --- | --- |
| Backend `Unit` | Pure commerce/provider rules, application workflow state, media validation, pagination and middleware. |
| Backend `Integration` | HTTP authorization/ownership, persistence model, checkout/payment/refund workflows, production, reviews, notifications, real SQL and controlled load/recovery. |
| Backend `Architecture` | Layer direction, HTTP/provider isolation, original route/security/role compatibility and immutable migration payloads. |
| Backend `Fixtures` | Explicit request actors, fake providers, realistic commerce setup and disposable HTTP hosts. |
| Frontend colocated tests | Feature models, presentation and shared transport/lifecycle behavior. |
| Playwright | Public/account/checkout/admin journeys, race conditions, accessibility, keyboard and responsive behavior. |
| Production browser | Built-app route/metadata/accessibility and local lab performance/bundle gates. |
| Webhook ingress | Local proxy raw-byte/signature/forwarding behavior. |

The acceptance executable references test fixtures only. It is not registered in or published with the business API. External SMTP acceptance is explicitly opt-in; deterministic CI uses captured email and fake payment adapters. No unit/browser fixture constitutes authenticated Zahls merchant acceptance or a production transaction.

Architecture snapshots come from clean commit `5b0f639`. The endpoint fixture has one line per original endpoint; it retains route, authorization and body/rate-limit metadata. Migration fingerprints exclude namespace imports, blank lines and line indentation; literal contents and historical statements remain significant. Deliberate future contract changes should update these fixtures with reviewable evidence; never regenerate them to hide a regression.

CI runs Release restore/build/tests, the pinned offline EF checks, dependency/privacy/secret checks and separate Node 22 browser gates on develop pushes, pull requests and dispatch. It retains safe SQL/browser diagnostics. Existing GitHub Pages deployment automation still targets `main`; development work is committed/pushed only to `develop`. See [quality controls](../quality/release-hardening.md) and the [Phase 8 result](../reports/phase8-quality-result.md).
