param()
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
[xml]$project = Get-Content -LiteralPath (Join-Path $repository 'be/be.csproj') -Raw
$secretId = [string]$project.Project.PropertyGroup.UserSecretsId
$secretPath = Join-Path $env:APPDATA ('Microsoft/UserSecrets/' + $secretId + '/secrets.json')
if (-not (Test-Path -LiteralPath $secretPath)) { throw 'Configure the local .NET User Secrets store first; see docs/payments/ZAHLS_TEST_SETUP.md.' }
$settings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
function Read-ZahlsSecret([string]$name) {
    $flat = $settings.PSObject.Properties['Zahls:' + $name]
    if ($flat) { return $flat.Value }
    if ($settings.Zahls) { return $settings.Zahls.$name }
    return $null
}
$instance = Read-ZahlsSecret 'InstanceName'
$apiKey = Read-ZahlsSecret 'ApiKey'
if ([string]::IsNullOrWhiteSpace($apiKey) -or $instance -notmatch '^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$') {
    throw 'A valid instance slug and private API key are required. No request sent.'
}
# Deliberately fixed destination: never send a private key to a configurable host.
# GET only. This script cannot create Gateway sessions, change flags or issue refunds.
$headers = @{ 'X-API-KEY' = $apiKey; Accept = 'application/json' }
foreach ($endpoint in @('SignatureCheck/', 'PaymentProvider/')) {
    try {
        $response = Invoke-RestMethod -Method Get -Uri ('https://api.zahls.ch/v1.16/' + $endpoint + '?instance=' + [Uri]::EscapeDataString($instance)) -Headers $headers -TimeoutSec 20
        if ($response.status -ne 'success') { Write-Output ($endpoint + ': unsuccessful envelope'); continue }
        Write-Output ($endpoint + ': authenticated success')
        if ($endpoint -eq 'PaymentProvider/') {
            foreach ($provider in $response.data) {
                [pscustomobject]@{
                    Provider = $provider.name
                    Methods = @($provider.paymentMethods).Count
                    ActiveMethods = @($provider.activePaymentMethods).Count
                }
            }
        }
    }
    catch {
        # Provider bodies and exception text can contain sensitive merchant details.
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'network failure' }
        Write-Output ($endpoint + ': ' + $status)
        exit 1
    }
}
Write-Output 'An accepted key or listed provider does not verify TEST mode, eligibility or CHF support. Confirm these in Zahls before changing readiness flags.'
