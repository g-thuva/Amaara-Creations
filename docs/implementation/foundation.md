# Foundation - Foundation & Data Model

## Starting Repository State

- Current working directory: `C:\Nexaura`.
- Repository started clean after Phase 0 commits.
- Backend baseline: .NET 8 (`global.json` SDK line 8.0.420), ASP.NET Core Identity, EF Core SQL Server 8.0.10, JWT auth.
- Frontend baseline: React 19, Vite 6.4, Node 22 expected by `.nvmrc`, JavaScript app with incremental TypeScript introduced in this phase.
- Existing APIs used legacy `/api/[controller]` routes; Foundation preserves them and adds `/api/v1/[controller]`.
- SQL Server was not available locally during implementation, so migration compilation/script generation was verified, but live database update was not truthfully claimed.

## Architecture Decisions

- Preserve the modular-monolith shape. No microservices or extra backend projects were introduced.
- Keep .NET 8 because Phase 0 intentionally aligned and documented that supported baseline.
- Prefer additive schema changes and backfills over destructive table replacement.
- Preserve mutable catalog compatibility fields (`Product.Price`, `Product.Category`, `Product.ImageUrl`) while adding durable foundations (`Category`, `ProductVariant`, `ProductMedia`).
- Add order item snapshot fields so historical order reads no longer depend solely on mutable product values.
- Store media metadata/keys, not large file blobs.
- Add request correlation and ProblemDetails without changing every controller response in one risky sweep.

## Runtime And Package Changes

- Backend runtime remains .NET 8 / EF Core 8.0.10.
- Frontend added:
  - `typescript`
  - `vitest`
  - `npm run typecheck`
  - `npm test`
- CI now runs backend tests and frontend lint/typecheck/test/build.

## Database Entities Added Or Changed

Added foundation entities:

- `Category`
- `Collection`
- `ProductCollection`
- `ProductVariant`
- `ProductMedia`
- `Address`
- `CustomDesign`
- `CustomDesignAsset`
- `RefreshSession`
- `OrderStatusHistory`
- `Payment`
- `ShippingZone`
- `ShippingMethod`
- `Shipment`
- `Promotion`
- `AuditLog`

Changed existing entities:

- `Product`: added `Slug`, `ShortDescription`, `BasePrice`, `CategoryId`, `BaseSku`, `IsFeatured`, and navigation collections.
- `OrderItem`: made `ProductId` nullable for future custom-design lines, and added `ItemType`, `ProductVariantId`, `CustomDesignId`, `ProductNameSnapshot`, `SkuSnapshot`, `UnitPrice`, `ConfigurationSnapshotJson`, and `ImageSnapshot`.
- `Order`: added navigation collections for status history, payments, and shipments.

## Important Relationships

- Product optionally belongs to a Category.
- Product has many ProductVariants and ProductMedia records.
- Product can belong to many Collections through ProductCollection.
- Address and RefreshSession are user-owned.
- CustomDesign optionally belongs to a User and has many CustomDesignAssets.
- OrderItem can point to Product, ProductVariant, or CustomDesign while always retaining immutable snapshot fields.
- Payment, Shipment, and OrderStatusHistory point to Order with restrictive delete behavior.
- ProductVariant uses rowversion concurrency for stock-sensitive changes.

## Migration

- Migration name: `20261003072610_FoundationFoundationDataModel`.
- SQL script: `docs/implementation/foundation.sql`.

## Migration And Backfill Strategy

- Additive schema changes are used wherever possible.
- `Products.BasePrice` is backfilled from existing `Products.Price`.
- `Products.Slug` is backfilled to deterministic `product-{Id}` values before the unique index is created.
- Existing `OrderItems` are backfilled with:
  - `UnitPrice` from `Price`
  - `ProductNameSnapshot` from `Products.Name`
  - `ImageSnapshot` from `Products.ImageUrl`
  - `SkuSnapshot` from `Products.BaseSku`
- Existing `Products.ImageUrl` values are copied into `ProductMedia`.
- Existing product stock is copied into a default `ProductVariant`.
- Existing free-text `Products.Category` values are copied into `Categories`, then `Products.CategoryId` is populated.
- Existing orders/order items are preserved.

## API Conventions

- Legacy `/api/[controller]` routes remain available for compatibility.
- Versioned `/api/v1/[controller]` routes were added to every controller.
- `/api/v1/health` was added alongside `/health`.
- `ProblemDetails` is configured for exception/status handling and model validation.
- Validation ProblemDetails include a `correlationId` extension where available.
- Request correlation accepts a safe `X-Correlation-ID` header or generates one, stores it in `HttpContext.Items`, adds it to response headers, and scopes logs.

## Pagination

- Added `PaginationQuery` with a maximum page size of 100.
- Added reusable `PagedResult<T>` with navigation metadata.
- Existing list endpoints remain compatible and can migrate incrementally.

## TypeScript Frontend Foundation

- Added `tsconfig.json`.
- Added `src/vite-env.d.ts`.
- Added shared API types in `src/types/api.ts`.
- Added `normalizeApiError` in TypeScript and connected it to the existing Axios interceptor.
- Updated default API base URL to `http://localhost:5192/api/v1`.
- Improved media URL derivation so `/api/v1` does not become the media host.

## Tests Added

- Backend xUnit test project: `tests/be.Tests`.
- Tests cover:
  - pagination contracts
  - Foundation EF model entity presence
  - ProductVariant rowversion concurrency
  - OrderItem snapshot/nullability foundation
  - correlation middleware safe incoming ID behavior
- Frontend Vitest test covers API error normalization for ProblemDetails and legacy message responses.

## Commands Executed

- `git status --short`
- `dotnet build be\be.csproj --no-restore --configuration Release`
- `dotnet ef migrations add FoundationFoundationDataModel --project be\be.csproj --startup-project be\be.csproj --configuration Release`
- `dotnet ef migrations script --project be\be.csproj --startup-project be\be.csproj --configuration Release --idempotent --output docs\implementation\foundation.sql`
- `dotnet restore tests\be.Tests\be.Tests.csproj`
- `dotnet test tests\be.Tests\be.Tests.csproj --configuration Release --no-restore`
- `npm install`
- `npm ci`
- `npm run typecheck`
- `npm run lint`
- `npm test`
- `npm run build`
- `npm audit --audit-level=high`

## Build And Test Results

- Backend Release build: passed.
- Backend tests: passed, 6 tests.
- EF migration list: migration is discoverable; applied/pending status unavailable because SQL Server could not be reached.
- EF migration script generation: passed.
- `npm ci`: attempted twice, but blocked by Windows `EPERM` file locks on native esbuild/Rollup binaries under `node_modules`; dependencies were repaired with `npm install`.
- `npm install`: passed with the expected Node 20 versus project Node 22 engine warning.
- Frontend lint: passed with 3 existing `react-hooks/exhaustive-deps` warnings in admin pages.
- Frontend typecheck: passed.
- Frontend Vitest: passed, 2 tests.
- Frontend production build: passed.
- Frontend audit: passed, 0 vulnerabilities.

## Problems Discovered

- Debug backend build output was locked by a running `be.exe`; Release builds were used for validation.
- Local Node is v20.20.2 while the project requires Node 22. npm commands completed with `EBADENGINE` warnings.
- `npm ci` could not complete because Windows held native dependency binaries open. This is an environment file-lock issue, not a lockfile or package resolution error.
- Local SQL Server was not available, so live migration application could not be verified.

## Known Follow-Up Work

- Apply the generated migration to a real SQL Server database and run smoke tests against Products, Orders, Admin dashboard, Cart, Wishlist, Reviews, Uploads, and Auth.
- Migrate list endpoints from legacy custom pagination responses to `PagedResult<T>`.
- Replace remaining manual anonymous error payloads with ProblemDetails helpers where practical.
- Implement Authentication refresh-session lifecycle using the new `RefreshSessions` table.
- Build address UI/workflows in Authentication using the new `Addresses` table.

## Authentication Prerequisites

- Confirm SQL Server migration succeeds on a copy of production-like data.
- Confirm backfilled categories/media/variants are acceptable to admin users.
- Decide refresh-token rotation policy and token hashing strategy.
- Decide default-address uniqueness enforcement approach before saved address UI ships.
