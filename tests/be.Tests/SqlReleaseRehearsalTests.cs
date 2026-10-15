using System.Diagnostics;
using System.Text.Json;
using be.Data;
using be.Models;
using be.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit.Abstractions;

namespace be.Tests;

public sealed class SqlReleaseRehearsalTests(ITestOutputHelper output)
{
    private static string Connection(string suffix)
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_Release789_" + suffix + "_" + Guid.NewGuid().ToString("N")[..8];
        return connection.ConnectionString;
    }

    [SqlCommerceFact]
    public async Task UpgradeFromVerifiedPhase6PreservesHistoricalPricesAndFreshSchemaMatchesModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection("Upgrade")).Options;
        await using var db = new ApplicationDbContext(options);
        await db.GetService<IMigrator>().MigrateAsync("20261014112403_Phase6ZahlsTestCurrency");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Products (Name,Slug,Price,BasePrice,Currency,Description,ImageUrl,Category,Stock,IsActive,IsFeatured,CreatedAt,UpdatedAt) VALUES (N'Historical LKR fixture',N'upgrade-lkr',125.50,125.50,'LKR',N'Fixture',N'',N'custom',7,1,0,SYSUTCDATETIME(),SYSUTCDATETIME()), (N'Explicit CHF fixture',N'upgrade-chf',25.25,25.25,'CHF',N'Fixture',N'',N'custom',3,1,0,SYSUTCDATETIME(),SYSUTCDATETIME());");
        await db.Database.MigrateAsync();
        var products = await db.Products.OrderBy(p => p.Currency).ToListAsync();
        Assert.Equal(25.25m, products[0].Price); Assert.Equal("CHF", products[0].Currency); Assert.Equal(3, products[0].Stock);
        Assert.Equal(125.50m, products[1].Price); Assert.Equal("LKR", products[1].Currency); Assert.Equal(7, products[1].Stock);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync()); Assert.False(db.Database.HasPendingModelChanges());
        output.WriteLine("Upgrade rehearsal passed: Phase6ZahlsTestCurrency -> current; historical currencies, prices and stock preserved.");
    }

    [SqlCommerceFact]
    public async Task ConcurrentManualAdjustmentsUseTheCommerceLockAndRejectStaleBalances()
    {
        var connection = Connection("Inventory"); var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var seed = new ApplicationDbContext(options); await seed.Database.MigrateAsync();
        var user = new User { UserName = "inventory@example.test", Email = "inventory@example.test", Name = "Disposable inventory fixture" };
        var product = new Product { Name = "Stock fixture", Slug = "inventory-race", Stock = 1 }; seed.AddRange(user, product); await seed.SaveChangesAsync();
        await using var a = new ApplicationDbContext(options); await using var b = new ApplicationDbContext(options);
        async Task<bool> Adjust(ApplicationDbContext db)
        {
            try { await new InventoryOperations(db).AdjustAsync(product.Id, new(null, 1, -1, "SQL count fixture", 0), user.Id, default); return true; }
            catch (be.Services.Checkout.CommerceException exception) { Assert.Equal("STOCK_CHANGED", exception.Code); return false; }
        }
        var outcomes = await Task.WhenAll(Adjust(a), Adjust(b)); Assert.Single(outcomes, x => x);
        seed.ChangeTracker.Clear(); Assert.Equal(0, (await seed.Products.SingleAsync()).Stock); Assert.Single(await seed.InventoryTransactions.ToListAsync()); Assert.Single(await seed.AuditLogs.ToListAsync());
    }

    [SqlCommerceFact]
    public async Task CopyOnlyBackupRestoresToNewDisposableDatabaseWithFinancialAndMediaIntegrity()
    {
        var sourceConnection = Connection("Backup"); var sourceBuilder = new SqlConnectionStringBuilder(sourceConnection);
        var sourceName = sourceBuilder.InitialCatalog; var restoreName = sourceName + "_Restored";
        Assert.StartsWith("AmaaraPhase6Tests_", sourceName); Assert.StartsWith("AmaaraPhase6Tests_", restoreName);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(sourceConnection).Options;
        await using var source = new ApplicationDbContext(options); await source.Database.MigrateAsync();
        var user = new User { Name = "Disposable restore fixture", UserName = "restore@example.test", Email = "restore@example.test" };
        var product = new Product { Name = "Restore media fixture", Slug = "restore-fixture", Stock = 11, Price = 25.25m, BasePrice = 25.25m, Currency = "CHF" };
        product.Media.Add(new ProductMedia { StorageKey = "fixture/product.png", Url = "/uploads/fixture/product.png" });
        source.AddRange(user, product); await source.SaveChangesAsync();
        foreach (var currency in new[] { "LKR", "CHF" }) source.Orders.Add(new Order { UserId = user.Id, OrderNumber = "RESTORE-" + currency, Currency = currency, Subtotal = 25.25m, Total = 25.25m,
            Payments = [new Payment { Provider = "Fixture", ProviderReference = "RESTORE-" + currency, ProviderMode = "TEST", Method = "Mock", Currency = currency, Amount = 25.25m, SettledAmount = 25.25m, Status = PaymentStatus.Paid }] });
        await source.SaveChangesAsync();
        var masterBuilder = new SqlConnectionStringBuilder(sourceConnection) { InitialCatalog = "master", Pooling = false };
        await using var master = new SqlConnection(masterBuilder.ConnectionString); await master.OpenAsync();
        async Task<string> PathProperty(string property)
        {
            await using var command = new SqlCommand("SELECT CONVERT(nvarchar(4000), SERVERPROPERTY(@property))", master); command.Parameters.AddWithValue("@property", property);
            return (await command.ExecuteScalarAsync())?.ToString() ?? throw new InvalidOperationException("SQL Server default backup/data path is required for this disposable rehearsal.");
        }
        static string Literal(string value) => "N'" + value.Replace("'", "''") + "'";
        static string Identifier(string value) => "[" + value.Replace("]", "]]") + "]";
        var backupFile = Path.Combine(await PathProperty("InstanceDefaultBackupPath"), sourceName + ".bak");
        var dataPath = await PathProperty("InstanceDefaultDataPath"); var logPath = await PathProperty("InstanceDefaultLogPath");
        async Task Execute(string sql)
        {
            await using var command = new SqlCommand(sql, master) { CommandTimeout = 120 }; await command.ExecuteNonQueryAsync();
        }
        var watch = Stopwatch.StartNew();
        await Execute($"BACKUP DATABASE {Identifier(sourceName)} TO DISK = {Literal(backupFile)} WITH COPY_ONLY, CHECKSUM;");
        await Execute($"RESTORE VERIFYONLY FROM DISK = {Literal(backupFile)} WITH CHECKSUM;");
        var moves = new List<string>();
        await using (var command = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = {Literal(backupFile)}", master))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            var index = 0;
            while (await reader.ReadAsync())
            {
                var logicalName = reader["LogicalName"].ToString()!; var isLog = reader["Type"].ToString() == "L";
                moves.Add($"MOVE {Literal(logicalName)} TO {Literal(Path.Combine(isLog ? logPath : dataPath, restoreName + "_" + index++ + (isLog ? ".ldf" : ".mdf")))}");
            }
        }
        await using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = @name", master))
        { command.Parameters.AddWithValue("@name", restoreName); Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync())); }
        await Execute($"RESTORE DATABASE {Identifier(restoreName)} FROM DISK = {Literal(backupFile)} WITH CHECKSUM, {string.Join(", ", moves)};");
        var restoredBuilder = new SqlConnectionStringBuilder(sourceConnection) { InitialCatalog = restoreName };
        await using var restored = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(restoredBuilder.ConnectionString).Options);
        Assert.Empty(await restored.Database.GetPendingMigrationsAsync()); Assert.Equal(2, await restored.Orders.CountAsync()); Assert.Equal(11, (await restored.Products.SingleAsync()).Stock);
        Assert.Equal("fixture/product.png", (await restored.ProductMedia.SingleAsync()).StorageKey);
        var balances = await restored.Payments.GroupBy(p => p.Currency).Select(g => new { Currency = g.Key, Amount = g.Sum(p => p.SettledAmount) }).ToListAsync();
        Assert.Equal(2, balances.Count); Assert.All(balances, b => Assert.Equal(25.25m, b.Amount));
        watch.Stop();
        output.WriteLine($"Backup CHECKSUM / VERIFYONLY / restore / migration / currency / stock / media checks passed. Development rehearsal RPO: 0 fixture writes. RTO including validation: {watch.Elapsed.TotalSeconds:F2} seconds. Disposable databases and backup retained.");
        var artifact = Path.Combine(AppContext.BaseDirectory, "release789-backup-rehearsal.json");
        await File.WriteAllTextAsync(artifact, JsonSerializer.Serialize(new { status = "PASS", developmentOnly = true, rpoFixtureWrites = 0, rtoSeconds = watch.Elapsed.TotalSeconds, currencies = balances.Select(b => b.Currency), financialRows = 2, mediaReferences = 1, productionGuarantee = false }));
    }
}
