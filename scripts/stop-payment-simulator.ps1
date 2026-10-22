$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot
$sessionPath = Join-Path $repositoryPath '.runtime\local-payments\session.json'
if (-not (Test-Path -LiteralPath $sessionPath)) { Write-Host 'No simulator session is recorded.'; return }
$session = Get-Content -LiteralPath $sessionPath -Raw | ConvertFrom-Json
foreach ($entry in @(@{ Name = 'Api'; Match = '*be.PaymentSimulator.dll*--local-payments*' }, @{ Name = 'Storefront'; Match = '*vite.js*--mode simulator*--port 5188*' }, @{ Name = 'Management'; Match = '*vite.js*--mode admin*--port 5189*' })) {
    $processId = [int]$session.($entry.Name)
    $ownedProcess = Get-CimInstance Win32_Process -Filter ('ProcessId=' + $processId)
    if (-not $ownedProcess) { continue }
    if ($ownedProcess.CommandLine -notlike $entry.Match -or $ownedProcess.CommandLine -notlike ('*' + $repositoryPath + '*')) { throw ('Refusing to stop a process that no longer matches this simulator: ' + $entry.Name) }
    Stop-Process -Id $processId -ErrorAction Stop
}
Write-Host ('Simulator stopped. SQL records retained in ' + $session.Database + '. The business API, storefront and database were not stopped or changed.')
