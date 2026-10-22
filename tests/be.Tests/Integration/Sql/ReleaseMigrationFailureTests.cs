using System.Data.Common;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Inventory;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Domain.Reviews;
using be.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace be.Tests;

public sealed class ReleaseMigrationFailureTests
{
    [SqlCommerceFact]
    public async Task FailedMigrationRollsBackItsDataChangesAndForwardRetryPreservesFinancialHistory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_MigrationFailure_" + Guid.NewGuid().ToString("N")[..8];
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed = new ApplicationDbContext(options);
        await seed.GetService<IMigrator>().MigrateAsync("20261015193736_Phase7ReviewDefaults");
        var user = new User { Name = "Failure rehearsal", UserName = "failure@example.test" };
        var lkr = new Product { Name = "Historical LKR", Slug = "failure-lkr", Currency = "LKR", Price = 125.50m, BasePrice = 125.50m, Stock = 7 };
        var chf = new Product { Name = "Explicit CHF", Slug = "failure-chf", Currency = "CHF", Price = 25.25m, BasePrice = 25.25m, Stock = 3 };
        seed.AddRange(user, lkr, chf);
        await seed.SaveChangesAsync();
        foreach (var product in new[] { lkr, chf })
            seed.Orders.Add(new Order { UserId = user.Id, OrderNumber = "FAILURE-" + product.Currency, Currency = product.Currency, Subtotal = product.Price, Total = product.Price,
                OrderItems = [new OrderItem { ProductId = product.Id, ProductNameSnapshot = product.Name, Quantity = 1, Price = product.Price, UnitPrice = product.Price, Subtotal = product.Price, ConfigurationSnapshotJson = "{\"source\":\"legacy fixture\"}" }],
                Payments = [new Payment { Provider = product.Currency == "LKR" ? "Datatrans" : "Zahls", ProviderMode = "TEST", ProviderReference = "FAILURE-" + product.Currency, Currency = product.Currency, Amount = product.Price, SettledAmount = product.Price, Status = PaymentStatus.Paid }] });
        seed.Reviews.Add(new Review { UserId = user.Id, ProductId = lkr.Id, Rating = 5, Comment = "Legacy review", ModerationStatus = "" });
        seed.InventoryTransactions.Add(new InventoryTransaction { ProductId = lkr.Id, PreviousQuantity = 0, QuantityDelta = 7, NewQuantity = 7, MovementType = "", Reason = "Legacy fixture" });
        await seed.SaveChangesAsync();
        const string last = "20261015194809_Phase7NormalizeLegacyEvidence";
        Assert.Contains(last, await seed.Database.GetPendingMigrationsAsync());
        await using (var failing = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).AddInterceptors(new FailHistory(last)).Options))
            await Assert.ThrowsAsync<InvalidOperationException>(() => failing.Database.MigrateAsync());
        await using var verify = new ApplicationDbContext(options);
        Assert.Contains(last, await verify.Database.GetPendingMigrationsAsync());
        Assert.Equal("", (await verify.Reviews.AsNoTracking().SingleAsync()).ModerationStatus);
        Assert.Equal("", (await verify.InventoryTransactions.AsNoTracking().SingleAsync()).MovementType);
        await verify.Database.MigrateAsync();
        Assert.Empty(await verify.Database.GetPendingMigrationsAsync());
        Assert.Equal("Pending", (await verify.Reviews.AsNoTracking().SingleAsync()).ModerationStatus);
        Assert.Equal("Adjustment", (await verify.InventoryTransactions.AsNoTracking().SingleAsync()).MovementType);
        var totals = await verify.Payments.GroupBy(payment => payment.Currency).Select(group => new { Currency = group.Key, Amount = group.Sum(payment => payment.SettledAmount) }).ToDictionaryAsync(total => total.Currency, total => total.Amount);
        Assert.Equal(125.50m, totals["LKR"]); Assert.Equal(25.25m, totals["CHF"]);
        Assert.Equal(new[] { "Datatrans", "Zahls" }, await verify.Payments.OrderBy(payment => payment.Provider).Select(payment => payment.Provider).ToArrayAsync());
        Assert.All(await verify.OrderItems.ToListAsync(), item => Assert.Equal("{\"source\":\"legacy fixture\"}", item.ConfigurationSnapshotJson));
        Assert.Equal(7, (await verify.Products.SingleAsync(product => product.Id == lkr.Id)).Stock);
    }

    private sealed class FailHistory(string migration) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [__EFMigrationsHistory]", StringComparison.Ordinal) && command.CommandText.Contains(migration, StringComparison.Ordinal))
                throw new InvalidOperationException("Deliberate migration-history failure in disposable rehearsal.");
            return ValueTask.FromResult(result);
        }
    }
}
