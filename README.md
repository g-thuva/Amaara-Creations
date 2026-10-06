# Amaara Creations

Amaara Creations is a React/Vite frontend with an ASP.NET Core Web API backend using ASP.NET Core Identity, JWT authentication, EF Core, and SQL Server.

## Prerequisites

- Node.js 22 or newer (`.nvmrc` is provided)
- npm 10+
- .NET SDK 8.0.x (`global.json` pins the SDK line)
- SQL Server or SQL Server Express for full API/data workflows

## Frontend

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

Development configuration is in `be/appsettings.Development.json`. Use user secrets for local private values:

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
