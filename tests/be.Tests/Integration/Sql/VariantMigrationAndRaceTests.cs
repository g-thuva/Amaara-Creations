using be.Application.Features.Checkout;
using be.Domain.Cart;
using be.Domain.Catalog;
using be.Domain.Common;
using be.Domain.Identity;
using be.Domain.Shipping;
using be.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace be.Tests;
public sealed class VariantMigrationAndRaceTests
{
    private static DbContextOptions<ApplicationDbContext> Options()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!);
        Assert.StartsWith("AmaaraPhase6Tests_", connection.InitialCatalog);
        connection.InitialCatalog = "AmaaraPhase6Tests_Variants_" + Guid.NewGuid().ToString("N")[..8];
        return new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
    }

    [SqlCommerceFact]
    public async Task UpgradePreservesExistingCartAndRollbackRefusesToDiscardVariantChoices()
    {
        var options = Options();
        await using var db = new ApplicationDbContext(options);
        var migrator = db.Database.GetService<IMigrator>();
        const string previous = "20261015194809_Phase7NormalizeLegacyEvidence";
        await migrator.MigrateAsync(previous);
        var user = new User { Name = "Upgrade", UserName = "upgrade@example.test", Email = "upgrade@example.test" };
        var product = new Product { Name = "Upgrade", Slug = "upgrade", Price = 100, BasePrice = 100, Stock = 100 };
        db.AddRange(user, product); await db.SaveChangesAsync();
        // Create a cart line using only columns present in the old schema.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [CartItems] ([UserId],[ProductId],[ItemType],[Quantity],[UnitPriceSnapshot],[CreatedAt],[UpdatedAt]) VALUES ({user.Id},{product.Id},0,2,100,GETUTCDATE(),GETUTCDATE());");
        await migrator.MigrateAsync();
        var original = await db.CartItems.SingleAsync();
        Assert.Null(original.ProductVariantId);
        Assert.Equal(2, original.Quantity);
        var option = new ProductVariant { ProductId = product.Id, Sku = "UPGRADE-OPTION", Name = "Option", StockQuantity = 5, PriceOverride = 130 };
        db.Add(option); await db.SaveChangesAsync();
        db.CartItems.Add(new CartItem { ProductId = product.Id, ProductVariantId = option.Id, UserId = user.Id, Quantity = 1 });
        await db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(previous));
        Assert.Equal(51003, exception.Number);
        Assert.Equal(2, await db.CartItems.CountAsync());
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), name => name.EndsWith("BoutiqueCartVariants"));
    }

    [SqlCommerceFact]
    public async Task ConcurrentCustomersCannotOversellAnOptionOrConsumeBaseStock()
    {
        var options = Options();
        await using var seed = new CommerceFixture(new ApplicationDbContext(options));
        await seed.Db.Database.MigrateAsync();
        var userA = new User { Id = "a", Name = "A", UserName = "a@example.test", Email = "a@example.test" };
        var userB = new User { Id = "b", Name = "B", UserName = "b@example.test", Email = "b@example.test" };
        var product = new Product { Name = "Variant race", Slug = "variant-race", Currency = "LKR", Price = 100, BasePrice = 100, Stock = 100 };
        var zone = new ShippingZone { Name = "Test zone", Country = "Sri Lanka", RequiresPhone = true };
        var shipping = new ShippingMethod { Name = "Test method", Code = "TEST", Currency = "LKR", BasePrice = 25, ShippingZone = zone };
        var addressA = new Address { User = userA, RecipientName = "A", AddressLine1 = "Test street", City = "Colombo", Phone = "+94770000000" };
        var addressB = new Address { User = userB, RecipientName = "B", AddressLine1 = "Test street", City = "Colombo", Phone = "+94770000000" };
        seed.Db.AddRange(userA, userB, product, zone, shipping, addressA, addressB); await seed.Db.SaveChangesAsync();
        var option = new ProductVariant { ProductId = product.Id, Name = "Scarce option", Sku = "RACE-OPTION", PriceOverride = 130, StockQuantity = 3 };
        seed.Db.ProductVariants.Add(option); await seed.Db.SaveChangesAsync();
        seed.Db.CartItems.AddRange(new CartItem { UserId = "a", ProductId = product.Id, ProductVariantId = option.Id, Quantity = 2 }, new CartItem { UserId = "b", ProductId = product.Id, ProductVariantId = option.Id, Quantity = 2 });
        await seed.Db.SaveChangesAsync();
        await using var first = new CommerceFixture(new ApplicationDbContext(options));
        await using var second = new CommerceFixture(new ApplicationDbContext(options));
        first.Settings.Currency = second.Settings.Currency = "LKR";
        var a = new CheckoutSessionRequest { AddressId = addressA.Id, ShippingMethodId = shipping.Id, IdempotencyKey = Guid.NewGuid() };
        var b = new CheckoutSessionRequest { AddressId = addressB.Id, ShippingMethodId = shipping.Id, IdempotencyKey = Guid.NewGuid() };
        a.QuoteVersion = (await first.Pricing.CalculateAsync("a", a, default)).Quote.QuoteVersion;
        b.QuoteVersion = (await second.Pricing.CalculateAsync("b", b, default)).Quote.QuoteVersion;
        async Task<bool> Checkout(CommerceFixture fixture, string user, CheckoutSessionRequest request)
        {
            try { await fixture.Checkout.CreateAsync(user, request, default); return true; }
            catch (CommerceException exception) when (exception.Code == "INSUFFICIENT_STOCK") { return false; }
        }
        var results = await Task.WhenAll(Checkout(first, "a", a), Checkout(second, "b", b));
        Assert.Single(results, result => result);
        seed.Db.ChangeTracker.Clear();
        Assert.Equal(1, await seed.Db.Orders.CountAsync());
        Assert.Equal(1, await seed.Db.ProductVariants.Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(100, await seed.Db.Products.Select(x => x.Stock).SingleAsync());
        var line = await seed.Db.OrderItems.SingleAsync();
        Assert.Equal(option.Id, line.ProductVariantId);
        Assert.Equal(260, line.Subtotal);
    }
}
