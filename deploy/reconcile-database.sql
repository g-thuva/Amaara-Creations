-- Invoke with sqlcmd -b -v EXPECTED_DATABASE="<approved exact name>" -d "<same name>".
-- Capture before/after outputs privately. This script reads aggregate evidence only.
IF DB_NAME() <> N'$(EXPECTED_DATABASE)' THROW 51010, 'Approved database identity does not match.', 1;
SELECT DB_NAME() AS DatabaseIdentity;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
DECLARE @counts nvarchar(max) = N'';
SELECT @counts = @counts + N'SELECT N''' + REPLACE(SCHEMA_NAME(schema_id) + N'.' + name, N'''', N'''''') +
    N''' AS TableName, COUNT_BIG(*) AS [RowCount] FROM ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';'
FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY name;
EXEC sys.sp_executesql @counts;
SELECT Currency, Status, COUNT_BIG(*) AS Orders, SUM(Total) AS OrderTotals FROM dbo.Orders GROUP BY Currency, Status;
SELECT Provider, ProviderMode, Currency, Status, COUNT_BIG(*) AS Payments, SUM(Amount) AS AttemptAmounts, SUM(SettledAmount) AS SettledAmounts
FROM dbo.Payments GROUP BY Provider, ProviderMode, Currency, Status;
SELECT Currency, Status, COUNT_BIG(*) AS Refunds, SUM(Amount) AS RefundAmounts FROM dbo.Refunds GROUP BY Currency, Status;
SELECT SUM(CONVERT(bigint, Stock)) AS BaseStock, SUM(CASE WHEN Stock < 0 THEN 1 ELSE 0 END) AS NegativeBaseBalances FROM dbo.Products;
SELECT SUM(CONVERT(bigint, StockQuantity)) AS VariantStock, SUM(CASE WHEN StockQuantity < 0 THEN 1 ELSE 0 END) AS NegativeVariantBalances FROM dbo.ProductVariants;
SELECT MovementType, COUNT_BIG(*) AS Movements, SUM(CONVERT(bigint, QuantityDelta)) AS Delta FROM dbo.InventoryTransactions GROUP BY MovementType;
SELECT COUNT_BIG(*) AS BrokenCustomOrderReferences FROM dbo.OrderItems i LEFT JOIN dbo.CustomDesigns d ON d.Id = i.CustomDesignId WHERE i.CustomDesignId IS NOT NULL AND d.Id IS NULL;
SELECT COUNT_BIG(*) AS MalformedHistoricalSnapshots FROM dbo.OrderItems WHERE ConfigurationSnapshotJson IS NOT NULL AND ISJSON(ConfigurationSnapshotJson) <> 1;
SELECT COUNT_BIG(*) AS BrokenProofAssets FROM dbo.CustomDesignProofRevisions p LEFT JOIN dbo.CustomDesignAssets a ON a.Id = p.AssetId WHERE a.Id IS NULL;
SELECT COUNT_BIG(*) AS UnsentNotifications FROM dbo.CommerceNotifications WHERE SentAt IS NULL;
