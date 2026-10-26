# Genuine Zahls TEST setup on Windows

Use the normal application and your own registered customer account. Runtime
simulation and automatic fixture sign-in have been removed. LIVE is unsupported.

## Current verified readiness

| Prerequisite | Result on 27 October 2026 |
|---|---|
| Merchant instance and API key | Private values preserved; real authenticated `SignatureCheck/` and `PaymentProvider/` GETs succeeded. |
| Provider | PayPal is listed, but both its method arrays are empty. No available method verified. |
| Provider eligibility / genuine TEST mode | Not verified. Confirm in Zahls' merchant dashboard/provider configuration. |
| CHF acceptance | Not verified for this provider. |
| Optional method restriction | `PaymentMethods: []` is valid and omits `pm`. No identifiers were invented. |
| Gateway POST and hosted checkout | Blocked pending genuine provider readiness. |
| Webhook ingress | Real temporary Cloudflare ingress tested: unrelated route 404, missing/invalid signature 401. |
| Merchant webhook registration | Not verified; must register the full callback URL in Zahls. |
| Signing key | Existing value preserved privately; correspondence to the registered merchant webhook is unverified. |
| Provider-supported payment/refund details | Not supplied/verified. |

In Zahls, open the existing PayPal provider configuration and confirm its sandbox
connection and eligibility. Check the enabled methods, not just the provider's
presence. Ask Zahls support why no active methods are returned if the dashboard
still claims the provider is ready. Obtain confirmation of CHF support and real
TEST details. Do not activate live acquiring for this task.

## Configuration

The shared files are sanitized. `be/appsettings.json` and
`be/appsettings.Development.Local.example.json` contain:

```json
{
  "Zahls": {
    "Enabled": true,
    "Mode": "TEST",
    "ApiBaseUrl": "https://api.zahls.ch",
    "ApiVersion": "v1.16",
    "InstanceName": "",
    "ApiKey": "",
    "WebhookSigningKey": "",
    "Currency": "CHF",
    "FrontendBaseUrl": "http://localhost:5173/Amaara-Creations",
    "WebhookUrl": "",
    "PaymentMethods": [],
    "TestProviderConfigured": false,
    "ChfSupportedByTestProvider": false,
    "LookAndFeelProfile": "",
    "TimeoutSeconds": 15
  },
  "Datatrans": { "Enabled": false }
}
```

`Enabled` permits validation; it does not assert readiness. Existing private
merchant values were migrated to the project's .NET User Secrets store,
`amaara-creations-be-dev`. Original values are backed up alongside it in a
private `zahls-preserved-*.json` file. Do not commit or share that directory.
User Secrets are local development storage, not a production secret vault.

For new credentials, edit this private file without placing key values in shell
history or a frontend file:

```powershell
Set-Location C:\Nexaura
$secretFile = Join-Path $env:APPDATA 'Microsoft\UserSecrets\amaara-creations-be-dev\secrets.json'
notepad $secretFile
```

Keep unrelated entries. Use flat keys `Zahls:InstanceName`, `Zahls:ApiKey`,
`Zahls:WebhookSigningKey` and later `Zahls:WebhookUrl`. Keep
`Zahls:TestProviderConfigured` and `Zahls:ChfSupportedByTestProvider` false until
the corresponding real merchant facts are verified. Retain `Zahls:Mode` = `TEST`.
The optional `appsettings.Development.Local.json` is loaded last by `Program.cs`;
ensure it does not override these keys with obsolete values. Restart the API
after editing User Secrets.

Read-only API check, which never changes flags or creates payments:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-zahls-test-readiness.ps1
```

Normal `client/.env.development.local` values (merge without deleting unrelated
entries):

```env
VITE_API_BASE_URL=http://localhost:5192/api/v1
VITE_MEDIA_BASE_URL=http://localhost:5192
VITE_MANAGEMENT_LOGIN_URL=http://localhost:5179/Amaara-Creations/login
```

Use `localhost` consistently for the normal app and cookies. Reload old browser
tabs after retiring the simulator. Old synthetic sessions belonged to a separate
database and are not valid ordinary customer credentials.

## Start the actual application

Use separate PowerShell terminals. Keep the existing SQL connection, media/AWS
settings and Identity configuration. Do not migrate or replace a business
database as part of payment testing.

```powershell
# API: launchSettings.json selects Development and http://localhost:5192.
Set-Location C:\Nexaura
dotnet run --project .\be\be.csproj --launch-profile be
```

```powershell
# Storefront: http://localhost:5173/Amaara-Creations/
Set-Location C:\Nexaura
npm.cmd --prefix client run dev -- --host localhost --port 5173 --strictPort
```

```powershell
# Optional staff application; normal staff login still requires MFA.
Set-Location C:\Nexaura
npm.cmd --prefix client run dev:admin -- --host localhost --strictPort
```

Use Node 22+. On this machine the existing portable runtime can be selected with
`$env:PATH = 'C:\Nexaura\.runtime\node22\node-v22.23.4-win-x64;' + $env:PATH`.
Stop an already running instance of the same component before starting another;
do not kill unrelated port owners. At task completion the normal API and existing
storefront are running on 5192 and 5173.

## Webhook-only temporary public HTTPS URL

```powershell
# Terminal 1: fixed proxy forwards only the signed Zahls callback to API 5192.
Set-Location C:\Nexaura
node .\tests\webhook-tunnel-proxy.mjs
```

```powershell
# Terminal 2: official binary downloaded and digest-verified during this task.
Set-Location C:\Nexaura
.\.runtime\cloudflared.exe tunnel --url http://127.0.0.1:5199 --no-autoupdate
```

Copy the printed `https://….trycloudflare.com` address. In Zahls merchant
**Webhooks**, register that address plus
`/api/v1/payments/zahls/webhook`. Select JSON transaction notifications, enable
retries, and store that webhook's signing key privately. Set `Zahls:WebhookUrl`
to the identical complete URL. Do not append tokens/query parameters or tunnel
5192 directly. GET requests to the callback intentionally do not return success.

```powershell
$webhookOrigin = Read-Host 'Paste the printed HTTPS trycloudflare origin (no path)'
# Expected 404: the API/Swagger is not exposed.
curl.exe -i "$webhookOrigin/swagger/v1/swagger.json"
# Expected 401: unsigned requests cannot confirm payments.
curl.exe -i -X POST "$webhookOrigin/api/v1/payments/zahls/webhook" -H 'Content-Type: application/json' --data '{}'
```

Cloudflare Quick Tunnels need no account or purchased domain and their hostname
changes on restart. Update both Zahls and the private backend URL each time.
Inspect Zahls' webhook delivery history/logs for response codes and retry status;
a browser return is not evidence of delivery. Do not enable interactive email
access restrictions on a provider webhook. Stop the tunnel and proxy with Ctrl+C
when finished. The verification tunnel used for this task was stopped afterward.
[Official Quick Tunnel guide](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/),
[Zahls webhook guide](https://www.zahls.ch/en/developers/guides/webhook).

## Genuine external acceptance after merchant prerequisites are fixed

Record the merchant TEST-mode/method/CHF evidence before setting the two
readiness flags to true. Sign in normally, add a CHF product, select a valid
address and actual configured delivery option, then click Pay securely. Inspect
the genuine Zahls-hosted URL returned by the API. Complete only the provider's
approved TEST instrument. Confirm signed webhook delivery and the persisted
payment/order before accepting the confirmation screen.

Test decline, cancel, pending, safe retry and delayed reconciliation. Replay the
same genuine notification from Zahls delivery logs and compare event count,
stock and order finalization. Test partial/full refunds through authorized staff
only if the provider supports them. Record real gateway/transaction/refund IDs
privately. No amount should be inferred from frontend values or converted from
historical LKR data.
