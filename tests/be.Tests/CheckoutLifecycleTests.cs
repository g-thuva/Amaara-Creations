using be.DTOs.Checkout;
using be.Models;
using be.Services.Checkout;
using be.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace be.Tests;

public sealed class CheckoutLifecycleTests
{
    [Theory]
    [InlineData("LKR", 123.455, 12346)]
    [InlineData("CHF", 10.005, 1001)]
    [InlineData("JPY", 10.5, 11)]
    [InlineData("KWD", 10.0005, 10001)]
    public void CurrencyExponentsAndRoundingAreExplicit(string currency, decimal amount, long expected) => Assert.Equal(expected, Money.ToMinor(amount, currency));
    [Fact] public void UnsupportedAndNegativeMoneyFailClosed() { Assert.Throws<CommerceException>(() => Money.ToMinor(1, "XYZ")); Assert.Throws<CommerceException>(() => Money.ToMinor(-1, "LKR")); }
    [Fact] public void TaxesAndFeesUseDecimalAndRespectInclusiveTax()
    {
        var o = new CheckoutOptions { TaxRate = 10, PaymentFeePercentage = 2 };
        Assert.Equal((20m, 4.90m, 249.90m), CheckoutPricingService.Totals(200, 0, 25, o));
        o.PricesIncludeTax = true;
        Assert.Equal((18.18m, 4.50m, 229.50m), CheckoutPricingService.Totals(200, 0, 25, o));
    }
    [Fact] public async Task QuoteUsesLiveStandardAndCustomPricesAndOneShippingCharge()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true);
        var q = (await f.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = 1 }, default)).Quote;
        Assert.Equal(248m, q.Subtotal); Assert.Equal(25m, q.Shipping); Assert.Equal(273m, q.Total); Assert.Equal("LKR", q.Currency); Assert.Equal(2, q.Items.Count);
    }
    [Fact] public async Task ChfQuoteRequiresExplicitChfProductBuilderAndShippingPrices()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true, currency: "CHF");
        var quote = (await f.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = 1 }, default)).Quote;
        Assert.Equal("CHF", quote.Currency); Assert.Equal(248m, quote.Subtotal); Assert.Equal(25m, quote.Shipping); Assert.Equal(273m, quote.Total);
        var session = await f.Checkout.CreateAsync("a", await f.RequestAsync(), default);
        var order = await f.Db.Orders.Include(o => o.OrderItems).SingleAsync();
        var payment = await f.Db.Payments.SingleAsync();
        Assert.Equal("CHF", order.Currency); Assert.Equal("CHF", payment.Currency); Assert.Equal(273m, payment.Amount);
        Assert.Contains("\"Currency\":\"CHF\"", order.OrderItems.Single(i => i.ItemType == OrderItemType.CustomDesign).ConfigurationSnapshotJson);
        Assert.Equal(order.Id, session.OrderId);
    }
    [Fact] public async Task LkrProductCannotBecomeChfByChangingCheckoutConfiguration()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); f.Settings.Currency = "CHF";
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = 1 }, default));
        Assert.Equal("PAYMENT_CURRENCY_MISMATCH", error.Code);
        Assert.Equal("LKR", (await f.Db.Products.SingleAsync()).Currency);
        Assert.Empty(f.Db.Orders); Assert.Empty(f.Db.InventoryReservations);
    }
    [Fact] public async Task OriginalLkrCustomDesignCannotBeRepricedAsChf()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true, currency: "CHF");
        var design = await f.Db.CustomDesigns.SingleAsync();
        var original = new CustomBuilderConfigurationVersion { VersionNumber = 0, Status = BuilderVersionStatus.Superseded, Currency = "LKR" };
        f.Db.CustomBuilderConfigurationVersions.Add(original); design.BuilderConfigurationVersion = original;
        await f.Db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = 1 }, default));
        Assert.Equal("PAYMENT_CURRENCY_MISMATCH", error.Code); Assert.Empty(f.Db.Orders);
    }
    [Fact] public async Task QuoteAndOrderFailClosedWhenPaymentGatewayIsUnavailable()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); f.Gateway.Available = false;
        var request = new CheckoutSessionRequest { AddressId = 1, ShippingMethodId = 1, IdempotencyKey = Guid.NewGuid() };
        var quote = (await f.Pricing.CalculateAsync("a", request, default)).Quote;
        Assert.False(quote.PaymentAvailable); Assert.False(quote.CanPay);
        request.QuoteVersion = quote.QuoteVersion;
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.Checkout.CreateAsync("a", request, default));
        Assert.Equal("PAYMENT_NOT_CONFIGURED", error.Code); Assert.Empty(f.Db.Orders); Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock);
    }
    [Fact] public async Task AddressOwnershipIsEnforced()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync();
        var e = await Assert.ThrowsAsync<CommerceException>(() => f.Pricing.CalculateAsync("a", new() { AddressId = 2 }, default)); Assert.Equal(404, e.Status);
    }
    [Fact] public async Task ChangedPriceRejectsOldQuoteWithoutReservingStock()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var r = await f.RequestAsync();
        (await f.Db.Products.SingleAsync()).Price = 101; await f.Db.SaveChangesAsync();
        var e = await Assert.ThrowsAsync<CommerceException>(() => f.Checkout.CreateAsync("a", r, default));
        Assert.Equal("CHECKOUT_CHANGED", e.Code); Assert.Empty(f.Db.Orders); Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock);
    }
    [Fact] public async Task DuplicateCheckoutReturnsOneOrderPaymentAndReservation()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var r = await f.RequestAsync();
        var first = await f.Checkout.CreateAsync("a", r, default); var second = await f.Checkout.CreateAsync("a", r, default);
        Assert.Equal(first.PaymentId, second.PaymentId); Assert.Single(f.Db.Orders); Assert.Single(f.Db.Payments); Assert.Single(f.Db.InventoryReservations); Assert.Equal(1, f.Gateway.Initializations); Assert.Single(f.Db.CartItems);
    }
    [Fact] public async Task DoubleSubmissionWithDifferentKeysStillUsesOnePendingOrder()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var r = await f.RequestAsync();
        var first = await f.Checkout.CreateAsync("a", r, default); r.IdempotencyKey = Guid.NewGuid();
        var second = await f.Checkout.CreateAsync("a", r, default); Assert.Equal(first.PaymentId, second.PaymentId); Assert.Single(f.Db.Orders); Assert.Equal(1, f.Gateway.Initializations);
    }
    [Fact] public async Task CrossCustomerIdempotencyKeyIsHidden()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var r = await f.RequestAsync(); await f.Checkout.CreateAsync("a", r, default);
        var e = await Assert.ThrowsAsync<CommerceException>(() => f.Checkout.CreateAsync("b", r, default)); Assert.Equal(404, e.Status);
    }
    [Fact] public async Task AmbiguousGatewayCreationKeepsReservationAndBlocksDuplicateAttempt()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); f.Gateway.FailInitialization = true;
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.StartAsync());
        Assert.Equal("PAYMENT_INITIALIZATION_UNCERTAIN", error.Code);
        var payment = await f.Db.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status); Assert.Equal("creation_uncertain", payment.ProviderStatus);
        Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock); Assert.Single(f.Db.CartItems);
        var retry = await f.Checkout.RetryAsync("a", payment.OrderId, Guid.NewGuid(), default);
        Assert.Equal(payment.Id, retry.PaymentId); Assert.Null(retry.RedirectUrl); Assert.Equal(1, f.Gateway.Initializations);
    }
    [Fact] public async Task DefiniteGatewayRejectionReleasesStockAndKeepsCart()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); f.Gateway.RejectInitialization = true;
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.StartAsync());
        Assert.Equal("PAYMENT_INITIALIZATION_FAILED", error.Code);
        Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock); Assert.Single(f.Db.CartItems);
        Assert.Equal(PaymentStatus.Failed, (await f.Db.Payments.SingleAsync()).Status);
    }
    [Fact] public async Task HostedUrlAndProviderIdentitySurviveIdempotentCheckout()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var request = await f.RequestAsync();
        var first = await f.Checkout.CreateAsync("a", request, default);
        var second = await f.Checkout.CreateAsync("a", request, default);
        var payment = await f.Db.Payments.SingleAsync();
        Assert.Equal(first.RedirectUrl, second.RedirectUrl);
        Assert.Equal(first.RedirectUrl, payment.HostedPaymentUrl);
        Assert.Equal(payment.ProviderTransactionId, payment.ProviderGatewayId);
        Assert.Equal("Sandbox", payment.ProviderMode);
    }
    [Fact] public async Task VerifiedMixedPaymentFinalizesExactlyOnceAndKeepsSnapshot()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true); var p = await f.StartAsync();
        var snapshot = (await f.Db.OrderItems.SingleAsync(i => i.ItemType == OrderItemType.CustomDesign)).ConfigurationSnapshotJson;
        var state = f.State(p); await f.Payments.ApplyAsync(state, "same-body", default); await f.Payments.ApplyAsync(state, "same-body", default); await f.Payments.ApplyAsync(state, "alternate-body", default);
        Assert.Equal(OrderStatus.Confirmed, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock); Assert.Equal("Consumed", (await f.Db.InventoryReservations.SingleAsync()).Status); Assert.Empty(f.Db.CartItems);
        Assert.Single(f.Db.CommerceNotifications); Assert.Equal(2, await f.Db.PaymentWebhookEvents.CountAsync());
        (await f.Db.CustomBuilderConfigurationVersions.SingleAsync()).PricePerSquareUnit = 99; await f.Db.SaveChangesAsync();
        Assert.Equal(snapshot, (await f.Db.OrderItems.SingleAsync(i => i.ItemType == OrderItemType.CustomDesign)).ConfigurationSnapshotJson);
        Assert.Equal(ProofStatus.Preparing, (await f.Db.CustomDesigns.SingleAsync()).ProofStatus); Assert.Equal(ProductionStatus.NotStarted, (await f.Db.CustomDesigns.SingleAsync()).ProductionStatus);
    }
    [Fact] public async Task CartItemsAddedOrEditedDuringPaymentRemain()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync();
        var cart = await f.Db.CartItems.SingleAsync(); cart.Quantity = 3; cart.UpdatedAt = cart.UpdatedAt.AddSeconds(1);
        f.Db.CartItems.Add(new CartItem { UserId = "a", ProductId = 2, Quantity = 1 }); await f.Db.SaveChangesAsync();
        await f.Payments.ApplyAsync(f.State(p), null, default); Assert.Equal(2, await f.Db.CartItems.CountAsync());
    }
    [Theory] [InlineData(GatewayState.Failed)] [InlineData(GatewayState.Cancelled)]
    public async Task FailedOrCancelledPaymentReleasesAndRetryKeepsSameOrder(GatewayState state)
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p, state), null, default);
        Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock); Assert.Single(f.Db.CartItems);
        var retry = await f.Checkout.RetryAsync("a", p.OrderId, Guid.NewGuid(), default); Assert.Equal(p.OrderId, retry.OrderId); Assert.Single(f.Db.Orders); Assert.Equal(2, await f.Db.Payments.CountAsync()); Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock);
    }
    [Fact] public async Task AuthorizedPaymentDoesNotFinalizeOrConsume()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p, GatewayState.Authorized), null, default);
        Assert.Equal(OrderStatus.PendingPayment, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal("Reserved", (await f.Db.InventoryReservations.SingleAsync()).Status); Assert.Single(f.Db.CartItems);
    }
    [Fact] public async Task ExpiredUnstartedReservationIsReleasedOnce()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync();
        await f.Payments.ApplyAsync(f.State(p, GatewayState.Pending), null, default);
        p = await f.Db.Payments.SingleAsync(); p.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); await f.Db.SaveChangesAsync();
        await f.Payments.ExpireAsync(p.Id, default); await f.Payments.ExpireAsync(p.Id, default);
        Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock); Assert.Equal("Released", (await f.Db.InventoryReservations.SingleAsync()).Status); Assert.Single(f.Db.CartItems);
    }
    [Fact] public async Task LateVerifiedSettlementIsRecordedForReviewWithoutOverselling()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); var paid = f.State(p);
        await f.Payments.ApplyAsync(f.State(p, GatewayState.Cancelled), null, default); await f.Payments.ApplyAsync(paid, null, default);
        Assert.Equal(OrderStatus.PaymentReview, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal(PaymentStatus.Paid, (await f.Db.Payments.SingleAsync()).Status); Assert.Equal(10, (await f.Db.Products.SingleAsync()).Stock); Assert.Single(f.Db.CartItems);
    }
    [Fact] public async Task ReconciliationConfirmsProviderStateAndDoesNotTrustBrowser()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); f.Gateway.NextStatus = f.State(p); await f.Payments.ReconcileAsync(p.Id, default);
        Assert.Equal(PaymentStatus.Paid, (await f.Db.Payments.SingleAsync()).Status);
    }
    [Fact] public async Task ForcedReconciliationOfPaidPaymentFlagsDisputeWithoutChangingMoney()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var payment = await f.StartAsync();
        await f.Payments.ApplyAsync(f.State(payment), null, default);
        f.Gateway.NextStatus = f.State(payment, GatewayState.Disputed) with { ProviderStatus = "chargeback" };
        await f.Payments.ReconcileAsync(payment.Id, default, force: true);
        var persisted = await f.Db.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Paid, persisted.Status);
        Assert.Equal(payment.Amount, persisted.SettledAmount);
        Assert.Equal(0, persisted.RefundedAmount);
        Assert.Equal("chargeback", persisted.ProviderStatus);
        Assert.Equal(OrderStatus.PaymentReview, (await f.Db.Orders.SingleAsync()).Status);
    }
    [Fact] public async Task HistoricalProviderReconcilesAndRefundsThroughItsOwnAdapter()
    {
        var legacy = new FakePaymentGateway { Provider = "Datatrans" };
        var active = new FakePaymentGateway { Provider = "Zahls" };
        await using var f = new CommerceFixture(gateway: legacy); await f.SeedAsync();
        var payment = await f.StartAsync();
        payment.HostedPaymentUrl = null; await f.Db.SaveChangesAsync();
        Assert.Equal(legacy.RedirectUrl(payment.ProviderTransactionId!), f.Checkout.Session(payment).RedirectUrl);
        var router = new PaymentGatewayRouter([legacy, active]);
        var reconciliation = new PaymentReconciliationService(f.Db, active, f.Inventory, Options.Create(f.Settings), NullLogger<PaymentReconciliationService>.Instance, router);
        legacy.NextStatus = f.State(payment);
        await reconciliation.ReconcileAsync(payment.Id, default);
        Assert.Equal(PaymentStatus.Paid, (await f.Db.Payments.SingleAsync()).Status);
        var refunds = new RefundService(f.Db, active, router);
        var refund = await refunds.RequestAsync(payment.Id, "a", new(25, "Legacy provider refund", Guid.NewGuid()), default);
        Assert.Equal("Completed", refund.Status);
        Assert.Equal(1, legacy.RefundCalls);
        Assert.Equal(0, active.RefundCalls);
    }
    [Theory] [InlineData("merchant")] [InlineData("amount")] [InlineData("currency")] [InlineData("reference")] [InlineData("transaction")]
    public async Task MismatchedPaymentCannotFinalize(string field)
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); var s = f.State(p);
        s = field switch { "merchant" => s with { MerchantId = "other" }, "amount" => s with { Amount = 1 }, "currency" => s with { Currency = "CHF" }, "reference" => s with { Reference = "other" }, _ => s with { TransactionId = "260000000000000009" } };
        await Assert.ThrowsAsync<CommerceException>(() => f.Payments.ApplyAsync(s, null, default)); Assert.Equal(PaymentStatus.Pending, (await f.Db.Payments.SingleAsync()).Status); Assert.Single(f.Db.CartItems);
    }
    [Fact] public async Task UnknownProviderStateCannotMarkPaid()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p, GatewayState.Unknown), null, default); Assert.Equal(PaymentStatus.Pending, (await f.Db.Payments.SingleAsync()).Status);
    }
    [Fact] public async Task RefundsAreIdempotentAndCannotExceedSettledMoney()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p), null, default);
        var r = new RefundRequest(100, "Test partial refund", Guid.NewGuid()); var first = await f.Refunds.RequestAsync(p.Id, "a", r, default); var duplicate = await f.Refunds.RequestAsync(p.Id, "a", r, default);
        Assert.Equal(first.Id, duplicate.Id); Assert.Equal(1, f.Gateway.RefundCalls); Assert.Equal("Completed", first.Status);
        await Assert.ThrowsAsync<CommerceException>(() => f.Refunds.RequestAsync(p.Id, "a", new(126, "Over refund", Guid.NewGuid()), default));
        await f.Refunds.RequestAsync(p.Id, "a", new(125, "Remaining refund", Guid.NewGuid()), default);
        Assert.Equal(PaymentStatus.Refunded, (await f.Db.Payments.SingleAsync()).Status); Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock); Assert.Contains(f.Db.AuditLogs, a => a.Action == "RefundCompleted");
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task ProviderRefundFailureNeverFalselyMarksRefunded(bool ambiguous)
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p), null, default); f.Gateway.RejectRefund = !ambiguous; f.Gateway.AmbiguousRefund = ambiguous;
        var r = await f.Refunds.RequestAsync(p.Id, "a", new(50, "Test failure", Guid.NewGuid()), default); Assert.Equal(ambiguous ? "Pending" : "Failed", r.Status); Assert.Equal(0, (await f.Db.Payments.SingleAsync()).RefundedAmount);
    }
    [Fact] public async Task StaleFailureForOldAttemptCannotReleaseRetryReservations()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var old = await f.StartAsync();
        await f.Payments.ApplyAsync(f.State(old, GatewayState.Failed), null, default);
        var next = await f.Checkout.RetryAsync("a", old.OrderId, Guid.NewGuid(), default);
        await f.Payments.ApplyAsync(f.State(old, GatewayState.Cancelled), "stale-cancellation", default);
        Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock);
        Assert.Equal(PaymentStatus.Pending, (await f.Db.Payments.SingleAsync(p => p.Id == next.PaymentId)).Status);
        Assert.Equal(PaymentStatus.Failed, (await f.Db.Payments.SingleAsync(p => p.Id == old.Id)).Status);
    }
    [Fact] public async Task OldAttemptSettlingAfterSuccessfulRetryRequiresReviewAndBlocksShipment()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var old = await f.StartAsync();
        await f.Payments.ApplyAsync(f.State(old, GatewayState.Failed), null, default);
        var session = await f.Checkout.RetryAsync("a", old.OrderId, Guid.NewGuid(), default);
        var next = await f.Db.Payments.SingleAsync(p => p.Id == session.PaymentId); await f.Payments.ApplyAsync(f.State(next), null, default);
        await f.Payments.ApplyAsync(f.State(old), "late-extra-settlement", default);
        Assert.Equal(OrderStatus.PaymentReview, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal(8, (await f.Db.Products.SingleAsync()).Stock);
        Assert.Empty(f.Db.CartItems); Assert.Single(f.Db.CommerceNotifications);
        await Assert.ThrowsAsync<CommerceException>(() => f.Lifecycle.UpdateShipmentAsync(old.OrderId, "admin", new("Packed", null, null, null), default));
    }
    [Fact] public async Task ChangedArtworkCannotBeSubstitutedWhenRetryingAnImmutableOrder()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true); var p = await f.StartAsync();
        await f.Payments.ApplyAsync(f.State(p, GatewayState.Failed), null, default);
        f.Db.CustomDesignAssets.Add(new CustomDesignAsset { CustomDesignId = 1, AssetType = "artwork", StorageKey = "test-only/new-artwork" }); await f.Db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<CommerceException>(() => f.Checkout.RetryAsync("a", p.OrderId, Guid.NewGuid(), default));
        Assert.Equal("CHECKOUT_CHANGED", error.Code); Assert.Equal(1, f.Gateway.Initializations);
    }
    [Fact] public async Task CustomFulfilmentRequiresPaidOrderProofApprovalAndProductionReadiness()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(custom: true); var p = await f.StartAsync();
        var packed = new ShipmentRequest("Packed", null, null, null);
        await Assert.ThrowsAsync<CommerceException>(() => f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", packed, default));
        await f.Payments.ApplyAsync(f.State(p), null, default);
        await Assert.ThrowsAsync<CommerceException>(() => f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", packed, default));
        f.Db.ChangeTracker.Clear(); var design = await f.Db.CustomDesigns.SingleAsync(); design.ProofStatus = ProofStatus.Approved; design.ProductionStatus = ProductionStatus.Ready; await f.Db.SaveChangesAsync();
        await f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", packed, default);
        await Assert.ThrowsAsync<CommerceException>(() => f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", new("Shipped", "Test carrier", "TEST-ONLY", "javascript:alert(1)"), default));
        await f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", new("Shipped", "Test carrier", "TEST-ONLY", "https://tracking.example.test/TEST-ONLY"), default);
        await f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", new("Delivered", "Test carrier", "TEST-ONLY", null), default);
        Assert.Equal(OrderStatus.Completed, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal(PaymentStatus.Paid, (await f.Db.Payments.SingleAsync()).Status);
        Assert.Contains(f.Db.CommerceNotifications, n => n.EventKey == "shipped:" + p.OrderId); Assert.Contains(f.Db.AuditLogs, n => n.Action == "ShipmentStatusChanged");
    }
    [Fact] public async Task PickupFollowsCollectionStatesAndPreservesSettledPayment()
    {
        await using var f = new CommerceFixture(); await f.SeedAsync(); var method = await f.Db.ShippingMethods.SingleAsync(); method.IsPickup = true; method.PickupLocation = "Test-only pickup address"; method.BasePrice = 0; await f.Db.SaveChangesAsync();
        var p = await f.StartAsync(); await f.Payments.ApplyAsync(f.State(p), null, default);
        await f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", new("ReadyForPickup", null, null, null), default);
        await f.Lifecycle.UpdateShipmentAsync(p.OrderId, "admin", new("Delivered", null, null, null), default);
        Assert.Equal(OrderStatus.Completed, (await f.Db.Orders.SingleAsync()).Status); Assert.Equal(PaymentStatus.Paid, (await f.Db.Payments.SingleAsync()).Status);
        Assert.Contains("Test-only pickup address", (await f.Db.CommerceNotifications.FirstAsync()).Body);
    }
    [Fact] public void LifecycleTransitionsDoNotPermitReopeningDeliveredOrders()
    {
        Assert.True(OrderLifecycleService.Legal(OrderStatus.Confirmed, OrderStatus.Processing)); Assert.False(OrderLifecycleService.Legal(OrderStatus.Delivered, OrderStatus.PendingPayment));
        Assert.False(OrderLifecycleService.LegalShipment(ShipmentStatus.Pending, ShipmentStatus.Delivered, false)); Assert.True(OrderLifecycleService.LegalShipment(ShipmentStatus.ReadyForPickup, ShipmentStatus.Delivered, true));
    }
}
