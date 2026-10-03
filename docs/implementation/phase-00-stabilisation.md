# Phase 00 Stabilisation

## Purpose

Phase 0 established a safe development baseline before data-model and feature expansion. The work focused on buildability, environment-based configuration, dependency consistency, credential removal, CI, database/migration visibility, and factual smoke-test results.

## Initial Repository State

- Path: `C:\Nexaura`
- Branch: `main`
- Initial status: clean, up to date with `origin/main`
- Last commits inspected:
  - `326d250 Add back button to admin layout header`
  - `bfee10d Add .gitignore and remove build and node_modules`
  - `af53e17 Add admin dashboard and statistics endpoints`
  - `cccea7e Add user profile and stats endpoints`
  - `4feced8 Add product reviews feature with API endpoints`

## Current Architecture

- Frontend: React SPA, Vite, React Router, Axios, localStorage-backed auth context.
- Backend: ASP.NET Core Web API, ASP.NET Core Identity, EF Core, JWT bearer auth, role-based authorization.
- Database: SQL Server via `Microsoft.EntityFrameworkCore.SqlServer`.
- Uploads: local `wwwroot/uploads/...` static files; product/avatar upload endpoints return relative `/uploads/...` paths.

## Dependency Versions

Frontend:

- Local Node observed: `v20.20.2`
- Project baseline: Node `>=22`, `.nvmrc` = `22`
- npm observed through approved run: `10.8.2`
- React / React DOM: `^19.1.1`
- React Router DOM: `7.18.2`
- Axios: `^1.13.1` resolved to audit-clean `1.20.0`
- Vite: `^6.4.3`
- ESLint: `^9.33.0`

Backend:

- Local SDKs: 7.0.410, 8.0.420, 9.0.304
- Project SDK baseline: .NET SDK `8.0.420` via `global.json`
- Target framework: `net8.0`
- JWT bearer / Identity / EF Core SQL Server / EF Tools: `8.0.10`
- Swashbuckle: `6.6.2`

## Security Findings

- Hard-coded JWT fallback key existed in `Program.cs` and `appsettings.json`.
- Hard-coded admin email/password existed in `Program.cs`, `appsettings.json`, and tracked `be/un pw`.
- Auth debug endpoints exposed request/header information.
- JWT event logging logged auth-header/token prefixes.
- Forgot-password returned and logged reset tokens.
- Frontend hard-coded `http://localhost:5192/api` and product upload URLs.
- CORS origins were hard-coded in code.
- Upload validation checked extension/size but not content type.

## Changes Implemented

- Removed tracked credential sample file `be/un pw`.
- Removed production JWT/admin fallbacks.
- Added required JWT/config validation at startup.
- Added `AdminBootstrap` config, disabled by default and idempotent when enabled.
- Removed auth debug endpoints.
- Stopped returning/logging password reset tokens.
- Removed auth-header/token-prefix logging.
- Added environment-driven CORS origins.
- Added `client/.env.example`, `VITE_API_BASE_URL`, `VITE_MEDIA_BASE_URL`, and central frontend config.
- Store newly uploaded product image paths as relative paths.
- Added upload MIME validation and configurable max file size.
- Added `/health`.
- Added local Data Protection key storage under ignored `be/App_Data/`.
- Made Development startup tolerate missing SQL Server for `/health`; non-development still fails fast.
- Aligned backend packages to .NET 8.
- Updated frontend dependencies to audit-clean versions and removed `react-icons`.
- Updated Vite config for reproducible builds with React Router/Axios aliases and treeshaking disabled.
- Added `.nvmrc`, `global.json`, and CI backend build/lint gates.

## Configuration

Backend settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:AccessTokenMinutes`
- `Cors:AllowedOrigins`
- `Uploads:MaxFileSizeBytes`
- `AdminBootstrap:Enabled`
- `AdminBootstrap:Email`
- `AdminBootstrap:Password`

Frontend settings:

- `VITE_API_BASE_URL`
- `VITE_MEDIA_BASE_URL`

Use `dotnet user-secrets` for local private backend values. Production secrets must come from environment/secret management.

## Database And Migrations

Migrations listed successfully:

- `20251101195840_InitialCreate`
- `20251101200718_InitialIdentity`
- `20251101203958_AddProducts`
- `20251101210010_AddCartItems`
- `20251102095431_AddOrdersAndOrderItems`
- `20251102101324_AddWishlistItems`
- `20251102102538_AddReviews`

SQL Server was not available at `localhost\SQLEXPRESS01`, so applied/pending status and `database update` were not verified.

## API Inventory

Auth:

- `POST /api/auth/register` public registration
- `POST /api/auth/login` public login
- `POST /api/auth/logout` authenticated logout
- `GET /api/auth/me` authenticated current user
- `POST /api/auth/change-password` authenticated password change
- `POST /api/auth/refresh-token` placeholder response only
- `POST /api/auth/forgot-password` generic response, no token disclosure
- `POST /api/auth/reset-password` token-based reset

Products:

- `GET /api/products` public list/search/filter
- `GET /api/products/{id}` public details
- `POST /api/products` admin create
- `PUT /api/products/{id}` admin update
- `DELETE /api/products/{id}` admin soft delete

Cart:

- `GET /api/cart`
- `POST /api/cart`
- `PUT /api/cart/{itemId}`
- `DELETE /api/cart/{itemId}`
- `DELETE /api/cart`

Wishlist:

- `GET /api/wishlist`
- `POST /api/wishlist`
- `DELETE /api/wishlist/{productId}`
- `POST /api/wishlist/{productId}/cart`
- `DELETE /api/wishlist`

Orders:

- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders`
- `PUT /api/orders/{id}/status` admin
- `GET /api/orders/admin/all` admin
- `GET /api/orders/admin/{id}` admin

Reviews:

- `GET /api/products/{productId}/reviews`
- `POST /api/products/{productId}/reviews`
- `PUT /api/reviews/{id}`
- `DELETE /api/reviews/{id}`
- `GET /api/admin/reviews` admin
- `DELETE /api/admin/reviews/{id}` admin
- `GET /api/admin/reviews/stats` admin

Admin:

- `GET /api/admin/dashboard`
- `GET /api/admin/dashboard/revenue`
- `GET /api/admin/dashboard/orders`
- `GET /api/admin/dashboard/products`
- `GET /api/admin/dashboard/recent-orders`
- `GET /api/admin/customers`
- `GET /api/admin/customers/{id}`
- `GET /api/admin/customers/{id}/orders`
- `GET /api/admin/customers/{id}/stats`

Users/uploads:

- `GET /api/users/profile`
- `PUT /api/users/profile`
- `GET /api/users/profile/stats`
- `GET /api/users/profile/avatar`
- `POST /api/users/profile/avatar`
- `POST /api/upload/product-image` admin
- `POST /api/upload/avatar`
- `GET /health`

## Frontend Route Inventory

Working/registered:

- `/`
- `/products`
- `/products/:id`
- `/custom`
- `/wishlist`
- `/cart`
- `/checkout`
- `/profile`
- `/orders`
- `/orders/:id`
- `/login`
- `/register`
- `/admin/dashboard`
- `/admin/products`
- `/admin/reviews`
- `/admin/customers`
- `/admin/orders`

Known incomplete/future:

- Custom builder remains a frontend prototype; full configured cart/order production workflow is deferred.

## Command Results

- `pwd`: pass
- `git status`: initial clean; final dirty with Phase 0 edits
- `git branch --show-current`: `main`
- `git log -5 --oneline`: pass
- `node --version`: `v20.20.2`
- `npm --version`: sandbox-blocked directly; approved npm runs show `10.8.2`
- `npm ci`: pass, warns local Node 20 does not satisfy project Node `>=22`
- `npm run lint`: pass with 3 warnings (`react-hooks/exhaustive-deps` in admin pages)
- `npm run build`: pass
- `npm audit --audit-level=high`: pass, 0 vulnerabilities
- `dotnet --info`: pass
- `dotnet restore`: pass
- `dotnet build --no-restore`: pass, 0 warnings
- `dotnet test --no-restore`: pass; no test project output
- `dotnet list package --vulnerable`: pass with no vulnerable backend packages
- `dotnet ef migrations list --no-build`: pass/listed migrations; DB status unavailable because SQL Server was not reachable
- `dotnet run --no-build --urls http://localhost:5192`: pass for Development health startup; DB-backed endpoints remain unverified without SQL Server
- `GET http://localhost:5192/health`: pass

## Runtime Smoke Test

- Homepage: not browser-tested
- Products: not tested against API because SQL Server unavailable
- Authentication: not tested because SQL Server unavailable
- Cart: not tested because SQL Server unavailable
- Wishlist: not tested because SQL Server unavailable
- Orders: not tested because SQL Server unavailable
- Admin: not tested because SQL Server unavailable
- Backend health: pass

## External Dependencies

- SQL Server: requires configuration; unavailable in this environment
- Email provider: not implemented/configured
- Payment provider: not implemented
- Object storage: not implemented; local uploads only
- GitHub Pages: frontend workflow exists

## Checklist

- [x] Repository inspected
- [x] Existing user changes preserved
- [x] Frontend dependencies install
- [x] Frontend builds
- [x] Backend restores
- [x] Backend builds
- [x] EF migrations list successfully
- [ ] Development DB migration verified
- [x] Secrets removed from tracked source
- [x] JWT configuration environment-based
- [x] Database configuration environment-based
- [x] API URL environment-based
- [x] Admin bootstrap safe
- [x] Auth debug endpoints removed
- [x] Sensitive logs removed
- [x] Upload validation checked
- [x] CORS environment-based
- [x] Health endpoint available
- [x] CI uses supported Node
- [x] CI uses supported .NET
- [x] README updated
- [x] .gitignore checked
- [ ] Full data-backed smoke tests performed
- [x] Remaining limitations documented

## Known Technical Debt

- Frontend still stores JWT in `localStorage`; HttpOnly refresh/session architecture is deferred.
- Frontend lint has 3 hook dependency warnings in admin pages.
- Vite treeshaking is disabled to avoid Rollup hanging on current dependency graph in this environment.
- Password reset requires a real email flow before production use.
- Data Protection keys are local and unencrypted in Development; production should use a managed key store.

## Deferred To Phase 1

- Entity/data model expansion.
- Category/collection/variant model.
- Media architecture beyond local relative paths.
- Custom design persistence.
- Order item snapshots and richer order model.
- Payment/shipping schema foundations.
- Audit log.
- TypeScript foundation.

## Deferred To Phase 2

- HttpOnly refresh-token sessions.
- Refresh token rotation.
- Email verification.
- Production forgot/reset password email flow.
- Saved addresses.
- Expanded roles/policies.
- MFA and logout-all.

## Manual Actions Required

- Rotate any previously committed admin/JWT-like credentials.
- Configure production SQL Server and run migrations.
- Configure production `Jwt:Key`, `Cors:AllowedOrigins`, and connection string through secrets/environment.
- Configure a production Data Protection key store.
- Configure email before enabling production password reset.

## New Developer Commands

```powershell
git clone <repo>
cd Amaara-Crea

cd client
npm ci
copy .env.example .env
npm run dev

cd ..\be
dotnet restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS01;Database=AmaaraCreationsDB;Trusted_Connection=true;TrustServerCertificate=true;"
dotnet user-secrets set "Jwt:Key" "replace-with-a-local-32-character-minimum-secret"
dotnet ef database update
dotnet run --urls http://localhost:5192
```

## Phase 1 Readiness

Status: ready for Phase 1 after a developer supplies a reachable SQL Server and verifies `dotnet ef database update` plus data-backed auth/product/cart/admin smoke tests.
