# S3 testing and acceptance evidence

No approved AWS buckets/profile/account were supplied, and AWS CLI is not installed in the execution environment. **No live AWS integration, bucket policy, OAC distribution, cost, SQL backup/restore or production migration is claimed as verified.** Mocked S3 and local disposable API tests establish application behavior only.

## Automated checks

`S3MediaStorageTests` uses an official SDK client subclass with an in-memory object store. It checks avatar/public and artwork/proof/private uploads, PII-free keys, normalization, checksums/encryption/cache headers, streaming/disposal, immutable conditional writes, verified import retries, concurrent upload uniqueness, idempotent deletion, malformed/unsupported/oversized/scanner-rejected input, missing objects, failure responses and strict traversal/tier validation.

`S3MediaApiTests` exercises the actual controllers, validation, Identity/MFA login, policies, services and EF test store with mocked S3. It checks anonymous denial, customer isolation before S3 reads, insufficient staff denial, correct staff access, staff-only proofs, customer proof ownership, public avatar upload/save/refresh/read and reference-aware cleanup. No test bypasses the customer ownership service by guessing an object key.

`MediaMigrationTests` covers inventory/correlation/orphans, deterministic import and rerun, remote-byte verification, idempotent conditional field changes, metadata rollback, changed-source rejection and traversal refusal. It uses EF's in-memory store; SQL Server transaction/backup rehearsal is a separate required owner test. The additive migration is generated from the current model; historical migrations are unchanged. The mutable snapshot and API baseline add only the intended new queue/public-read route.

Frontend tests verify application public URLs/CDN URLs resolve and private `s3:` references are rejected. Profile uploads preserve portable URLs; the existing crop/loading/error handling remains and byte-upload progress is announced separately from server processing. Existing catalogue/gallery and private blob workflows use the same HTTP contracts.

Development commands:

```powershell
dotnet test tests/be.Tests/be.Tests.csproj
dotnet build tools/MediaMigration/MediaMigration.csproj
Set-Location client
npm test
npm run lint
npm run typecheck
npm run build
npm run build:admin
npm run check:isolation
```

The implementation session uses `.runtime/s3-build` / `.runtime/s3-final-build` for .NET outputs because the owner's running local API locks its normal executable. It does not stop/reconfigure that process. Tests that require explicitly configured disposable SQL/SMTP/load resources remain skipped when those settings are absent.

Measured results on 23/10/2026:

| Check | Result |
| --- | --- |
| Full .NET suite | **403 passed, 18 skipped, 0 failed** (421 total); skipped SQL/SMTP/load integrations need their separately configured disposable resources |
| Frontend Vitest | **282 passed** in 35 files |
| Mobile avatar browser flow | **Passed** at 320px: cancel, rejected-upload retry, in-dialog upload/processing status, portable profile payload, refreshed 512×512 image |
| ESLint / TypeScript | Passed |
| Storefront / admin Vite builds | Passed |
| Storefront isolation | Passed (38 manifest entries, 39 assets) |
| CloudFormation | `cfn-lint 1.58.0` passed locally; no account deployment/change set was executed |
| EF model | No pending model changes; idempotent SQL upgrade generated and reviewed, not applied |
| Configured local SQL/media audit | Loopback-only read-only inventory: 3 files, 2 referenced, 0 missing, 1 retained orphan; both referenced files passed validation-only dry-run |
| Secret scan | Passed; no candidates in non-ignored repository text |

Logs/scripts are under `.runtime/s3-final-tests.log`, `.runtime/s3-storefront-build.log`, `.runtime/s3-admin-build.log` and `.runtime/s3-media-upgrade.sql`. The backend API tests execute Identity/MFA/authorization with mocked object storage and an isolated EF store; they do not certify real S3/SQL Server infrastructure.

## Owner-approved live verification

Use separate non-production buckets/role and disposable accounts/designs/products. Record expected account, region, bucket names, profile and cleanup/retention approval first. Do not create resources or test destructive operations against production. Use a separate operator profile for bucket-policy checks; the runtime IAM policy intentionally cannot administer/inspect all bucket settings.

1. `aws sts get-caller-identity`; confirm the approved account/role. Check Block Public Access, ownership controls, encryption, versioning, bucket-policy status and the exact public OAC SourceArn using `s3api get-public-access-block`, `get-bucket-ownership-controls`, `get-bucket-encryption`, `get-bucket-versioning`, `get-bucket-policy-status` and `get-bucket-policy`. Inspect effective IAM/SCP permissions too.
2. Run the configured application with the approved profile. Upload a test avatar and product image through existing authenticated UI/API routes; confirm S3 keys, normalized length/SHA-256, MIME, encryption and application image display after refresh. Test replacement and ensure the old referenced image survives a failed save. Confirm progress/error/retry on desktop/mobile.
3. Upload customer artwork and an authorized staff proof. Verify customer A and correct MFA-assured staff can read them; anonymous, customer B and insufficient staff cannot. Confirm wrong design/asset combinations do not reach storage. Verify customer cannot upload staff proofs. Verify no private object URL is returned by the public route.
4. Using an unauthenticated HTTP client, GET exact known public-bucket and private-bucket **S3 REST URLs**; expect access denied for both. Public CloudFront URL should succeed while private-bucket content is never present in the distribution. Do not log confidential private key URLs in shared evidence.
5. With the runtime role, attempt GET of an operator-created test object in a designated **unapproved test bucket**; expect denied. Do not probe unrelated real buckets. Use IAM policy simulation/effective-policy review as additional evidence.
6. Interrupt a test upload/read and simulate a database failure after upload. Confirm no success UI on failure and the durable reservation is retained. Exercise cleanup with a short approved test schedule; verify live SQL references and order snapshots prevent deletion and retries recover missing/unreferenced test objects.
7. Run inventory/dry-run/backup/apply against a disposable **SQL Server** database and originals. Interrupt after upload, rerun without duplicate objects, verify every remote checksum/length, and check SQL references/metadata only change after verification. Reopen previews, exercise ownership/proofs, then rollback and verify original references and bytes. Do not delete originals as part of this test.
8. Inspect both production builds for AWS keys/token strings and private configuration, validate the CloudFormation template/change set, configure alerts/readiness canaries, and record operator approval before production.

Current private reads use backend streaming, so signed-URL expiry and direct-upload CORS tests are not applicable. If that architecture changes later, add authorization-before-signing, exact-key/type/size/checksum grants, short expiry, quarantine/completion and expired-grant tests before enabling it.
