# AWS S3 setup and local development

Read [the audit](STORAGE_AUDIT.md), [security](S3_SECURITY.md) and [production guide](PRODUCTION_DEPLOYMENT.md). No resources were created by this implementation. Region defaults to `ap-south-1`; owners must approve their region, account, costs and recovery/retention windows.

## Provisioning (owner approval required)

The reviewable [CloudFormation template](../../deploy/storage/storage.cloudformation.yaml) creates two versioned, encrypted, bucket-owner-enforced buckets with all Block Public Access settings enabled, HTTPS-only bucket policies, a public CloudFront distribution with OAC, and a prefix-scoped application managed policy. It retains buckets on stack removal/replacement. The 90-day noncurrent-version expiry is an example requiring retention approval; active versions are not expired. Private media is never an origin of the public distribution.

Use unique stacks/buckets per environment: `amaara-dev-public-<suffix>`, `amaara-dev-private-<suffix>` and separate production equivalents. Review a CloudFormation change set before execution; creating a stack/distribution can incur charges. Do not use website endpoints, public-read ACLs, or account/root access keys. Attach the application policy to the selected execution role/SSO permission set; deployment permissions belong to a separate operator identity.

The configuration and policies support the four default prefixes. If prefixes are customized, change both the application settings and infrastructure/IAM allowlists together. No listing, ACL, tagging, multipart or bucket-administration permissions are required by the application. It uses bounded single-object uploads, not multipart. Administrative monitoring/verification uses a separate operator role.

## Windows development

Install AWS CLI v2 from the [official Windows installer](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html). This was not installed or configured automatically.

```powershell
aws configure sso --profile amaara-dev
aws sso login --profile amaara-dev
$env:AWS_PROFILE = 'amaara-dev'
aws sts get-caller-identity
```

Verify the returned account/role matches the approved **development** resources. Do not paste access keys or SSO tokens into chat, JSON, Vite variables or Git. The SDK uses its [default credential/profile chain](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/creds-assign.html); `AWSSDK.SSO` and `AWSSDK.SSOOIDC` are included for [Identity Center profiles](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/creds-idc.html). Environment temporary credentials and workload/container/instance roles are also supported. No explicit static-credential constructor is used.

Merge the `Storage` section of [the development example](../../be/appsettings.S3.Development.example.json) into your existing, ignored `appsettings.Development.Local.json` (or `appsettings.Development.json`). **Do not overwrite SQL/JWT/CORS/email configuration.** The example is not loaded automatically and has no credentials. Replace the bucket names and restart the API after changing provider/bucket/delivery settings. `Storage:Provider=Local` is the default when neither new nor legacy provider configuration exists; the old `Uploads:Provider` is a compatibility alias. S3 failures never select Local.

`Storage:Limits` bounds each category to at most the current 5 MiB endpoint/global limit. `Uploads:MaxImagePixels` retains the decoder's 24M-pixel ceiling. Smaller category limits work with both providers. Expiry is validated at 15–300 seconds but reserved for future signed downloads; **current private access is streamed on every authorized request and does not issue signed URLs**.

From `C:\Nexaura`:

```powershell
dotnet restore be/be.csproj
dotnet ef migrations script --idempotent --project be/be.csproj --output .runtime/storage-upgrade.sql
# Review/backup/apply the additive cleanup-work migration using the release runbook.
Set-Location C:\Nexaura\be
dotnet run --launch-profile be
# Separate terminal:
Set-Location C:\Nexaura\client
npm ci
npm run dev
```

Use the existing authenticated profile/product/artwork/proof forms. An avatar/product returns `/api/v1/media/public/<generated-key>`. In ApiProxy mode the backend streams PNG from the private public-media bucket; the browser needs neither AWS credentials nor S3 CORS. Private upload responses keep the existing `/custom-designs/.../assets/...` or proof-file URLs.

After an upload, copy the **public** generated key from the response or read private reference from authorized test metadata. Check only that approved object:

```powershell
aws s3api head-object --bucket APPROVED_DEV_PUBLIC_BUCKET --key avatars/REPLACE_GENERATED_GUID.png --region ap-south-1
aws s3api get-object --bucket APPROVED_DEV_PUBLIC_BUCKET --key avatars/REPLACE_GENERATED_GUID.png --region ap-south-1 .runtime/verified-avatar.png
```

Expect `image/png`, encryption `AES256`, actual normalized length, checksum metadata and public immutable cache control. Avoid logging private keys/URLs or customer filenames. After profile save and refresh, the same application URL must still work. See [the test matrix](S3_TESTING.md) for ownership and live-resource checks.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Missing credentials / 503 storage unavailable | Set `AWS_PROFILE` in the API process's environment; verify the process identity/profile, not just another terminal. Remove stale environment access keys that override the intended profile. |
| Expired SSO | Run `aws sso login --profile amaara-dev` again, then retry. Never copy token cache contents. |
| Wrong region / bucket / AccessDenied | Check approved account/region, exact bucket names and four object-prefix grants; bucket policies, SCPs and explicit denies can override identity policy. |
| Missing object | A known 404 maps to media-not-found. Without `ListBucket`, S3 may return 403 for a missing key; the app safely reports storage unavailable instead of treating denied access as proof of absence. |
| Broken image URL | Check API origin/Vite media base and proxy route; CloudFront mode requires an HTTPS CDN origin and a deployed OAC/bucket policy. |
| Wrong MIME / image rejection | Extension, declared MIME and decoded bytes must agree. Images are normalized to PNG; SVG/PDF are not supported by existing workflows. |
| SQL queue table missing | Apply the reviewed additive migration first; no S3 bytes are uploaded without a persisted reservation. |
| CORS | Allow React's exact origin at the API. There is no browser-to-S3 upload, so bucket upload CORS is unnecessary. CDN canvas reads require the approved storefront origin response header. |

GitHub Pages can serve the built React files; it cannot host ASP.NET Core or hold AWS credentials. Run the API on an appropriate server/workload platform with its own secure configuration.
