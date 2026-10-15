using be.Data;
using be.Models;
using be.Services.Checkout;
using be.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace be.Tests;

public sealed class SqlCommerceFactAttribute : FactAttribute
{
    public SqlCommerceFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION"))) Skip = "Set AMAARA_TEST_SQL_CONNECTION for isolated real SQL Server concurrency verification."; }
}
public sealed class SqlCommerceTests
{
    [SqlCommerceFact]
    public async Task RealSqlServerConcurrentIdempotencyFailureAndRetryKeepOneOrder()
    {
        var connection = Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.StartsWith("AmaaraPhase6Tests_", builder.InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var seed = new ApplicationDbContext(options);
        await seed.Database.MigrateAsync();
        var prefix = Guid.NewGuid().ToString("N");
        var user = new User { Id = prefix, Name = "Idempotency fixture", UserName = prefix + "@example.test", Email = prefix + "@example.test" };
        var product = new Product { Name = "Idempotency fixture", Slug = "idempotency-" + prefix, Price = 100m, BasePrice = 100m, Stock = 1 };
        var zone = new ShippingZone { Name = "Idempotency zone " + prefix, Country = "Sri Lanka" };
        var method = new ShippingMethod { Name = "Test delivery", Code = "IDEMPOTENCY_" + prefix, ShippingZone = zone, Currency = "LKR", BasePrice = 0m };
        var address = new Address { User = user, RecipientName = user.Name, AddressLine1 = "Test street", City = "Colombo", Country = "Sri Lanka", Phone = "+94770000000" };
        seed.AddRange(user, product, zone, method, address);
        await seed.SaveChangesAsync();
        seed.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 1 });
        await seed.SaveChangesAsync();

        var gateway = new FakePaymentGateway();
        await using var first = new CommerceFixture(new ApplicationDbContext(options), gateway);
        await using var second = new CommerceFixture(new ApplicationDbContext(options), gateway);
        var request = new be.DTOs.Checkout.CheckoutSessionRequest { AddressId = address.Id, ShippingMethodId = method.Id, IdempotencyKey = Guid.NewGuid() };
        request.QuoteVersion = (await first.Pricing.CalculateAsync(user.Id, request, default)).Quote.QuoteVersion;
        var sessions = await Task.WhenAll(
            first.Checkout.CreateAsync(user.Id, request, default),
            second.Checkout.CreateAsync(user.Id, request, default));
        Assert.Equal(sessions[0].OrderId, sessions[1].OrderId);
        Assert.Equal(sessions[0].PaymentId, sessions[1].PaymentId);
        Assert.Equal(1, gateway.Initializations);
        Assert.Equal(1, await seed.Orders.CountAsync(o => o.UserId == user.Id));
        Assert.Equal(1, await seed.Payments.CountAsync(p => p.OrderId == sessions[0].OrderId));
        Assert.Equal(1, await seed.InventoryReservations.CountAsync(r => r.OrderId == sessions[0].OrderId));
        seed.ChangeTracker.Clear();
        Assert.Equal(0, (await seed.Products.SingleAsync(p => p.Id == product.Id)).Stock);

        var original = await first.Db.Payments.SingleAsync(p => p.Id == sessions[0].PaymentId);
        await first.Payments.ApplyAsync(first.State(original, GatewayState.Failed), "idempotency-fixture-failure", default);
        seed.ChangeTracker.Clear();
        Assert.Equal(1, (await seed.Products.SingleAsync(p => p.Id == product.Id)).Stock);
        Assert.Equal("Released", (await seed.InventoryReservations.SingleAsync(r => r.OrderId == sessions[0].OrderId)).Status);
        Assert.Equal(PaymentStatus.Failed, (await seed.Payments.SingleAsync(p => p.Id == original.Id)).Status);

        var retryKey = Guid.NewGuid();
        var retries = await Task.WhenAll(
            first.Checkout.RetryAsync(user.Id, sessions[0].OrderId, retryKey, default),
            second.Checkout.RetryAsync(user.Id, sessions[0].OrderId, retryKey, default));
        Assert.Equal(retries[0].PaymentId, retries[1].PaymentId);
        Assert.NotEqual(original.Id, retries[0].PaymentId);
        Assert.Equal(2, gateway.Initializations);
        Assert.Equal(1, await seed.Orders.CountAsync(o => o.UserId == user.Id));
        Assert.Equal(2, await seed.Payments.CountAsync(p => p.OrderId == sessions[0].OrderId));
        Assert.Equal(2, await seed.CheckoutAttempts.CountAsync(a => a.OrderId == sessions[0].OrderId));
        seed.ChangeTracker.Clear();
        Assert.Equal(0, (await seed.Products.SingleAsync(p => p.Id == product.Id)).Stock);
        Assert.Equal(1, await seed.InventoryReservations.CountAsync(r => r.OrderId == sessions[0].OrderId && r.Status == "Reserved"));
    }

    [SqlCommerceFact]
    public async Task RealSqlServerChfFixtureSerializesStockWithMockedZahlsGateway()
    {
        var connection = Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.StartsWith("AmaaraPhase6Tests_", builder.InitialCatalog); // Disposable SQL only; never the business database.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var seed = new ApplicationDbContext(options);
        await seed.Database.MigrateAsync();
        var prefix = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            new User { Id = prefix + "a", Name = "Fixture A", UserName = prefix + "a@example.test", Email = prefix + "a@example.test" },
            new User { Id = prefix + "b", Name = "Fixture B", UserName = prefix + "b@example.test", Email = prefix + "b@example.test" }
        };
        var product = new Product { Name = "TEST CHF product", Slug = "test-chf-" + prefix, Currency = "CHF", Price = 25.50m, BasePrice = 25.50m, Stock = 1 };
        var zone = new ShippingZone { Name = "TEST CHF destination " + prefix, Country = "Fixtureland", RequiresPostalCode = true, RequiresPhone = true };
        var method = new ShippingMethod { Name = "TEST CHF delivery", Code = "TEST_CHF_" + prefix, ShippingZone = zone, Currency = "CHF", BasePrice = 3.25m, IsDevelopmentValue = true };
        var addresses = users.Select(user => new Address { User = user, RecipientName = user.Name, AddressLine1 = "Fixture street", City = "Fixture city", Country = "Fixtureland", PostalCode = "0000", Phone = "+41123456789" }).ToArray();
        seed.AddRange(users); seed.AddRange(product, zone, method); seed.AddRange(addresses);
        await seed.SaveChangesAsync();
        seed.CartItems.AddRange(users.Select(user => new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 1 }));
        await seed.SaveChangesAsync();

        var mockedGateway = new FakePaymentGateway { Provider = "Zahls", Mode = "TEST" }; // No provider network call or real money.
        await using var first = new CommerceFixture(new ApplicationDbContext(options), mockedGateway);
        await using var second = new CommerceFixture(new ApplicationDbContext(options), mockedGateway);
        foreach (var fixture in new[] { first, second })
        {
            fixture.Settings.Currency = "CHF";
            fixture.Settings.AllowDevelopmentShipping = true;
        }
        async Task<be.DTOs.Checkout.CheckoutSessionRequest> Request(CommerceFixture fixture, int index)
        {
            var request = new be.DTOs.Checkout.CheckoutSessionRequest { AddressId = addresses[index].Id, ShippingMethodId = method.Id, IdempotencyKey = Guid.NewGuid() };
            var quote = (await fixture.Pricing.CalculateAsync(users[index].Id, request, default)).Quote;
            Assert.Equal("CHF", quote.Currency);
            Assert.Equal(28.75m, quote.Total);
            Assert.Equal("Zahls", quote.PaymentProvider);
            Assert.Equal("TEST", quote.PaymentMode);
            request.QuoteVersion = quote.QuoteVersion;
            return request;
        }
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
        async Task<bool> Try(CommerceFixture fixture, int index, be.DTOs.Checkout.CheckoutSessionRequest request)
        {
            try { await fixture.Checkout.CreateAsync(users[index].Id, request, default); return true; }
            catch (CommerceException exception) { failures.Add(exception.Code); return false; }
            catch (DbUpdateConcurrencyException) { failures.Add(nameof(DbUpdateConcurrencyException)); return false; }
        }
        var requests = new[] { await Request(first, 0), await Request(second, 1) };
        var results = await Task.WhenAll(Try(first, 0, requests[0]), Try(second, 1, requests[1]));
        Assert.True(results.Count(result => result) == 1, $"Expected one successful checkout; failures: {string.Join(", ", failures)}");
        seed.ChangeTracker.Clear();
        Assert.Equal(0, (await seed.Products.SingleAsync(item => item.Id == product.Id)).Stock);
        var reservation = await seed.InventoryReservations.SingleAsync(item => item.ProductId == product.Id);
        Assert.Equal(1, reservation.Quantity);
        var payment = await seed.Payments.SingleAsync(item => item.OrderId == reservation.OrderId);
        var order = await seed.Orders.SingleAsync(item => item.Id == reservation.OrderId);
        Assert.Equal("CHF", order.Currency);
        Assert.Equal("CHF", payment.Currency);
        Assert.Equal(28.75m, payment.Amount);
        Assert.Equal("TEST", payment.ProviderMode);
        Assert.Equal("Zahls", payment.Provider);
    }

    [SqlCommerceFact]
    public async Task RealSqlServerSerializesStockCouponAndRefundRaces()
    {
        var connection = Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")!;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.StartsWith("AmaaraPhase6Tests_", builder.InitialCatalog); // Never migrate or mutate the business database.
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var seed = new ApplicationDbContext(opts); await seed.Database.MigrateAsync();
        var prefix = Guid.NewGuid().ToString("N");
        var users = new[] { new User { Id = prefix + "a", Name = "Test A", UserName = prefix + "a@example.test", Email = prefix + "a@example.test" }, new User { Id = prefix + "b", Name = "Test B", UserName = prefix + "b@example.test", Email = prefix + "b@example.test" } };
        var product = new Product { Name = "Concurrency fixture", Slug = "test-" + prefix, Price = 100, BasePrice = 100, Stock = 1 };
        var zone = new ShippingZone { Name = "Test-" + prefix, Country = "Sri Lanka" };
        var method = new ShippingMethod { Name = "Test delivery", Code = prefix.ToUpperInvariant(), ShippingZone = zone, Currency = "LKR", BasePrice = 0 };
        var addresses = users.Select(u => new Address { User = u, RecipientName = u.Name, AddressLine1 = "Test street", City = "Colombo", Country = "Sri Lanka", Phone = "+94770000000" }).ToArray();
        seed.AddRange(users); seed.AddRange(product, zone, method); seed.AddRange(addresses); await seed.SaveChangesAsync();
        seed.CartItems.AddRange(users.Select(u => new CartItem { UserId = u.Id, ProductId = product.Id, Quantity = 1 })); await seed.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        await using var a = new CommerceFixture(new ApplicationDbContext(opts), gateway); await using var b = new CommerceFixture(new ApplicationDbContext(opts), gateway);
        async Task<be.DTOs.Checkout.CheckoutSessionRequest> Request(CommerceFixture f, int i, string? code = null)
        {
            var request = new be.DTOs.Checkout.CheckoutSessionRequest { AddressId = addresses[i].Id, ShippingMethodId = method.Id, CouponCode = code, IdempotencyKey = Guid.NewGuid() };
            request.QuoteVersion = (await f.Pricing.CalculateAsync(users[i].Id, request, default)).Quote.QuoteVersion; return request;
        }
        async Task<bool> Try(CommerceFixture f, int i, be.DTOs.Checkout.CheckoutSessionRequest r)
        {
            try { await f.Checkout.CreateAsync(users[i].Id, r, default); return true; } catch (CommerceException) { return false; } catch (DbUpdateConcurrencyException) { return false; }
        }
        var requests = new[] { await Request(a, 0), await Request(b, 1) };
        var results = await Task.WhenAll(Try(a, 0, requests[0]), Try(b, 1, requests[1])); Assert.Equal(1, results.Count(x => x));
        seed.ChangeTracker.Clear(); Assert.Equal(0, (await seed.Products.SingleAsync(p => p.Id == product.Id)).Stock);
        var reservation = await seed.InventoryReservations.SingleAsync(r => r.ProductId == product.Id); Assert.Equal(1, reservation.Quantity);
        var winner = results[0] ? a : b; var payment = await winner.Db.Payments.SingleAsync(p => p.OrderId == reservation.OrderId);
        await winner.Payments.ApplyAsync(winner.State(payment), "sql-body", default); await winner.Payments.ApplyAsync(winner.State(payment), "sql-body", default);
        seed.ChangeTracker.Clear(); Assert.Equal("Consumed", (await seed.InventoryReservations.SingleAsync(r => r.Id == reservation.Id)).Status);
        await using var c = new CommerceFixture(new ApplicationDbContext(opts), gateway); await using var d = new CommerceFixture(new ApplicationDbContext(opts), gateway);
        async Task<bool> Refund(CommerceFixture f)
        {
            try { await f.Refunds.RequestAsync(payment.Id, users[0].Id, new(75, "Concurrency test", Guid.NewGuid()), default); return true; } catch (CommerceException) { return false; }
        }
        var refunds = await Task.WhenAll(Refund(c), Refund(d)); Assert.Equal(1, refunds.Count(x => x));
        Assert.Equal(75m, await seed.Refunds.Where(r => r.PaymentId == payment.Id && r.Status != "Failed").SumAsync(r => r.Amount));
        seed.ChangeTracker.Clear();
        var couponProduct = new Product { Name = "Coupon race fixture", Slug = "coupon-" + prefix, Price = 100, BasePrice = 100, Stock = 10 };
        var promotion = new Promotion { Code = "TEST-" + prefix.ToUpperInvariant(), Name = "Test-only coupon", StartsAt = DateTime.UtcNow.AddMinutes(-1), Currency = "LKR", UsageLimit = 1, DiscountValue = 10 };
        seed.AddRange(couponProduct, promotion); await seed.SaveChangesAsync();
        foreach (var user in users)
        {
            var cart = await seed.CartItems.SingleOrDefaultAsync(i => i.UserId == user.Id);
            if (cart == null) seed.CartItems.Add(new CartItem { UserId = user.Id, ProductId = couponProduct.Id, Quantity = 1 });
            else { cart.ProductId = couponProduct.Id; cart.UpdatedAt = DateTime.UtcNow; }
        }
        await seed.SaveChangesAsync();
        await using var e = new CommerceFixture(new ApplicationDbContext(opts), gateway); await using var f = new CommerceFixture(new ApplicationDbContext(opts), gateway);
        var couponRequests = new[] { await Request(e, 0, promotion.Code), await Request(f, 1, promotion.Code) };
        var couponResults = await Task.WhenAll(Try(e, 0, couponRequests[0]), Try(f, 1, couponRequests[1])); Assert.Equal(1, couponResults.Count(x => x));
        Assert.Equal(1, await seed.PromotionRedemptions.CountAsync(r => r.PromotionId == promotion.Id && r.Status == "Reserved"));
        // All fixtures are retained in the isolated test database; existing business data is untouched.
    }
}
