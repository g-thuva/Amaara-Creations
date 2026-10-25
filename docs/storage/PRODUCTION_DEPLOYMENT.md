# Production media deployment and operations

This is a deployment design and reviewable configuration, not a record of a production deployment. The owner must approve resources, account, IAM identity, hostname, retention, monitoring, costs and migration before provisioning/cutover.

Use separate production public/private buckets, with bucket-owner-enforced ownership, ACLs disabled, Block Public Access, SSE-S3, versioning and HTTPS-only bucket policies. The [CloudFormation template](../../deploy/storage/storage.cloudformation.yaml) supplies these and a public CloudFront/OAC distribution; review its change set and 90-day recovery example first. CloudFront's S3 **REST origin** uses always-signed OAC requests. The private bucket is never an origin or public policy resource. A private media API cannot be replaced with an unguarded S3 redirect.

Attach the prefix-scoped application policy to the workload execution role (EC2/ECS/EKS/OIDC or the host's equivalent supported temporary identity). Do not deploy AWS keys in appsettings, service definitions or Vite. A dedicated operator role provisions CloudFront/bucket policy/monitoring and migration verification; the application has no such administration permissions.

Merge [the S3 production overrides](../../deploy/storage/appsettings.S3.Production.example.json) or equivalent environment/secret-store settings deliberately:

```text
Storage__Provider=S3
Storage__AllowLocalInProduction=false
Storage__LegacyLocalReadsEnabled=false
Storage__S3__Region=ap-south-1
Storage__S3__PublicBucket=<approved-production-public-bucket>
Storage__S3__PrivateBucket=<approved-production-private-bucket>
Storage__S3__PublicDelivery=CloudFront
Storage__S3__PublicBaseUrl=https://<distribution-domain>
Storage__S3__ReadinessPublicKey=s3:public/products/<yyyy>/<MM>/<verified-canary-guid>.png
Storage__S3__ReadinessPrivateKey=s3:private/custom-proofs/<test-design-id>/<yyyy>/<MM>/<verified-canary-guid>.png
```

Canaries must exist in the approved buckets and use valid generated-key grammar. Pin/retain those objects independently of abandoned-upload cleanup; do not use a temporary unclaimed avatar reservation as a permanent readiness canary. `/health/ready` checks SQL connectivity, pending schema migrations and configured S3 canary HEAD reads with cancellation. Production requires both canaries; development may omit them, in which case readiness does not establish S3 connectivity. Readiness does not prove Put/Delete permissions, OAC, backups or scanner health: run the explicit acceptance tests too. Health failures contain no bucket names/credentials. `/health/live` stays a process liveness check.

S3-only deployments no longer create persistent upload directories. Data-protection keys still require the existing persistent/private key ring and SQL still requires normal backup/retention. Do not remove those. Existing local-media nginx aliases/systemd media mounts in the older release templates apply to Local or the explicit transition only; remove aliases after verified migration, keep `/api/v1/media/public/*` proxied to ASP.NET, and never expose `App_Data`. Coordinate the reverse proxy's body limit with the existing 5 MiB file/multipart overhead and the API's route limits. TLS, approved CORS origins and staff assurance remain required.

Public SQL references use stable application URLs, so changing the public CDN hostname does not require rewriting media rows. The endpoint returns a short-lived cached redirect to the configured CDN; object bytes cache immutably under new keys. Select a custom CDN hostname/certificate if needed, use HTTPS/TLS policy, configure the approved public-origin CORS response for canvas use and review response headers. Cache only GET/HEAD image responses; never proxy authenticated customer artwork through this distribution. Emergency public deletion can require explicit invalidation; it cannot retract already downloaded images.

Monitor S3/CloudFront 4xx/5xx, request latency, storage/transfer cost, scanner rejections/outages, API storage 503s, cleanup backlog/oldest item and retry counts. Enable appropriate CloudTrail data events/server access logging into a separate restricted destination with approved retention; these have cost and privacy implications. Configure AWS Budgets/alerts with owner-approved amounts and contacts. Do not put confidential object keys into broad-access monitoring dashboards. The application logs cleanup work IDs and exception types, not customer filenames/credentials.

Review current-object retention separately from the example noncurrent-version lifecycle. DeleteObject in a versioned bucket creates a recoverable deletion marker; physical version deletion belongs to the approved recovery/retention policy, not the runtime role. Coordinate DB backup windows, original/S3 object versions and key-ring recovery. Run a recurring isolated restore drill of both tiers, DB references and authorization; versioning alone is not a complete backup.

Before cutover: apply the additive schema upgrade, exercise approved test resources, complete [migration](MEDIA_MIGRATION.md), verify public/private read paths and ownership, deploy the immutable API/frontend builds and inspect readiness. Rollback requires the release/configuration backup, preserved originals, manifest and reconciliation of any new S3-only uploads. Follow the migration rollback steps; no deployment, bucket creation or production migration is authorized merely by the presence of these templates.

The former production example assumes local persistent media. Do not reuse it unchanged for S3: the new startup validator requires S3 in production unless an explicit durable Local rollback is selected. These storage overrides are independent of the existing payment, SMTP, JWT, database and management-origin settings.
