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

- `VITE_API_BASE_URL` defaults to `http://localhost:5192/api`
- `VITE_MEDIA_BASE_URL` defaults to `http://localhost:5192`

Quality checks:

```powershell
cd client
npm run lint
npm run build
npm audit --audit-level=high
```

## Backend

```powershell
cd be
dotnet restore
dotnet build
dotnet ef migrations list
dotnet run --urls http://localhost:5192
```

Development configuration is in `be/appsettings.Development.json`. Use user secrets for local private values:

```powershell
cd be
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS01;Database=AmaaraCreationsDB;Trusted_Connection=true;TrustServerCertificate=true;"
dotnet user-secrets set "Jwt:Key" "replace-with-a-local-32-character-minimum-secret"
dotnet user-secrets set "AdminBootstrap:Enabled" "true"
dotnet user-secrets set "AdminBootstrap:Email" "admin@example.test"
dotnet user-secrets set "AdminBootstrap:Password" "replace-with-a-strong-local-password"
```

`AdminBootstrap` is disabled by default and never falls back to a hard-coded password.

## Phase 0 Notes

See `docs/implementation/phase-00-stabilisation.md` for the Phase 0 stabilisation record, command results, current API/route inventory, security changes, and remaining manual infrastructure steps.
