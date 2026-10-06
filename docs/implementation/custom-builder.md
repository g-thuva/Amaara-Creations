# Custom Builder — Custom sticker builder

## 1. Scope and outcome

Custom Builder replaces the former browser-only custom sticker prototype with a persistent, server-backed workflow. Customers can load a published configuration, receive an authoritative quote, save and reopen a design, upload private artwork, add the design to the existing cart, create an order with an immutable snapshot, and review proof revisions. Authorised administrators can edit a draft builder configuration, test its price, publish a version, inspect ordered custom designs, retrieve artwork, upload proofs, and advance production state.

The implementation extends the existing authentication, cart, order, media, audit, and storefront foundations. It does not implement payment, shipping calculation, refunds, fulfilment integrations, or a broad visual redesign.

## 2. Repository state and preserved work

- Branch at implementation: `develop`.
- Starting commit: `925fe19 docs(storefront): record Storefront implementation`.
- The starting tree was intentionally dirty with the Storefront Revision 2 brand-continuity work. Those changes were preserved.
- Custom Builder remains uncommitted as requested by the implementation brief.

## 3. Phase 0–4 verification

The existing SQL Server connection, ASP.NET Core Identity/JWT/refresh-session flow, policy-based administration, media validation, catalogue, CMS, cart, order, wishlist, profile, address, and storefront route architecture were retained. No functional or security blocker prevented Custom Builder. The current Storefront presentation remains a functional design baseline and is not treated as the final Amaara visual system.

## 4. Baseline command record

Before Custom Builder changes:

| Check | Result |
| --- | --- |
| Backend restore | Passed |
| Backend Release build | Passed, 0 warnings and 0 errors |
| Backend tests | 14 passed |
| EF migration list | Passed through `20261005103855_CatalogueMediaCatalogueMediaCms` |
| `npm ci` | Passed; 252 packages, 0 vulnerabilities |
| Frontend lint | Passed |
| Frontend typecheck | Passed |
| Frontend tests | 31 passed |
| Frontend production build | Passed; main JS 346.27 kB, 111.08 kB gzip |

The installed Node runtime was 20.20.2 while `client/package.json` declares Node 22 or newer. npm also printed existing deprecation notices for transitive `inflight` and `glob` packages. These warnings did not fail the install or build.

## 5. Original custom builder audit

The original `/custom` route kept width, height, text, font, and preview state in React. Its documented dimension range was 2–30 cm. It had no persistent design record, active backend configuration, authoritative quote, private artwork upload, real cart line, ordered snapshot, saved-design area, proof workflow, or admin workflow. The preview was useful and has been retained in a server-integrated form. No competing prototype route remains.

## 6. Domain model

Custom Builder adds `CustomBuilderConfigurationVersion`, `CustomBuilderOption`, and `CustomDesignProofRevision`. It extends `CustomDesign`, `CustomDesignAsset`, `CartItem`, and `OrderItem`.

The principal enums are:

- `BuilderVersionStatus`: Draft, Published, Superseded.
- `BuilderOptionGroup`: Shape, Material, Finish, Font, Colour.
- `CustomDesignStatus`: Draft, Ready, InCart, Ordered, Archived.
- `ProofStatus`: NotRequired, Preparing, AwaitingApproval, ChangesRequested, Approved.
- `ProofRevisionStatus`: AwaitingApproval, ChangesRequested, Approved, Superseded.
- `ProductionStatus`: NotStarted, Queued, Printing, Finishing, QualityCheck, Ready.
- `CartItemType`: Product, CustomDesign.

## 7. Database entities and historical behavior

| Entity/table | Purpose | Key relationships | Historical/immutable? | Custom Builder change |
| --- | --- | --- | --- | --- |
| `CustomBuilderConfigurationVersions` | Versioned size, quantity, proof, artwork, currency, and pricing settings | Optional publishing user; one-to-many options | Published rows are immutable through the API | New |
| `CustomBuilderOptions` | Allowed customer choices and per-unit adjustments | Required configuration version | Preserved with its version | New |
| `CustomDesigns` | Customer-owned editable design and current quote/state | User, configuration version, assets, proof revisions | Locked after ordering; archived rather than erased | Extended |
| `CustomDesignAssets` | Private artwork and proof file metadata | Required design | Ordered artwork/proof assets are retained | Extended with dimensions |
| `CustomDesignProofRevisions` | Append-only proof decisions and messages | Required design and private asset; optional uploader | Revision history is retained | New |
| `CartItems` | Mixed standard-product and custom-design lines | Exactly one product or custom design; optional config version | Mutable cart state | Extended |
| `OrderItems` | Ordered product/custom line and immutable production snapshot | Order; optional product/custom design | Snapshot JSON is immutable | Extended |
| `AuditLogs` | Existing administrative audit trail | Actor and entity identifiers | Existing append-only record | Used for config, proof, and production actions |

## 8. Relationships, constraints, and indexes

Database checks enforce valid positive dimension/quantity ranges, non-negative configuration/option pricing, and the cart item discriminator. Restrictive foreign keys protect configuration versions, ordered designs, and proof assets from accidental cascading deletion. Configuration options cascade only with their owning configuration version.

Important indexes include unique configuration version numbers; configuration status/publish time; unique `(ConfigurationVersionId, Group, Code)` options; customer design lookup by `(UserId, UpdatedAt)`; unique `(CustomDesignId, RevisionNumber)` proofs; and filtered unique cart keys for product and custom-design lines. SQL verification found five relevant check constraints and ten indexes on the three new Custom Builder tables.

## 9. Builder configuration

The canonical server unit is centimetres. The development seed creates one published version and one draft only when no builder configuration exists and only in Development. Initial 2–30 cm dimensions preserve the prototype range. The seed also supplies editable quantity limits, shapes, material, finish, fonts, colours, artwork/proof flags, currency, and pricing fields.

The public API omits price rates, minimum line charge, option adjustments, row versions, and internal notes. Admin DTOs include these values. Draft edits do not affect customers. Publishing supersedes the previous published row and clones a new draft, preserving every previously referenced version.

## 10. Configuration validation

The server requires minimum values not to exceed maximum values, positive steps, positive quantity limits, non-negative rates/adjustments, a three-character currency code, unique option codes within each group, valid option groups, and non-empty option codes/labels. Quote requests must align exactly with configured steps and active option codes.

## 11. Pricing engine

`ICustomStickerPricingService` and `CustomStickerPricingService` provide the single authoritative implementation used by public quote, design create/update, cart addition, admin draft preview, and order creation.

The enabled calculation is intentionally small:

1. Validate the active configuration, dimensions, quantity, and selected active options.
2. Calculate area as `width × height` in cm².
3. Calculate the raw unit price as `area × price-per-square-unit + selected per-unit option adjustments`.
4. Apply the configured minimum line price by converting it to a minimum unit price for the requested quantity.
5. Round unit price and subtotal to two decimal places with `MidpointRounding.AwayFromZero`.
6. Multiply the accepted unit price by quantity.

All authoritative money uses `decimal`. The development currency is LKR. `PricingVersion` is `config-v{version}`. Shipping, tax, delivery, payment, supplier cost, margin, and other Phase 6 totals are excluded. The browser submits no accepted price.

**COMMERCIAL PRICING REQUIRES BUSINESS OWNER VERIFICATION.** The development seed rate and quantity bounds are test configuration, not approved commercial policy.

## 12. Customer design lifecycle

Implemented lifecycle:

```text
Draft/Ready → InCart → Ordered
     ↓          ↓
  Archived   removed from cart → Ready

Ordered → proof preparation/review → Approved
        → production NotStarted → Queued → Printing → Finishing → QualityCheck → Ready
```

An ordered or archived design cannot be edited. Removing a custom line from the cart restores the design to a reusable state. Archive is a soft lifecycle action. Row-version concurrency tokens prevent stale design/config updates.

## 13. Design schema and persistence

Custom design schema version 1 stores the selected option codes, dimensions, quantity, optional text/alignment, editor JSON, configuration version, latest authoritative unit/subtotal quote, pricing version, proof state, production state, and timestamps. Customers can list, retrieve, create, update, duplicate, archive, and reuse their own designs. Ownership is applied in every customer query; cross-customer resources return 404 to avoid confirming existence.

## 14. Artwork storage and validation

- Formats: JPEG, PNG, and WebP.
- Maximum request/file size: 5,242,880 bytes (5 MiB).
- Maximum decoded dimensions: 24,000,000 pixels.
- Validation: declared MIME type and extension must match binary signatures. PNG chunk layout/CRC/IEND, JPEG SOF dimensions/EOI, and WebP container/dimensions are parsed before persistence.
- Storage: `be/App_Data/private-media`, outside `wwwroot` and ignored by Git.
- Database exposure: only an opaque storage key is retained internally; public DTOs expose authorised API download routes.
- Customer access: owner-only bearer-authorised endpoints.
- Admin access: existing `ManageOrders` policy.
- Public URL generation explicitly rejects private keys.
- SVG and PDF are intentionally unsupported.

## 15. Private asset authorization

Artwork and proof bytes are never mounted by static-file middleware. `GET /api/v1/custom-designs/{id}/assets/{assetId}` permits only the owner or an authorised admin role and otherwise returns 404. Proof downloads use the same ownership/admin rule. Tests cover anonymous/cross-owner denial and successful authorised access.

## 16. Cart integration

`CartItemType` explicitly distinguishes product and custom-design rows. A database check requires exactly one corresponding foreign key. Custom lines keep the design ID, current configuration version, quantity, and a server price snapshot. The cart service reloads the current published version and recalculates custom prices; it never accepts a browser price. Standard product stock/price behavior remains in the same controller.

The storefront cart shows the design name, dimensions/unit, shape, material, finish, text, quantity, item subtotal, and an edit link. A mixed product/custom cart was exercised with disposable fixtures and cleaned afterward.

## 17. Order snapshot

Order creation recalculates each custom line again with the current published configuration and writes `CustomDesignSnapshotVersion = 1` plus `ConfigurationSnapshotJson`. Version 1 stores design schema/name, configuration ID/version, pricing version, unit, dimensions, quantity, option code/label pairs, custom text/alignment, currency, unit price, subtotal, artwork asset IDs, and capture time.

The linked design becomes Ordered and its artwork cannot be removed. Restrictive relationships retain source configuration/design/assets. A backend test and live API check confirmed that publishing/changing configuration does not alter an existing snapshot or its price.

## 18. Proof workflow

An admin upload always creates a new private asset and `CustomDesignProofRevision`. Any prior awaiting revision becomes Superseded. The customer can securely download the latest proof and either request changes with a comment or approve it. Only the latest AwaitingApproval revision accepts a response; stale revision requests return 409. Revision number, status, admin message, customer response, response time, uploader, and asset are preserved.

## 19. Production-status foundation

The admin may keep the current state or advance only through:

`NotStarted → Queued → Printing → Finishing → QualityCheck → Ready`.

Invalid jumps return 409. This foundation records production readiness without implementing fulfilment, inventory reservation, shipping, or customer delivery state.

## 20. Admin workflow

`/admin/custom-builder` edits the current draft, its limits, price inputs, requirements, and options; previews a server quote; publishes atomically; and displays history. `/admin/custom-designs` provides paginated search and proof/production filters. Its detail page shows customer/order context, the readable production specification, secure artwork download, proof upload/history/feedback, and production controls.

## 21. Custom Builder API routes

| Method | Route | Authorization | Purpose |
| --- | --- | --- | --- |
| GET | `/api/v1/custom-builder/config` | Anonymous | Safe published customer configuration |
| POST | `/api/v1/custom-builder/quote` | Anonymous, rate limited | Validate selections and return authoritative item quote |
| GET | `/api/v1/custom-designs` | Authenticated owner | Paginated saved designs |
| GET | `/api/v1/custom-designs/{id}` | Authenticated owner/admin | Design, assets, and proof history |
| POST | `/api/v1/custom-designs` | Authenticated | Create and price a design |
| PUT | `/api/v1/custom-designs/{id}` | Authenticated owner | Concurrency-safe edit and reprice |
| POST | `/api/v1/custom-designs/{id}/duplicate` | Authenticated owner | Clone into the active config |
| DELETE | `/api/v1/custom-designs/{id}` | Authenticated owner | Archive a reusable design |
| POST | `/api/v1/custom-designs/{id}/artwork` | Authenticated owner, rate limited | Validate and privately store artwork |
| GET | `/api/v1/custom-designs/{id}/assets/{assetId}` | Owner or authorised admin | Stream private artwork |
| DELETE | `/api/v1/custom-designs/{id}/assets/{assetId}` | Authenticated owner | Remove unlocked artwork |
| POST | `/api/v1/custom-designs/{id}/cart` | Authenticated owner | Reprice and persist a custom cart line |
| GET | `/api/v1/custom-designs/{id}/proofs/{revision}/file` | Owner or authorised admin | Stream private proof revision |
| POST | `/api/v1/custom-designs/{id}/proofs/request-changes` | Authenticated owner | Respond to latest proof |
| POST | `/api/v1/custom-designs/{id}/proofs/approve` | Authenticated owner | Approve latest proof |
| GET | `/api/v1/admin/custom-builder` | `ManageCatalog` | Draft, published version, and history |
| PUT | `/api/v1/admin/custom-builder/draft/{id}` | `ManageCatalog` | Validate and update draft |
| POST | `/api/v1/admin/custom-builder/draft/{id}/quote` | `ManageCatalog` | Test draft pricing |
| POST | `/api/v1/admin/custom-builder/draft/{id}/publish` | `ManageCatalog` | Publish and clone the next draft |
| GET | `/api/v1/admin/custom-designs` | `ManageOrders` | Paginated/searchable production queue |
| GET | `/api/v1/admin/custom-designs/{id}` | `ManageOrders` | Production/customer/order detail |
| POST | `/api/v1/admin/custom-designs/{id}/proofs` | `ManageOrders`, rate limited | Upload next private proof revision |
| PUT | `/api/v1/admin/custom-designs/{id}/production-status` | `ManageOrders` | Apply a valid production transition |

The existing `/api/v1/cart` and `/api/v1/orders` routes now accept mixed custom/product workflows without adding legacy aliases.

## 22. Swagger/OpenAPI

Swagger UI remains at `/swagger`; JSON remains at `/swagger/v1/swagger.json`. Runtime inspection found 19 Custom Builder path templates, including routes with multiple HTTP methods. Controller annotations expose the existing bearer security definition and policy-protected admin operations.

## 23. Database migration

Migration: `20261006165024_CustomBuilderCustomStickerWorkflow`.

The generated 493-line SQL was inspected before application. It adds the expected tables, columns, checks, indexes, and foreign keys. It contains no table/column drop or data deletion; the only drop/recreate work adjusts existing cart/order relationships and defaults needed for nullable mixed line types.

Before application, a verified copy-only SQL Server backup was created at:

`C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\AmaaraCreationsDB-pre-CustomBuilder-20261006-222218.bak`

`RESTORE VERIFYONLY` passed. The migration was applied to `AmaaraCreationsDB`; `__EFMigrationsHistory`, all three new tables, cart/order columns, five relevant checks, and indexes were verified. Existing rows remained present (including 19 products and 11 categories at verification time). Disposable smoke records and files were removed after testing.

## 24. Authorization

Public access is limited to safe configuration and quote operations. All design mutation/list operations require authentication and filter by the JWT subject. Admin configuration uses `ManageCatalog`; custom production uses `ManageOrders`. SuperAdmin inherits these policies through the existing policy definitions. Customer-supplied configuration IDs, option IDs, prices, ownership, proof status, and production transitions are not trusted.

## 25. IDOR prevention

Backend tests exercise another customer's design GET, PUT, archive, cart addition, artwork access, and proof response. These return 404. Stale proof approval returns 409. Anonymous private-artwork access returned 401 in live verification.

## 26. Audit logging

Meaningful admin operations record the authenticated actor and request context through the existing audit service:

- draft configuration update;
- configuration publish;
- proof upload/revision;
- production-state transition.

Customer design and proof state remain attributable through ownership, proof response fields, and timestamps.

## 27. Builder frontend

`/custom` loads the active API configuration and requests a debounced server quote with cancellation/race protection. The UI renders configured numeric constraints and option groups, text/alignment, a functional proportional preview, artwork input, item subtotal, save, and save/add-to-cart actions. A signed-out draft is held only for sign-in continuity; accepted designs and prices always come from the API. `/custom/{id}` reloads persisted state after refresh and carries row-version concurrency updates through artwork changes.

## 28. Saved designs frontend

`/account/designs` lists persisted designs with price/state, reopens `/custom/{id}`, duplicates, adds to cart, and archives eligible designs. `/account/designs/{id}/proof` displays the latest proof, authenticated download, decision controls, and revision history.

## 29. Cart, checkout, and order presentation

Cart and checkout render either standard product data or custom design specification from the same response. Order detail uses a versioned snapshot adapter, displays the captured configuration instead of current builder state, and links ordered custom designs to proof review.

## 30. Backend tests

The final backend suite contains 20 passing tests and no failures. Custom Builder coverage includes exact decimal pricing/rounding, invalid range/option validation, cross-customer IDOR behavior, private storage paths and file validation, stale proof rejection, and preservation of a version-1 order snapshot after configuration changes.

## 31. Frontend tests

Vitest contains 34 passing tests across five files. Custom Builder unit coverage verifies option grouping, numeric request normalization with no browser price field, and versioned snapshot parsing. The live Playwright Custom Builder suite adds three passing journeys: save/reload/private artwork/cart, required-width responsive operation, and authorised builder/production admin screens.

## 32. Runtime smoke tests

The local Release API verified health, public config privacy, Swagger paths, quote, design create/reload/edit/duplicate/archive, private artwork upload and authorised/anonymous access, persistent custom cart, order and snapshot, admin draft save/test/publish, historical snapshot stability, admin queue/detail, proof revision 1, customer change request, revision 2, stale approval rejection, latest approval, admin-visible approval, and production queue transition.

The final successful workflow used a disposable development sequence and returned PASS for all 17 assertions. A separate disposable account/product regression verified catalogue/product, authentication, profile, addresses, orders, wishlist, standard cart, mixed cart, and existing Products/Categories/Collections/Media/CMS/Settings/Orders/Customers/Reviews admin APIs. Fixtures and private files were cleaned afterward.

## 33. Responsive and accessibility checks

The live builder passed without horizontal overflow at 375, 430, 768, 1024, and 1440 px. Inputs remained editable and the upload control remained operable. The public suite also passed at 320, 390, and 1280 px.

Controls use associated labels; quote/error/notification state uses status/alert semantics; preview uses a named live region; file input exposes accepted formats; links and native controls remain keyboard accessible; existing mobile navigation/filter dialogs retain focus trapping and Escape restoration. Proof actions are explicit buttons with a labelled comment field. No custom builder modal was introduced.

## 34. Existing functionality regression

The ten-test public Playwright suite passed direct routes, mobile keyboard navigation, CMS carousel controls/reduced motion, and responsive home/catalogue/custom/auth/content layouts. Disposable live API checks passed standard and mixed cart, wishlist, authentication, profile, addresses, orders, catalogue, product, and all listed existing admin resource endpoints. The absent published home CMS document correctly exercised the storefront's not-found/empty fallback.

## 35. Known limitations and owner decisions

- Commercial price-per-area, minimum line price, option adjustments, 2–30 cm bounds, 1–100 quantity bounds, material/finish choices, artwork requirement, and proof requirement require owner approval.
- JPEG/PNG/WebP and the 5 MiB/24 MP limits require production confirmation. SVG, PDF, vector editing, crop/filter, background removal, and DPI/preflight analysis are deferred.
- Local private storage is appropriate for current development/single-node use. Production needs durable private object storage or an equivalent shared encrypted volume and backup/retention policy.
- Proof email/SMS/WhatsApp notifications are not implemented.
- Production status is operational state only; it does not reserve inventory or dispatch a courier.
- The current Node installation emits the repository's Node 22 engine warning even though all commands pass.

## 36. Phase 6 handoff

Phase 6 can consume standard and custom cart lines, server-authoritative item prices, existing addresses, orders, and immutable design snapshots. Phase 6 owns shipping/delivery totals, payment provider integration, coupons if approved, webhooks/idempotency, inventory reservation, refund behavior, and the final order/shipping lifecycle.

## 37. Future UI/UX redesign boundary

The Custom Builder implementation prioritizes domain correctness and end-to-end functionality. Current visual styling is not considered the final Amaara design.

A future UI/UX pass may freely redesign the builder layout, stepper, option cards, preview layout, saved-design cards, proof presentation, and admin custom-design presentation, provided it preserves API contracts, server pricing, domain validation, design persistence, cart integration, order snapshots, asset authorization, and the proof state machine.

## 38. Files added, modified, and removed

Custom Builder added:

- `be/Controllers/AdminCustomBuilderController.cs`
- `be/Controllers/AdminCustomDesignsController.cs`
- `be/Controllers/CustomBuilderController.cs`
- `be/Controllers/CustomDesignsController.cs`
- `be/DTOs/CustomBuilder/CustomBuilderDtos.cs`
- `be/Data/ApplicationDbContext.CustomBuilder.cs`
- `be/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.cs`
- `be/Migrations/20261006165024_CustomBuilderCustomStickerWorkflow.Designer.cs`
- `be/Models/CustomBuilderModels.cs`
- `be/Services/CustomBuilderMappings.cs`
- `be/Services/CustomStickerPricingService.cs`
- `be/Services/CustomBuilderDevelopmentSeeder.cs`
- `client/e2e/CustomBuilder.spec.ts`
- `client/src/pages/ProofReview.jsx`
- `client/src/pages/SavedDesigns.jsx`
- `client/src/pages/admin/CustomBuilderConfig.jsx`
- `client/src/pages/admin/CustomDesignDetail.jsx`
- `client/src/pages/admin/CustomDesigns.jsx`
- `client/src/pages/CustomBuilder.test.jsx`
- `client/src/services/customBuilderApi.js`
- `client/src/utils/customBuilder.js`
- `tests/be.Tests/CustomBuilderCustomBuilderTests.cs`
- this document.

Repository hygiene also stops ignoring EF migration designer metadata, so the existing Foundation and Catalogue Media designer files are now visible for inclusion with the Custom Builder change. These files contain the `[Migration]` metadata needed for a clean checkout to discover those migrations.

Custom Builder modified the existing cart/order controllers and DTOs, DbContext/snapshot, custom design/cart/order models, media storage abstraction/service, API registration/startup, storefront routes/navigation/styles/cart/checkout/order detail, backend storage tests/project dependencies, `.gitignore`, and `README.md`.

Custom Builder removed no files. File removals visible in the shared working tree belong to the preserved Storefront Revision 2 stylesheet/component consolidation.
