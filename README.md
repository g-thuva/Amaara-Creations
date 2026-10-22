# Amaara Creations

React/Vite storefront and staff operations backed by an ASP.NET Core 8 / EF Core / SQL Server modular monolith.

The boutique redesign and security work is documented in [the implementation and release report](docs/redesign/TEST_AND_RELEASE_REPORT.md). The storefront and studio operations now have independent build artifacts. Administrative APIs default to disabled; read [staff isolation and rollout](docs/security/ADMIN_ISOLATION_DEPLOYMENT.md) before enabling them. This local implementation is not production deployment approval.

Account security, verified email/export/request workflows and safe media adapters are described in [activation and operations](docs/redesign/ACCOUNT_REQUESTS_AND_MEDIA.md). Review the [variant migration and rollback guard](docs/redesign/VARIANT_MIGRATION.md) before applying the two new additive migrations to an existing environment. The completed feature branch is kept local at the user's request.

- [Architecture and module ownership](docs/architecture/overview.md)
- [Backend](docs/architecture/backend.md), [frontend](docs/architecture/frontend.md), [admin](docs/architecture/admin.md)
- [Domain glossary](CONTEXT.md) and [architecture decisions](docs/adr/0001-modular-monolith.md)
- [Testing and quality gates](docs/architecture/testing.md)
- [Full repository refactor verification](docs/reports/full-repository-refactor-result.md)
- [Quality controls](docs/quality/release-hardening.md) and [Phase 8 verification](docs/reports/phase8-quality-result.md)
- [Release evidence](docs/release/engineering-record.md) and [cutover/recovery runbook](docs/runbooks/release-cutover.md)
- [Verified package and production cutover procedure](docs/release/production-cutover.md)
- [Completed architecture, quality and release verification](docs/reports/autonomous-completion-result.md)

## Development

Use Node 22+ (`.nvmrc`), npm 10+, the .NET 8 SDK selected by `global.json`, and SQL Server for full persistence workflows. Keep source work on `develop`.

```powershell
dotnet tool restore
dotnet restore tests/be.Tests/be.Tests.csproj
npm --prefix client ci
```

Configure backend secrets with `dotnet user-secrets --project be` or the ignored `be/appsettings.Development.Local.json`; use `be/appsettings.Development.Local.example.json` as the key reference. Set the SQL connection and a local JWT signing key of at least 32 characters. Bootstrap accounts are disabled by default and require explicitly configured values. Never put private keys in Vite configuration.

Before starting, check that the configured local development database has all committed migrations. Back up an existing database and review pending migrations before updating it. The update below explicitly targets `localhost/AmaaraCreationsDB`; confirm it is the intended local database. Run these commands in a normal PowerShell terminal so SQL Server Windows authentication can use your account:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations list --project be
dotnet ef database update --project be --connection 'Server=localhost;Database=AmaaraCreationsDB;Trusted_Connection=true;TrustServerCertificate=true'
```

```powershell
dotnet run --project be --urls http://localhost:5192
```

Open [Swagger UI](http://localhost:5192/swagger/index.html) for interactive API documentation, or [OpenAPI JSON](http://localhost:5192/swagger/v1/swagger.json) for the API definition. [Database readiness](http://localhost:5192/health/ready) must return `200` with `{"status":"ready"}`; a running Swagger page alone does not confirm database connectivity or an up-to-date schema.

In a second terminal:

```powershell
npm --prefix client run dev
```

Copy `client/.env.example` to a private local environment file when overriding public URLs. The default API/media origin is `http://localhost:5192`; public API routes are canonical `/api/v1/*`. The development frontend base is `/Amaara-Creations/`. Swagger is enabled in Development only.

## Verification

```powershell
dotnet build tests/be.Tests/be.Tests.csproj --configuration Release
dotnet test tests/be.Tests/be.Tests.csproj --configuration Release --no-build
dotnet ef migrations list --project be --configuration Release --no-build --no-connect
dotnet ef migrations has-pending-model-changes --project be --configuration Release --no-build
npm --prefix client run lint
npm --prefix client run typecheck
npm --prefix client test -- --run
npm --prefix client run build
npm --prefix client run build:admin
npm --prefix client run check:isolation
npm --prefix client run test:e2e
npm --prefix client run test:e2e:production
npm --prefix client run check:performance
node --test tests/webhook-tunnel-proxy.test.mjs
python scripts/scan-secrets.py
git diff --check
```

SQL concurrency, recovery and load tests require `AMAARA_TEST_SQL_CONNECTION` with a disposable database named `AmaaraPhase6Tests_*`. SQL-less runs skip those cases and do not prove SQL behavior. The [testing guide](docs/architecture/testing.md) explains fixtures, integration limits and architecture contracts.

EF's design-time factory supports offline model checks without HTTP startup or seeding. Before any online migration, identify the intended database and follow the cutover runbook; supply the explicit connection through configuration or `--connection`. Historical migrations remain intact.

## Repository map

```text
be/
  Api/                 HTTP, authorization adapters, middleware and OpenAPI
  Application/         feature contracts, use cases and integration abstractions
  Domain/              entities, states and invariant policies
  Infrastructure/      persistence, identity, payments, storage, email and jobs
  Program.cs           explicit startup composition
client/
  src/app/             shell, routing, guards, providers and navigation
  src/features/        storefront/account/commerce and nested admin workflows
  src/shared/          generic transport, lifecycle, presentation and formatting
  e2e/                 public/account/checkout/admin/production browser journeys
tests/                 backend tests, isolated acceptance host and webhook checks
docs/                  architecture, decisions, reports and release/runbooks
scripts/               secret, dependency, artifact and cutover checks
deploy/                inactive production configuration templates
```

## Release and deployment

Application engineering, genuine merchant/provider acceptance and production cutover are separate gates. Zahls remains TEST-only and disabled without valid configuration; historical Datatrans/LKR records remain readable. Missing production credentials, approved business policies or an identified hosting/domain target do not become successful launch evidence.

Use [the release record](docs/release/engineering-record.md) for verified local rehearsals and external blockers, and [the cutover runbook](docs/runbooks/release-cutover.md) for exact configuration, migration, backup/restore, smoke and rollback procedures. GitHub Pages is a historical static preview described in [its runbook](docs/runbooks/static-preview.md); its existing automation targets `main` and does not deploy the backend.
