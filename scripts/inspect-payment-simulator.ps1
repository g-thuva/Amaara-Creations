param([string]$Database)
$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot
$session = Get-Content -LiteralPath (Join-Path $repositoryPath '.runtime\local-payments\session.json') -Raw | ConvertFrom-Json
if (-not $Database) { $Database = $session.Database }
if ($Database -notmatch '^AmaaraPhase6Tests_LocalPayments_(LKR|CHF)_[a-f0-9]{32}$') { throw 'Only a disposable payment simulator database may be inspected.' }
if ($session.SqlServer -notmatch '^(localhost|127\.0\.0\.1|\.|\(localdb\)\\MSSQLLocalDB)(\\[A-Za-z0-9_]+)?$') { throw 'Only loopback SQL Server is allowed.' }
Add-Type -AssemblyName System.Data
$connectionSettings = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$connectionSettings['Data Source'] = $session.SqlServer; $connectionSettings['Initial Catalog'] = $Database
$connectionSettings['Integrated Security'] = $true; $connectionSettings['Encrypt'] = $true; $connectionSettings['TrustServerCertificate'] = $true
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionSettings.ConnectionString)
try {
    $connection.Open()
    $guard = $connection.CreateCommand()
    $guard.CommandText = "SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name=N'AmaaraLocalPaymentSimulator' AND CONVERT(nvarchar(100),value)=N'Synthetic disposable data only'"
    if ([int]$guard.ExecuteScalar() -ne 1) { throw 'The SQL simulator safety marker is missing; inspection refused.' }
    Write-Host ('SIMULATED SQL records only: ' + $Database)
    $queries = [ordered]@{
        Orders = 'SELECT Id, OrderNumber, Status, Total, Currency FROM Orders ORDER BY Id';
        Payments = 'SELECT Id, OrderId, Provider, ProviderMode, Status, Amount, Currency, SettledAmount, RefundedAmount FROM Payments ORDER BY Id';
        Attempts = 'SELECT Id, OrderId, PaymentId, Provider FROM CheckoutAttempts ORDER BY Id';
        Reservations = 'SELECT OrderId, ProductId, Quantity, Status FROM InventoryReservations';
        Refunds = 'SELECT Id, PaymentId, Amount, Currency, Status, IdempotencyKey FROM Refunds';
        WebhookEvents = 'SELECT PaymentId, ProviderStatus, ProcessedAt FROM PaymentWebhookEvents';
        Inventory = 'SELECT Id, Name, Stock, Currency FROM Products ORDER BY Id'
    }
    foreach ($entry in $queries.GetEnumerator()) {
        Write-Host ("`n" + $entry.Key)
        $command = $connection.CreateCommand(); $command.CommandText = $entry.Value
        $table = New-Object System.Data.DataTable
        $reader = $command.ExecuteReader()
        try { $table.Load($reader) } finally { $reader.Dispose() }
        $table | Format-Table -AutoSize | Out-Host
    }
} finally { $connection.Dispose() }
