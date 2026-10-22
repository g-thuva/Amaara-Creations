param([ValidateSet('LKR', 'CHF', 'Both')][string]$Currency = 'Both', [string]$SqlServer = 'localhost')
$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot
$previousPath = $env:PATH
$nodeDirectory = Join-Path $repositoryPath '.runtime\node22\node-v22.23.4-win-x64'
if (Test-Path -LiteralPath (Join-Path $nodeDirectory 'node.exe')) { $env:PATH = $nodeDirectory + ';' + $env:PATH }
try {
    $currencies = if ($Currency -eq 'Both') { @('LKR', 'CHF') } else { @($Currency) }
    foreach ($testCurrency in $currencies) {
        # Start refuses occupied ports; it never takes over another local service.
        & (Join-Path $PSScriptRoot 'start-payment-simulator.ps1') -Currency $testCurrency -SqlServer $SqlServer -NoBrowser
        try {
            Push-Location (Join-Path $repositoryPath 'client')
            try {
                # Windows PowerShell 5 treats harmless native stderr warnings as
                # errors when redirected. The process exit code is authoritative.
                $ErrorActionPreference = 'Continue'
                try { & npm.cmd run test:e2e:simulator; $testExit = $LASTEXITCODE }
                finally { $ErrorActionPreference = 'Stop' }
                if ($testExit) { throw ('Simulator browser verification failed for ' + $testCurrency) }
            } finally { Pop-Location }
        } finally { & (Join-Path $PSScriptRoot 'stop-payment-simulator.ps1') }
    }
} finally { $env:PATH = $previousPath }
