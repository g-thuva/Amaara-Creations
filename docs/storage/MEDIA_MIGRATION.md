# Local media migration and rollback

Use the offline [`tools/MediaMigration`](../../tools/MediaMigration/Program.cs) utility. No public migration endpoint exists. No local originals, real database rows or AWS objects were migrated by the implementation session.

The utility inventories `wwwroot/uploads/**` and `App_Data/private-media/**`, correlates product/media/user/order snapshot and private asset references, reports missing/unreferenced files and flags embedded CMS/settings content for manual review. Unmanaged/external URLs are not imported automatically. Set `Storage:LegacyPublicOrigins` to exact **approved old API origins** to recognize origin-qualified legacy URLs; an arbitrary URL is never fetched. Inventory does not follow reparse points/symlinks. Protect manifests/reports/backups outside web root: they contain private media references and SQL rollback values.

## Stages

1. Back up the current release/configuration and arrange a maintenance window. Pause uploads, profile/media changes and cleanup workers. Keep an immutable copy of the local originals. Inventory reads only files and database metadata; do not infer which database is safe from the working directory.
2. Configure the target dev/staging S3 buckets and profile. Generate an inventory plan. Review every missing file, unmanaged reference and CMS embedded URL before cutover. Orphan files remain local; they are not published or deleted.
3. Run dry-run. It checks source checksums, decodes/re-encodes and runs the configured scanner without uploading or changing SQL references. Unsupported/oversized legacy formats require review; limits are not bypassed.
4. Create a full SQL `BACKUP DATABASE` using `COPY_ONLY, CHECKSUM` through the existing backup runbook, into a new file. The `backup` command runs `RESTORE VERIFYONLY WITH CHECKSUM` and inspects headers for a single full checksum backup of the selected database. The file must be accessible locally and at the same server path. For a remote SQL Server, use the existing operator backup/restore process and stage a verified copy/path deliberately; do not pretend a local path is automatically visible to that server.
5. `backup` archives originals and records SHA-256 fingerprints of the SQL backup and file ZIP in the plan. It refuses an existing ZIP destination and changed inventory sources. The ZIP contains private artwork: encrypt it with the approved backup system and restrict its ACLs. The CLI does not invent backup credentials or encryption keys.
6. Review/apply the additive `MediaCleanupWork` EF migration through the release process. It creates only a recovery queue, not replacement media tables. The tool refuses reference writes with pending migrations.
7. Apply only after owner review. Each file uses a deterministic planned key, conditional create (`If-None-Match: *`), full remote GET/length/SHA-256 verification, then a serializable SQL transaction with before/after value checks. A retried already-committed row is a no-op. A changed row causes a conflict instead of overwriting a concurrent edit. Each file's verified metadata is saved to the manifest before SQL commit; an interrupted process can resume safely.
8. Verify avatar refresh, public thumbnails/gallery/cart/order snapshots, saved design previews, staff proofs, customer ownership isolation and actual CloudFront delivery. Private API URLs keep design/asset IDs; they never become public. Run the approved live-resource test matrix.
9. Enable `Storage:Provider=S3` with `LegacyLocalReadsEnabled=true` during the controlled overlap. Local keys explicitly route to local reads; S3 errors do not fall back. Resolve all inventory/manual-review gaps and verify old references before disabling this flag. Only then remove public/private media volume requirements from that deployment. Do not delete original files automatically.

Example commands from `C:\Nexaura` (replace private paths/environment deliberately):

```powershell
$env:AWS_PROFILE = 'amaara-dev'
dotnet run --project tools/MediaMigration -- inventory --content-root C:\Nexaura\be --environment Development --manifest C:\Nexaura\.runtime\storage-migration\plan.json
dotnet run --project tools/MediaMigration -- dry-run --content-root C:\Nexaura\be --environment Development --manifest C:\Nexaura\.runtime\storage-migration\plan.json
dotnet run --project tools/MediaMigration -- backup --content-root C:\Nexaura\be --environment Development --manifest C:\Nexaura\.runtime\storage-migration\plan.json --files-backup C:\Nexaura\.runtime\storage-migration\originals.zip --sql-backup C:\PrivateBackups\approved-copy-only.bak
dotnet run --project tools/MediaMigration -- apply --content-root C:\Nexaura\be --environment Development --manifest C:\Nexaura\.runtime\storage-migration\plan.json --execute --maintenance-window
```

`inventory` refuses to overwrite an existing plan. Repeat `apply` using the same manifest to resume; do not regenerate it after partial SQL changes. The target database fingerprint, buckets and region must match. Changed originals/checksums or unverified backups block reference writes. Missing/manual-review items also block apply unless the operator explicitly supplies `--acknowledge-review-items` after reviewing the exceptions. Exit code 2 means per-file failures remain; the manifest records statuses and exception types without dumping SQL/provider credentials. Exit 0 does **not** certify missing/manual-review/orphan files are resolved; read the summary and manifest.

Current source formats are normalized again on import to ensure old bytes meet the current security boundary. The manifest records both original-file SHA-256 and actual normalized remote SHA-256. SQL MIME/length/dimensions are updated with the reference and captured for rollback. Original print files outside current supported image formats are never silently converted, downscaled or published.

## Rollback

Pause writers/cleanup, retain the queue and S3 recovery versions, and use the same protected manifest/backups:

```powershell
dotnet run --project tools/MediaMigration -- rollback --content-root C:\Nexaura\be --environment Development --manifest C:\Nexaura\.runtime\storage-migration\plan.json --execute --maintenance-window
```

Rollback verifies original files still match and reverses only values matching the manifest's applied state; concurrent changes are refused. It restores provider, URL/key, MIME, size and dimensions. It does not delete S3 copies or local originals. Rollback of newer post-cutover uploads needs separate reconciliation; changing the provider alone cannot make S3-only bytes local. After reference rollback and verification, choose Local explicitly and restore durable mounts. Production Local rollback also requires `Storage:AllowLocalInProduction=true`. Keep the additive queue table/migration in place; do not downgrade a live schema merely to change storage.

Production reference writes additionally require `--approve-production` **after explicit owner approval**. These switches document deliberate operator actions; they are not authorization to run this tool on production during development.

The SQL transaction/backup procedure must be rehearsed on an approved disposable SQL Server database. Current automated migration tests use an isolated EF in-memory store and mocked S3; they validate planning, references, checksums, repeatability and rollback logic, not SQL Server's backup service or distributed crash behavior.
