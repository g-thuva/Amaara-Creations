# API Routing v1 Cleanup

## Problem Observed

Swagger exposed duplicate application routes such as `/api/Auth/login` and `/api/v1/Auth/login`, plus mixed legacy routes for reviews, orders, uploads, and health.

## Root Cause

The duplication came from controller-level route attributes that registered the same controller twice:

- `[Route("api/[controller]")]`
- `[Route("api/v1/[controller]")]`

Additional inconsistencies came from absolute legacy review routes such as `/api/products/{productId}/reviews` and `/api/admin/reviews`, a second auth refresh action route `/refresh-token`, both POST and PUT default-address actions, legacy order admin routes under `/orders/admin/*`, legacy product mutations on public product routes, legacy product-image upload, and `/api/v1/health`.

No ASP.NET API versioning package generated these routes. `MapControllers()` was registered once. Swagger was accurately showing the duplicated backend endpoint registrations.

## Strategy

- Canonical application API base: `/api/v1`
- Public/customer routes: lowercase resource names
- Administration routes: `/api/v1/admin/*`
- Infrastructure health: `/health`
- Backward compatibility: removed during rebuild after migrating frontend consumers
- Database impact: no schema migration required

## Changed Routes

| Old route | New canonical route | Action | Notes |
|---|---|---|---|
| `/api/Auth/login` | `/api/v1/auth/login` | Removed/replaced | Frontend already uses v1 base URL |
| `/api/v1/Auth/login` | `/api/v1/auth/login` | Replaced | Explicit lowercase route |
| `/api/Auth/refresh-token` | `/api/v1/auth/refresh` | Removed | Legacy alias removed; HttpOnly refresh-cookie flow retained |
| `/api/v1/auth/refresh-token` | `/api/v1/auth/refresh` | Removed | Single refresh implementation |
| `POST /api/v1/addresses/{id}/default` | `PUT /api/v1/addresses/{id}/default` | Removed | PUT is canonical state-setting action |
| `/api/Cart` | `/api/v1/cart` | Removed/replaced | Explicit lowercase route |
| `/api/Wishlist` | `/api/v1/wishlist` | Removed/replaced | Explicit lowercase route |
| `/api/Orders` | `/api/v1/orders` | Removed/replaced | Explicit lowercase customer route |
| `/api/v1/orders/admin/all` | `/api/v1/admin/orders` | Migrated | Admin order boundary standardized |
| `/api/v1/orders/admin/{id}` | `/api/v1/admin/orders/{id}` | Migrated | Admin order boundary standardized |
| `/api/v1/orders/{id}/status` | `/api/v1/admin/orders/{id}/status` | Migrated | Admin mutation no longer looks customer-scoped |
| `/api/Products` | `/api/v1/products` | Removed/replaced | Public reads only |
| `POST /api/v1/products` | `/api/v1/admin/products` | Removed/replaced | Catalogue Media admin product API is source of truth |
| `PUT /api/v1/products/{id}` | `/api/v1/admin/products/{id}` | Removed/replaced | Catalogue Media admin product API is source of truth |
| `DELETE /api/v1/products/{id}` | `/api/v1/admin/products/{id}/archive` | Removed/replaced | Archive/reactivate replaces public-route mutation |
| `/api/products/{productId}/reviews` | `/api/v1/products/{productId}/reviews` | Migrated | Public/customer review route versioned |
| `/api/admin/reviews` | `/api/v1/admin/reviews` | Migrated | Admin reviews versioned |
| `/api/admin/reviews/{id}` | `/api/v1/admin/reviews/{id}` | Migrated | Admin reviews versioned |
| `/api/admin/reviews/stats` | `/api/v1/admin/reviews/stats` | Migrated | Admin reviews versioned |
| `/api/upload/product-image` | `/api/v1/admin/products/{id}/media` | Removed/replaced | Catalogue Media media API is source of truth |
| `/api/v1/upload/product-image` | `/api/v1/admin/products/{id}/media` | Removed/replaced | Stale frontend service removed |
| `/api/v1/health` | `/health` | Removed | Health is infrastructure, version-neutral |

## Frontend Changes

- `addressApi.setDefault` now uses `PUT /addresses/{id}/default`.
- `orderApi` admin calls now use `/admin/orders`, `/admin/orders/{id}`, and `/admin/orders/{id}/status`.
- `productApi` admin helpers now target `/admin/products`; delete maps to archive.
- Legacy `uploadApi.uploadProductImage` was removed. Product media upload is through `adminApi.uploadProductMedia`.

## Swagger Changes

Swagger remains one document: `Amaara Creations API v1`.

Verified URLs:

- Swagger UI: `http://localhost:5192/swagger/index.html`
- OpenAPI JSON: `http://localhost:5192/swagger/v1/swagger.json`

OpenAPI verification:

- Path count: 78
- Unversioned `/api/*` application paths: 0
- `/api/v1/health`: absent
- `/health`: present
- `/api/v1/auth/refresh`: present once
- `/api/v1/auth/refresh-token`: absent
- Bearer security scheme: present
- Conflicting method/path operations: none observed

Schemas remain generated DTO/component definitions and were not removed.

## Final Endpoint List

### Auth

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/confirm-email`
- `POST /api/v1/auth/resend-confirmation`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/logout-all`
- `GET /api/v1/auth/me`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/auth/change-password`

### Users and Addresses

- `GET /api/v1/users/profile`
- `PUT /api/v1/users/profile`
- `GET /api/v1/users/profile/stats`
- `GET /api/v1/users/profile/avatar`
- `POST /api/v1/users/profile/avatar`
- `GET /api/v1/addresses`
- `POST /api/v1/addresses`
- `GET /api/v1/addresses/{id}`
- `PUT /api/v1/addresses/{id}`
- `DELETE /api/v1/addresses/{id}`
- `PUT /api/v1/addresses/{id}/default`

### Products, Catalogue, Reviews, Media

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- `GET /api/v1/products/{productId}/reviews`
- `POST /api/v1/products/{productId}/reviews`
- `PUT /api/v1/reviews/{id}`
- `DELETE /api/v1/reviews/{id}`
- `GET /api/v1/catalog/categories`
- `GET /api/v1/catalog/collections`
- `POST /api/v1/upload/avatar`

### Cart, Wishlist, Orders

- `GET /api/v1/cart`
- `POST /api/v1/cart`
- `DELETE /api/v1/cart`
- `PUT /api/v1/cart/{itemId}`
- `DELETE /api/v1/cart/{itemId}`
- `GET /api/v1/wishlist`
- `POST /api/v1/wishlist`
- `DELETE /api/v1/wishlist`
- `DELETE /api/v1/wishlist/{productId}`
- `POST /api/v1/wishlist/{productId}/cart`
- `GET /api/v1/orders`
- `POST /api/v1/orders`
- `GET /api/v1/orders/{id}`

### Admin

- `GET /api/v1/admin/dashboard`
- `GET /api/v1/admin/dashboard/revenue`
- `GET /api/v1/admin/dashboard/orders`
- `GET /api/v1/admin/dashboard/products`
- `GET /api/v1/admin/dashboard/recent-orders`
- `GET /api/v1/admin/customers`
- `GET /api/v1/admin/customers/{id}`
- `GET /api/v1/admin/customers/{id}/orders`
- `GET /api/v1/admin/customers/{id}/stats`
- `GET /api/v1/admin/orders`
- `GET /api/v1/admin/orders/{id}`
- `PUT /api/v1/admin/orders/{id}/status`
- `GET /api/v1/admin/reviews`
- `DELETE /api/v1/admin/reviews/{id}`
- `GET /api/v1/admin/reviews/stats`
- `GET /api/v1/admin/products`
- `GET /api/v1/admin/products/{id}`
- `POST /api/v1/admin/products`
- `PUT /api/v1/admin/products/{id}`
- `POST /api/v1/admin/products/{id}/archive`
- `POST /api/v1/admin/products/{id}/reactivate`
- `GET /api/v1/admin/products/{id}/variants`
- `POST /api/v1/admin/products/{id}/variants`
- `PUT /api/v1/admin/products/{productId}/variants/{variantId}`
- `POST /api/v1/admin/products/{productId}/variants/{variantId}/archive`
- `POST /api/v1/admin/products/{productId}/variants/{variantId}/reactivate`
- `POST /api/v1/admin/products/{id}/stock-adjustments`
- `GET /api/v1/admin/products/{id}/media`
- `POST /api/v1/admin/products/{id}/media`
- `PUT /api/v1/admin/products/{productId}/media/{mediaId}`
- `DELETE /api/v1/admin/products/{productId}/media/{mediaId}`
- `GET /api/v1/admin/media`
- `GET /api/v1/admin/media/{id}`
- `GET /api/v1/admin/categories`
- `GET /api/v1/admin/categories/{id}`
- `POST /api/v1/admin/categories`
- `PUT /api/v1/admin/categories/{id}`
- `POST /api/v1/admin/categories/{id}/archive`
- `POST /api/v1/admin/categories/{id}/reactivate`
- `GET /api/v1/admin/collections`
- `GET /api/v1/admin/collections/{id}`
- `POST /api/v1/admin/collections`
- `PUT /api/v1/admin/collections/{id}`
- `POST /api/v1/admin/collections/{id}/archive`
- `POST /api/v1/admin/collections/{id}/reactivate`
- `PUT /api/v1/admin/collections/{id}/products`
- `DELETE /api/v1/admin/collections/{collectionId}/products/{productId}`
- `GET /api/v1/admin/content/pages`
- `GET /api/v1/admin/content/pages/{id}`
- `POST /api/v1/admin/content/pages`
- `PUT /api/v1/admin/content/pages/{id}`
- `POST /api/v1/admin/content/pages/{id}/publish`
- `POST /api/v1/admin/content/pages/{id}/unpublish`
- `GET /api/v1/admin/settings`
- `PUT /api/v1/admin/settings/{key}`

### Public Content and Infrastructure

- `GET /api/v1/content/pages/{slug}`
- `GET /api/v1/content/settings`
- `GET /health`

## Tests Added

- `tests/be.Tests/ApiRouteContractTests.cs`

The test asserts no controller uses `[controller]` route tokens, no unversioned application controller routes remain, `/refresh-token` is absent, legacy admin order/review/upload routes are absent, and key canonical routes are registered once.

## Verification

Baseline:

- `dotnet restore`: PASS
- `dotnet build`: PASS
- `dotnet test`: PASS
- `dotnet ef migrations list`: PASS with sandbox DB status limitation
- `npm ci`: PASS outside sandbox, Node 20 warning
- `npm run lint`: PASS with 3 pre-existing hook warnings
- `npm run typecheck`: PASS
- `npm test`: PASS
- `npm run build`: PASS

Final:

- `dotnet restore`: PASS
- `dotnet build`: PASS, 0 warnings / 0 errors
- `dotnet test ..\tests\be.Tests\be.Tests.csproj --configuration Release --no-restore`: PASS, 14 tests
- `dotnet ef migrations list --configuration Release --no-build`: PASS
- `npm ci`: PASS, Node 20 warning, 0 vulnerabilities
- `npm run lint`: PASS with 3 pre-existing hook warnings
- `npm run typecheck`: PASS
- `npm test`: PASS, 4 tests
- `npm run build`: PASS

Runtime:

- Backend: PASS on `http://localhost:5192`
- Frontend: PASS on `http://127.0.0.1:5173/Amaara-Creations/`
- Swagger UI: PASS
- OpenAPI JSON: PASS
- Health `/health`: PASS
- Representative legacy routes: 404 for `/api/Auth/login`, `/api/Products`, `/api/Cart`, `/api/Wishlist`, `/api/Orders`, `/api/v1/health`, `/api/v1/auth/refresh-token`, `/api/v1/upload/product-image`, `/api/v1/orders/admin/all`, and `/api/admin/reviews`
- Address default: PUT 200; old POST action 405; one default address remains
- Auth: login 200, `/auth/me` 200, refresh 200, no raw refresh token returned
- Admin without token: 401
- Customer token against `/api/v1/admin/orders`: 403
- Admin dashboard with admin token: 200
- Customer reads for `/auth/me`, addresses, cart, wishlist, and orders: 200
- Public products/catalogue: 200
- Temporary product public reviews: 200

## Remaining Issues

- The frontend still has three pre-existing `react-hooks/exhaustive-deps` warnings in admin pages.
- Local Node is v20.20.2 while the client declares Node `>=22`; npm commands pass with `EBADENGINE` warnings.
- Older historical planning docs still contain pre-v1 route examples by design and now point to this cleanup document for the current contract.

## Status

ROUTING CLEANUP COMPLETE - SAFE TO CONTINUE Catalogue Media.
