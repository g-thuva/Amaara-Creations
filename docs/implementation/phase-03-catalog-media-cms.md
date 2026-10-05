# Phase 3 - Catalogue, Media & CMS Administration

## Purpose

Phase 3 turns the Phase 1 catalogue foundations into manageable admin workflows: products, categories, collections, variants, media, inventory adjustments, CMS pages, site settings, and Swagger/OpenAPI documentation.

## Initial Phase 3 Repository State

- Path: `C:\Nexaura`
- Branch: `develop`
- Initial status: clean
- Latest commits inspected:
  - `d65b2e7 docs: record phase 2 authentication baseline`
  - `083cc0c test(auth): cover session token foundations`
  - `b0ff0e0 feat(client): move auth to memory and guarded routes`
  - `5f892d3 feat(api): add address management and auth policies`
  - `9e91578 feat(api): implement secure refresh-session authentication`

## Verified Phase 0/1/2 Status

Phase 0 documented the .NET 8 / React / SQL Server baseline, environment configuration, upload hardening, and Swagger package presence. Phase 1 added the catalogue foundation entities: `Category`, `Collection`, `ProductCollection`, `ProductVariant`, `ProductMedia`, `AuditLog`, and related commerce foundations. Phase 2 added refresh sessions, email/account flows, saved addresses, and policies including `ManageCatalog` and `ManageContent`.

## Architecture Used

The implementation preserves the modular monolith. Admin APIs live under `/api/v1/admin/*`; public catalogue/content APIs live under `/api/v1/catalog/*` and `/api/v1/content/*`. Existing storefront product APIs remain compatible.

## Database / Schema Changes

Migration: `20261005103855_Phase3CatalogueMediaCms`.

Added tables:

- `InventoryTransactions`
- `CmsPages`
- `CmsSections`
- `SiteSettings`

Extended `ProductMedia` with metadata fields: content type, file size, width, height, original filename, storage provider, and creating user.

## Product Management

Implemented `/api/v1/admin/products` with server-side pagination, search, filters, safe sort keys, create, update, archive, reactivate, collection assignment, SKU uniqueness validation, and category reference validation.

## Category Management

Implemented `/api/v1/admin/categories` with list/detail/create/update/archive/reactivate. Parent hierarchy is supported because Phase 1 included `ParentCategoryId`; self-parenting and obvious cycles are rejected.

## Collection Management

Implemented `/api/v1/admin/collections` with list/detail/create/update/archive/reactivate and product assignment/removal. Duplicate product assignment is rejected.

## Variant Management

Implemented product variant list/create/update/archive/reactivate endpoints below `/api/v1/admin/products/{id}/variants`. Variant SKU uniqueness, non-negative stock, non-negative price override, and rowversion concurrency handling are included.

## Inventory Baseline

Implemented product-level stock adjustment endpoint `/api/v1/admin/products/{id}/stock-adjustments`. Adjustments cannot make stock negative and are recorded in `InventoryTransactions`.

## Media Storage Architecture

Added `IMediaStorageService` and `LocalMediaStorageService`. Local storage writes under `wwwroot/uploads/{area}/yyyy/MM/{generated-name}` and stores portable storage keys/URLs instead of absolute machine paths.

## Upload Security

Catalogue uploads accept JPEG, PNG, and WebP. The server validates configured size, file signatures, decoded dimensions where implemented, generated filenames, and max image pixels. SVG and executable-style arbitrary uploads are not accepted.

## Media Library

Implemented `/api/v1/admin/media` list/detail over `ProductMedia` metadata, plus product media upload/update/remove/primary-image management under `/api/v1/admin/products/{id}/media`.

## CMS Architecture

Implemented `CmsPage` and `CmsSection` with draft/published state. Public retrieval is only for published pages at `/api/v1/content/pages/{slug}`. CMS content is structured text; no raw HTML rendering path was added.

## Site Settings

Implemented admin settings upsert/list at `/api/v1/admin/settings` and public settings at `/api/v1/content/settings`. Secret-like keys containing words such as `secret`, `password`, `token`, `jwt`, or `connectionstring` are rejected.

## Public Catalogue API

Added public active category and collection endpoints:

- `GET /api/v1/catalog/categories`
- `GET /api/v1/catalog/collections`

## Admin APIs

Admin APIs use Phase 2 policies: `ManageCatalog` for catalogue/media and `ManageContent` for CMS/settings.

## Authorization Policies

- Product/category/collection/variant/media: `ManageCatalog`
- CMS/settings: `ManageContent`
- Existing dashboard/customer/order/review policies are unchanged.

## Audit Logging

Added `IAuditService` / `AuditService`. Admin create/update/archive/reactivate/upload/assignment mutations write `AuditLog` entries with actor, entity, values, correlation ID, and IP where available.

## Concurrency Handling

Variant updates support `RowVersion` and return HTTP 409 when EF detects a stale update. Product-level stock adjustment is transactional within the EF save operation but does not implement checkout reservation, which is deferred.

## Swagger / OpenAPI Implementation

Existing Swashbuckle setup is retained and the new controllers/DTOs are exposed through the v1 document. Swagger Bearer auth remains configured in Development.

## Swagger Authentication Instructions

1. Start the backend in Development.
2. Call `POST /api/v1/auth/login`.
3. Copy the returned access token.
4. Click **Authorize** in Swagger.
5. Paste the raw JWT token; Swagger applies the Bearer scheme.

## Exact Swagger URLs

Verified in this environment on an alternate port because `5192` was already in use:

- Swagger UI: `http://localhost:5193/swagger`
- Swagger direct page: `http://localhost:5193/swagger/index.html`
- OpenAPI JSON: `http://localhost:5193/swagger/v1/swagger.json`
- Backend API base: `http://localhost:5193/api/v1`

Default documented development URLs remain `http://localhost:5192/*` when that port is free.

## New Frontend Admin Routes

- `/admin/products`
- `/admin/categories`
- `/admin/collections`
- `/admin/media`
- `/admin/content`
- `/admin/settings`

## API Endpoint Inventory

| Method | Route | Authentication | Policy | Purpose |
|---|---|---|---|---|
| GET | `/api/v1/admin/products` | Bearer | ManageCatalog | Paginated product list |
| GET | `/api/v1/admin/products/{id}` | Bearer | ManageCatalog | Product details |
| POST | `/api/v1/admin/products` | Bearer | ManageCatalog | Create product |
| PUT | `/api/v1/admin/products/{id}` | Bearer | ManageCatalog | Update product |
| POST | `/api/v1/admin/products/{id}/archive` | Bearer | ManageCatalog | Archive product |
| POST | `/api/v1/admin/products/{id}/reactivate` | Bearer | ManageCatalog | Reactivate product |
| GET/POST | `/api/v1/admin/products/{id}/variants` | Bearer | ManageCatalog | Manage variants |
| PUT | `/api/v1/admin/products/{productId}/variants/{variantId}` | Bearer | ManageCatalog | Update variant |
| POST | `/api/v1/admin/products/{id}/media` | Bearer | ManageCatalog | Upload product media |
| GET/PUT/DELETE | `/api/v1/admin/products/{productId}/media/{mediaId}` | Bearer | ManageCatalog | Manage product media |
| GET/POST/PUT | `/api/v1/admin/categories` | Bearer | ManageCatalog | Manage categories |
| GET/POST/PUT | `/api/v1/admin/collections` | Bearer | ManageCatalog | Manage collections |
| PUT/DELETE | `/api/v1/admin/collections/{id}/products` | Bearer | ManageCatalog | Assign/remove products |
| GET | `/api/v1/admin/media` | Bearer | ManageCatalog | Media library |
| GET/POST/PUT | `/api/v1/admin/content/pages` | Bearer | ManageContent | Manage CMS pages |
| POST | `/api/v1/admin/content/pages/{id}/publish` | Bearer | ManageContent | Publish page |
| POST | `/api/v1/admin/content/pages/{id}/unpublish` | Bearer | ManageContent | Unpublish page |
| GET/PUT | `/api/v1/admin/settings` | Bearer | ManageContent | Manage safe settings |
| GET | `/api/v1/catalog/categories` | Public | None | Active categories |
| GET | `/api/v1/catalog/collections` | Public | None | Active collections |
| GET | `/api/v1/content/pages/{slug}` | Public | None | Published CMS page |
| GET | `/api/v1/content/settings` | Public | None | Public settings |

## Environment / Config Additions

- `Uploads:MaxImagePixels`
- Development SQL Server explicitly uses `Encrypt=false` alongside Windows authentication to avoid local TLS negotiation failures; production connection security remains environment-managed.

Existing `Uploads:MaxFileSizeBytes` is reused by the storage service.

## Tests

Backend model tests now cover Phase 3 entity mappings and ProductMedia metadata. Frontend existing Vitest coverage still passes. No new E2E framework was added.

## Build Results

- `dotnet restore`: PASS
- `dotnet build --configuration Release`: PASS
- `dotnet test ..\tests\be.Tests\be.Tests.csproj --configuration Release --no-restore`: PASS, 8 tests
- `dotnet ef migrations list --configuration Release --no-build`: PASS; all migrations through `20261005103855_Phase3CatalogueMediaCms` are applied
- `dotnet ef database update --configuration Release --no-build`: PASS using the Development connection string and the normal Windows authentication context
- `npm ci`: FAIL, locked native esbuild binary
- `npm install`: PASS, repaired dependencies with Node 20 vs required Node 22 warning
- `npm run lint`: PASS with 3 pre-existing hook dependency warnings
- `npm run typecheck`: PASS
- `npm test`: PASS, 4 tests
- `npm run build`: PASS

## Smoke Tests

- Backend health: PASS on `http://localhost:5193/health`
- Swagger UI: PASS
- Swagger JSON: PASS
- OpenAPI title/Bearer/multipart markers: PASS
- Protected admin products without token: PASS, returned 401
- Public DB-backed catalogue/content endpoints: PASS, returned 200
- Protected admin endpoint without a token: PASS, returned 401
- Authenticated admin list endpoints for products, categories, collections, media, content, and settings: PASS, returned 200 with a short-lived development `SuperAdmin` token
- Browser route-guard smoke test: PASS in isolated headless Chrome; `/admin/products` redirected an anonymous session to the login screen and rendered normally
- Authenticated browser interaction test: NOT AUTOMATED because no Chrome DevTools/Playwright runtime is connected and the repository does not include a browser test dependency

## Known Limitations

- Collection edit page does not prefetch existing product IDs into the multi-select; API support exists.
- CMS content is structured text, not rich HTML. That is deliberate for XSS safety in Phase 3.
- Authenticated browser automation could not be completed because the isolated Chrome DevTools target was unavailable in this session. Real frontend login requests, API authorization, frontend compilation, and all admin APIs were verified independently; no authenticated-browser claim is made.

## Deferred Phase 4 Work

Storefront UX redesign, navigation redesign, product-card/PDP presentation, SEO presentation, and visual content consumption refinements.

## Deferred Phase 5 Work

End-to-end custom sticker builder, server pricing, proof generation, design cart/order workflow, and production workflow.

## Deferred Later Commerce Work

Payment gateway, shipping engine, coupons, webhooks, refunds, order fulfilment state machine, and checkout inventory reservation.

## Phase 4 Readiness

READY FOR PHASE 4. The Phase 3 migration is applied, backend and frontend builds/tests pass, DB-backed public endpoints pass, authorization is enforced, authenticated admin reads pass, and the anonymous admin browser guard renders correctly. Authenticated browser E2E coverage remains a documented test-infrastructure gap rather than a Phase 3 runtime blocker.

## PHASE 3 COMPLETION VERIFICATION

### Previous Partial-Completion State

The initial Phase 3 pass was marked partially complete because SQL Server rejected the encrypted provider connection, the migration could not be applied, and real database-backed authentication and mutation flows were not yet exercised. Those blockers are retained here as implementation history and were resolved during this completion pass.

### SQL Server Root Cause and Development Fix

- SQL Server service: default `MSSQLSERVER` instance, running locally.
- Effective server/database: `localhost` / `AmaaraCreationsDB`.
- Authentication: Windows integrated security.
- Provider stack: .NET 8, EF Core SQL Server 8.0.10, Microsoft.Data.SqlClient 5.1.5.
- Original exception: `Microsoft.Data.SqlClient.SqlException` reporting that SQL Server required encryption but the machine could not support the negotiated connection.
- Microsoft.Data.SqlClient 5.1.5 uses mandatory encrypted transport when `Encrypt` is omitted. `TrustServerCertificate=true` bypasses certificate-chain validation but does not disable TLS negotiation.
- Development-only solution: `Encrypt=false;TrustServerCertificate=true` in `appsettings.Development.json`.
- No user-secret or environment connection override was present, so Development appsettings was the effective source.
- Production configuration was not weakened.

| Safe connection field | Effective value |
|---|---|
| Server | `localhost` |
| Database | `AmaaraCreationsDB` |
| Trusted connection | `True` |
| Encrypt | `False` |
| TrustServerCertificate | `True` |

### Migration and Schema Verification

- Generated SQL was reviewed before completion. It contained only expected column/table/index/foreign-key additions and no `DROP`, `TRUNCATE`, or `DELETE` statements.
- `dotnet ef database update --configuration Release --no-build`: PASS; the final run reported the database already up to date.
- `__EFMigrationsHistory`: verified to contain `20261005103855_Phase3CatalogueMediaCms` and the preceding migration chain.
- Verified tables: `InventoryTransactions`, `CmsPages`, `CmsSections`, `SiteSettings`, `AuditLogs`, `Categories`, `Collections`, `Products`, `ProductVariants`, `ProductMedia`, and `Users`.
- Verified ProductMedia fields: `ContentType`, `CreatedByUserId`, `FileSize`, `Height`, `OriginalFileName`, `StorageProvider`, and `Width`.
- Existing Users, Products, Orders, and Identity schema were preserved.

### Development Admin and Authentication

- No Admin/SuperAdmin role assignment existed before completion verification.
- A strong development admin credential was configured through .NET user-secrets and the existing explicit bootstrap mechanism; no password was committed or printed.
- Bootstrap user/role results are now checked so Identity errors cannot be silently ignored.
- Real admin login: PASS, HTTP 200 with a short-lived JWT.
- Invalid password: PASS, HTTP 401.
- Unverified customer login: PASS, HTTP 401.
- Confirmed customer login: PASS, HTTP 200.
- Refresh rotation: PASS using an HttpOnly cookie scoped to `/api/v1/auth`.
- Refresh tokens were absent from response bodies, and credential/token values were absent from captured application logs.
- Temporary customer identities were removed after verification; the configured development SuperAdmin remains.

### Swagger and Authorization

Completion verification used isolated backend port 5196 because port 5192 was intermittently occupied during process retries:

- Swagger UI: `http://localhost:5196/swagger/index.html` - PASS.
- OpenAPI JSON: `http://localhost:5196/swagger/v1/swagger.json` - PASS.
- Bearer HTTP security definition: PASS.
- Multipart product-media request contract: PASS.
- `GET /api/v1/admin/products` without token: 401 PASS.
- The same endpoint with a real Customer token: 403 PASS.
- The same endpoint with the real SuperAdmin JWT returned by login: 200 PASS.

The authorized request used the same Bearer header defined by Swagger. Interactive clicking of Swagger's Authorize dialog was not automated, but its generated security contract and a real JWT request were both verified.

### Catalogue and Inventory Smoke Tests

- Products: list, pagination, search, category/status/featured filters, safe sort, create, details, edit, archive, and reactivate passed against SQL Server.
- ManageCatalog mutation policy: unauthenticated 401, Customer 403, SuperAdmin allowed.
- Categories: list, create, edit, generated slug uniqueness, hierarchy cycle rejection, archive, and reactivate passed.
- Collections: list, create, edit, duplicate assignment rejection, product assignment/removal, archive, and reactivate passed.
- Variants: list, create, edit with rowversion, SKU uniqueness, negative stock/price rejection, archive, and reactivate passed.
- Inventory: positive adjustment persisted previous quantity `6`, delta `3`, new quantity `9`, reason, admin actor, and timestamp; a negative-result adjustment was rejected.
- Verification products/categories/collections were archived after testing rather than hard-deleted.

### Media Smoke and Security Tests

- Valid 1x1 PNG upload: PASS.
- Generated portable `uploads/products/...` storage key: PASS; no absolute Windows path stored.
- Content type, byte size, width, height, original filename, storage provider, alt text, sort order, primary state, and admin actor persistence: PASS.
- Physical file creation and authenticated deletion: PASS; deletion now removes the stored file and repairs the product primary-image reference.
- MIME/extension/data mismatch rejection: PASS, HTTP 400.
- Corrupted PNG rejection: PASS, HTTP 400.
- Oversized upload rejection: PASS, HTTP 400.
- Excessive pixel dimensions and integer-overflow-safe pixel checks: PASS in focused backend tests.
- WebP dimensions are decoded for VP8, VP8L, and VP8X headers instead of storing a placeholder.

### CMS, Settings, and Audit Smoke Tests

- CMS admin list/details/create/edit, section persistence/order, publish, unpublish, published public retrieval, and draft 404 behavior passed.
- Site settings admin read/upsert passed.
- Public setting exposure and private setting exclusion passed.
- Secret-like setting key rejection passed with HTTP 400.
- Representative audit entries were verified for Product, Category, Collection, ProductVariant, ProductMedia, CmsPage, and SiteSetting mutations.
- Audit JSON was checked for passwords, refresh tokens, the development JWT signing key, and connection-string material; none was present.

### Public and Customer Regression Checks

- Public active categories: PASS.
- Public active collections: PASS.
- Public paginated product listing/details: PASS; inactive products are excluded and internal rowversion data is not exposed.
- Public reviews: PASS through the canonical `/api/v1/products/{id}/reviews` route after the routing cleanup.
- Customer `auth/me`, cart, wishlist, orders, and addresses reads: PASS with a real Customer JWT.

### Frontend and npm Verification

- No Amaara client Node process was holding `node_modules` before reinstall.
- `npm ci`: PASS; the previous esbuild failure was transient Windows file locking.
- Node 20.20.2 produced the expected warning because the project declares Node 22 or newer; installation still completed with zero reported vulnerabilities.
- Lint: PASS with three pre-existing `react-hooks/exhaustive-deps` warnings in Orders, Customers, and Reviews admin pages.
- Typecheck: PASS.
- Frontend tests: PASS, 4 tests.
- Production build: PASS.
- The frontend API base remains environment-driven through `VITE_API_BASE_URL`; no port-specific code was added.
- Previous isolated Chrome verification confirmed the anonymous `/admin/products` guard renders the login page. A completion-pass attempt to automate authenticated Chrome navigation could not obtain a DevTools target, so authenticated browser interaction remains unclaimed.

### Final Command Results

| Command | Result |
|---|---|
| `dotnet restore` | PASS |
| `dotnet build --configuration Release --no-restore` | PASS, 0 warnings / 0 errors |
| `dotnet test ..\tests\be.Tests\be.Tests.csproj --configuration Release --no-restore` | PASS, 12 tests |
| `dotnet ef migrations list --configuration Release --no-build` | PASS |
| `dotnet ef database update --configuration Release --no-build` | PASS, already up to date |
| `npm ci` | PASS |
| `npm run lint` | PASS, 0 errors / 3 pre-existing warnings |
| `npm run typecheck` | PASS |
| `npm test` | PASS, 4 tests |
| `npm run build` | PASS |

### Completion Status

Phase 3 is COMPLETE and READY FOR PHASE 4. No Phase 4 implementation was started.
