# Storage audit — 23/10/2026

Scope: the local working tree in `C:\Nexaura`; existing dashboard edits are unrelated and preserved. No production database, credentials or cloud objects were inspected or changed for this audit.

## Current entry points and persistence

| Flow | Entry / owner | Current storage and database references |
| --- | --- | --- |
| Avatar | `UploadController`, authenticated `POST /api/v1/upload/avatar`; account/profile update is separate | `IAvatarStorage` / `LocalAvatarStorage`; normalized PNG in `wwwroot/uploads/avatars/{userId}_{guid}.png`; `AspNetUsers.AvatarUrl` stores a URL after profile save |
| Product/gallery | authorized catalogue upload in `AdminProductsController` / `ProductMediaService`; `AdminMediaController` only lists/reads metadata | `IMediaStorageService.UploadAsync`; `wwwroot/uploads/products/yyyy/MM/guid.png`; `ProductMedia` has provider, key, URL, MIME, size, dimensions, original filename, product/user relationships and sort/primary fields; primary URL copied to `Products.ImageUrl` |
| Artwork | `CustomDesignsController`, authenticated upload; `CustomDesignService.OwnDesign` and design-state checks | `UploadPrivateAsync`; `App_Data/private-media`; `CustomDesignAssets.StorageKey`, MIME, size, dimensions and design FK; API responses contain protected application download routes |
| Proof | `AdminCustomDesignsController`, `ManageOrders` policy and staff assurance; `ProductionService.UploadProof` | same private storage, `CustomDesignAsset` plus `CustomDesignProofRevision`; revision/approval state and notifications commit in a commerce transaction |
| Private reads | `GET /api/v1/custom-designs/{id}/assets/{assetId}` and `/{id}/proofs/{revision}/file` | resolve media through database, verify design/asset association, then ownership or `ManageOrders`; do not accept keys or buckets as authorization |

The task's `/artwork/{artworkId}/content` route is not registered in this checkout. Keep the actual `/assets/{assetId}` route and the actual staff `/proofs` upload route; do not break existing clients.

## Validation, authorization and frontend

- `ImageUploadValidator` verifies extension, declared MIME, signatures/structure and bounded actual input bytes. Current default is 5 MiB / 24 million pixels. Avatars also accept GIF; product/artwork/proof uploads accept JPEG, PNG and WebP. PDF, SVG and arbitrary original print documents are **not** currently supported. Do not widen the accepted formats as part of storage migration.
- `SafeRasterImage` fully decodes with SkiaSharp, limits concurrent decodes to two, applies EXIF orientation, re-encodes a metadata-free PNG and bounds output bytes. It preserves pixel dimensions; no thumbnail/downscale pipeline exists. Existing uploads do not retain an original alongside a preview. Do not claim archival preservation of originals that were already normalized before this change.
- `HttpUploadScanner` uses a configured private service; required scanning fails closed. Scanning occurs before persistence. No S3 object may bypass that boundary on upload/import.
- Public local images are served anonymously by `UseStaticFiles`; private keys start `private/` and resolve outside web root. `LocalMediaStorageService` checks traversal/root containment. Legacy private areas flatten slashes (for example `custom-artwork-42` on Windows).
- `StaffAuthorizationHandler`/policies enforce current staff permissions and management assurance; private service access reuses `IAuthorizationService`, rather than checking role names alone.
- React `resolveMediaUrl`, `Media`, account avatar crop/upload hooks, `ProductGallery`, admin product/media tables, cart/checkout images and order image snapshots consume application media URLs. Artwork/proof previews fetch protected blobs with the existing HTTP/auth client. No browser AWS client is needed.
- CSS thumbnails reuse uploaded images; there is no generated thumbnail store. Seed/media URLs may be external and must not be treated as local files or imported automatically.

## Deletion, replacement and other consumers

Product/gallery and artwork removal save SQL changes before deleting bytes, but have no durable deletion retry. Uploads write bytes before SQL, with no compensation for a failed SQL save. Proof upload is transactional in SQL only. Avatar upload returns a URL before profile save and leaves old/unclaimed files behind. Replacement uses a new random filename and must keep the previous referenced object until the database change succeeds.

Search covered storage abstractions/DI, controllers, application services, Identity/account/privacy export, catalogue queries, dashboard product images, order image snapshots, migrations/model configuration, seeds, notifications/email, background maintenance, frontend URL/blob consumers and deployment/release scripts. Existing commerce maintenance retries payments/refunds and notifications, not media. Email links users to authenticated proof pages; private file URLs are not embedded. SQL backup/release rehearsal retains private-media files separately from the database. Deployment currently assumes persistent local mounts and creates media directories unconditionally.

## Required implementation

1. Add Options-validated Local/S3 selection, one managed AWS SDK client, random PII-free public keys and separate fixed public/private buckets.
2. Preserve existing validation/scanning and private authorization. Stream protected S3 reads; never mint permanent private URLs.
3. Store durable logical `s3:public/...` / `s3:private/...` references in existing 500-character key columns. Existing `ProductMedia.StorageProvider` records S3; the private key's explicit tier/provider discriminator avoids a duplicate media table or destructive media schema change.
4. Return stable application public URLs, with API proxy delivery in development and redirect to configured CloudFront/OAC in production. Delivery settings stay independent of private access.
5. Add a small durable **cleanup work queue**, not a second media catalogue, for upload reservations and deletion retries. Check live SQL references before deleting abandoned bytes. Apply its additive migration through the normal reviewed release process.
6. Restrict legacy local reads to an explicit transition option; S3 failures must not trigger disk fallback. Stop creating persistent upload directories in S3-only deployments.
7. Provide inventory/dry-run/import/rollback tooling. Back up DB and bytes, validate transformed bytes and remote checksum/length before conditional SQL reference changes, preserve original files, report missing/unreferenced/external references, and never silently migrate production.

Live AWS verification requires owner-approved non-production buckets/profile; no AWS CLI or approved test resource identifiers were supplied in the task. Cloud account policy, OAC and anonymous-access checks cannot be certified by mocked tests.

## Read-only local inventory evidence

The migration utility was run with `--environment Development --require-loopback` against the configured local SQL Server. It inventoried **3 local files**, correlated **2 referenced files**, reported **0 missing references** and **0 manual-review items**, and identified **1 unreferenced file** which was retained. Validation-only dry-run decoded/checked both referenced files successfully. The private manifest is `.runtime/storage-audit/current-local.json`; its contents are not committed because it holds local media references. No database references, image files, S3 resources or application storage settings were changed by inventory/dry-run. No SQL backup/restore or S3 migration was executed.
