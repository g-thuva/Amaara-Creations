param(
    [ValidateSet('LKR', 'CHF')][string]$Currency = 'LKR',
    [string]$SqlServer = 'localhost',
    [switch]$NoBrowser
)
$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot
$runtimePath = Join-Path $repositoryPath '.runtime\local-payments'
$buildPath = Join-Path $repositoryPath ('.runtime\payment-simulator-build\' + [Guid]::NewGuid().ToString('N'))
$nodeExecutable = Join-Path $repositoryPath '.runtime\node22\node-v22.23.4-win-x64\node.exe'
if (-not (Test-Path -LiteralPath $nodeExecutable)) { $nodeExecutable = (Get-Command node -ErrorAction Stop).Source }
$nodeMajor = [int]((& $nodeExecutable --version) -replace '^v(\d+).*$', '$1')
if ($nodeMajor -lt 22) { throw 'Install Node.js 22+ before starting the payment simulator.' }
if ($SqlServer -notmatch '^(localhost|127\.0\.0\.1|\.|\(localdb\)\\MSSQLLocalDB)(\\[A-Za-z0-9_]+)?$') { throw 'Only a loopback SQL Server is allowed. A database connection string is not accepted.' }
$occupied = @(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object LocalPort -In 5208, 5188, 5189)
if ($occupied.Count) { throw 'Simulator ports 5208, 5188 or 5189 are occupied. Stop the existing lab with scripts\stop-payment-simulator.ps1; do not stop unrelated processes.' }
New-Item -ItemType Directory -Path $runtimePath -Force | Out-Null
$previousPath = $env:PATH
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$previousFlag = $env:AMAARA_LOCAL_PAYMENT_SIMULATOR
$previousApi = $env:VITE_API_BASE_URL
$previousMedia = $env:VITE_MEDIA_BASE_URL
$previousPublic = $env:VITE_PUBLIC_SITE_URL
$previousManagement = $env:VITE_MANAGEMENT_LOGIN_URL
$ownedProcesses = @()
try {
    $env:PATH = (Split-Path -Parent $nodeExecutable) + ';' + $env:PATH
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryPath 'client\node_modules\vite\bin\vite.js'))) {
        & npm.cmd --prefix (Join-Path $repositoryPath 'client') ci
        if ($LASTEXITCODE) { throw 'Frontend dependency installation failed.' }
    }
    # Isolated outputs avoid overwriting any running local business API binary.
    & dotnet build (Join-Path $repositoryPath 'tests\be.PaymentSimulator\be.PaymentSimulator.csproj') --configuration Debug --artifacts-path $buildPath --verbosity minimal
    if ($LASTEXITCODE) { throw 'Simulator build failed; no database or service was started.' }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'; $env:DOTNET_ENVIRONMENT = 'Development'; $env:AMAARA_LOCAL_PAYMENT_SIMULATOR = '1'
    $simulatorAssembly = Join-Path $buildPath 'bin\be.PaymentSimulator\debug\be.PaymentSimulator.dll'
    $apiProcess = Start-Process -FilePath dotnet -ArgumentList @(('"' + $simulatorAssembly + '"'), '--local-payments', '--currency', $Currency, '--sql-server', $SqlServer, '--state-root', ('"' + $runtimePath + '"')) -WorkingDirectory $repositoryPath -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimePath 'api.log') -RedirectStandardError (Join-Path $runtimePath 'api.error.log') -PassThru
    $ownedProcesses += $apiProcess
    $health = $null
    for ($attempt = 0; $attempt -lt 120; $attempt++) {
        if ($apiProcess.HasExited) { throw 'Simulator API startup failed. See .runtime\local-payments\api.error.log. No existing database was selected.' }
        try { $health = Invoke-RestMethod -Uri 'http://127.0.0.1:5208/simulator/health' -TimeoutSec 2; break } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $health.ready -or -not $health.simulated -or $health.database -notmatch '^AmaaraPhase6Tests_LocalPayments_(LKR|CHF)_[a-f0-9]{32}$') { throw 'Simulator did not report a guarded disposable SQL database.' }
    $env:VITE_API_BASE_URL = 'http://127.0.0.1:5208/api/v1'; $env:VITE_MEDIA_BASE_URL = 'http://127.0.0.1:5208'
    $env:VITE_PUBLIC_SITE_URL = 'http://127.0.0.1:5188/Amaara-Creations'; $env:VITE_MANAGEMENT_LOGIN_URL = 'http://127.0.0.1:5189/Amaara-Creations/login'
    $viteEntry = Join-Path $repositoryPath 'client\node_modules\vite\bin\vite.js'
    $shopProcess = Start-Process -FilePath $nodeExecutable -ArgumentList @(('"' + $viteEntry + '"'), '--mode', 'simulator', '--host', '127.0.0.1', '--port', '5188', '--strictPort') -WorkingDirectory (Join-Path $repositoryPath 'client') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimePath 'storefront.log') -RedirectStandardError (Join-Path $runtimePath 'storefront.error.log') -PassThru
    $ownedProcesses += $shopProcess
    $staffProcess = Start-Process -FilePath $nodeExecutable -ArgumentList @(('"' + $viteEntry + '"'), '--mode', 'admin', '--host', '127.0.0.1', '--port', '5189', '--strictPort') -WorkingDirectory (Join-Path $repositoryPath 'client') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimePath 'management.log') -RedirectStandardError (Join-Path $runtimePath 'management.error.log') -PassThru
    $ownedProcesses += $staffProcess
    @{ Api = $apiProcess.Id; Storefront = $shopProcess.Id; Management = $staffProcess.Id; Database = $health.database; SqlServer = $SqlServer; Currency = $Currency; StartedAt = [DateTimeOffset]::UtcNow.ToString('O') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runtimePath 'session.json') -Encoding UTF8
    foreach ($port in 5188, 5189) {
        $ready = $false
        for ($attempt = 0; $attempt -lt 60; $attempt++) {
            try { $null = Invoke-WebRequest -UseBasicParsing -Uri ('http://127.0.0.1:' + $port + '/Amaara-Creations/') -TimeoutSec 2; $ready = $true; break } catch { Start-Sleep -Milliseconds 250 }
        }
        if (-not $ready) { throw ('Simulator frontend did not start on port ' + $port) }
    }
    Write-Host 'SIMULATED PAYMENTS ONLY. No merchant credentials or real financial transactions.'
    Write-Host ('Disposable SQL database: ' + $health.database)
    Write-Host 'Open http://127.0.0.1:5208/simulator'
    Write-Host 'Stop with: .\scripts\stop-payment-simulator.ps1 (SQL records are retained).'
    if (-not $NoBrowser) { Start-Process 'http://127.0.0.1:5208/simulator' -WindowStyle Hidden }
} catch {
    # These handles are exclusively the children created by this invocation.
    foreach ($ownedProcess in $ownedProcesses) { if (-not $ownedProcess.HasExited) { Stop-Process -Id $ownedProcess.Id -ErrorAction SilentlyContinue } }
    throw
} finally {
    $env:PATH = $previousPath; $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment; $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
    $env:AMAARA_LOCAL_PAYMENT_SIMULATOR = $previousFlag; $env:VITE_API_BASE_URL = $previousApi; $env:VITE_MEDIA_BASE_URL = $previousMedia
    $env:VITE_PUBLIC_SITE_URL = $previousPublic; $env:VITE_MANAGEMENT_LOGIN_URL = $previousManagement
}
