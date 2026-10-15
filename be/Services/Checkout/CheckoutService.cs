using System.Text.Json;
using be.Data;
using be.DTOs.Checkout;
using be.Models;
using be.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Services.Checkout;

public sealed class CheckoutService(ApplicationDbContext db, CheckoutPricingService pricing, InventoryReservationService inventory, PromotionService promotions, IPaymentGateway gateway, IOptions<CheckoutOptions> options, ILogger<CheckoutService> logger, IPaymentGatewayRouter? router = null)
{
    public async Task<PaymentSessionDto> CreateAsync(string userId, CheckoutSessionRequest request, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        if (request.IdempotencyKey == Guid.Empty) throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "A checkout attempt identifier is required.");
        var fingerprint = CheckoutPricingService.Fingerprint(new { request.AddressId, request.ShippingMethodId, Coupon = request.CouponCode?.Trim().ToUpperInvariant(), request.QuoteVersion, request.Notes });
        Payment payment; Order order;
        await using (var tx = await CommerceTransaction.BeginAsync(db, ct))
        {
            var previous = await db.CheckoutAttempts.Include(a => a.Order).Include(a => a.Payment).SingleOrDefaultAsync(a => a.Id == request.IdempotencyKey, ct);
            if (previous != null)
            {
                if (previous.Order?.UserId != userId) throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
                if (previous.RequestFingerprint != fingerprint) throw new CommerceException("IDEMPOTENCY_CONFLICT", "This checkout attempt was already used with different details.", 409);
                if (previous.Payment == null) throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
                return Session(previous.Payment);
            }
            var active = await db.Payments.Include(p => p.Order).Where(p => p.Order!.UserId == userId && p.Order.FinalizedAt == null && (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Authorized)).OrderByDescending(p => p.Id).FirstOrDefaultAsync(ct);
            if (active != null)
            {
                if (!string.Equals(active.Provider, gateway.Provider, StringComparison.OrdinalIgnoreCase))
                    throw new CommerceException("PAYMENT_IN_PROGRESS", "An unpaid order with a different payment provider is in progress. Open your orders or contact support.", 409);
                var same = await db.CheckoutAttempts.AnyAsync(a => a.PaymentId == active.Id && a.RequestFingerprint == fingerprint, ct);
                if (!same) throw new CommerceException("PAYMENT_IN_PROGRESS", "An unpaid checkout is already in progress. Open your orders to resume it.", 409);
                db.CheckoutAttempts.Add(new CheckoutAttempt { Id = request.IdempotencyKey, OrderId = active.OrderId, PaymentId = active.Id, Provider = gateway.Provider, RequestFingerprint = fingerprint });
                await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
                return Session(active);
            }
            var priced = await pricing.CalculateAsync(userId, request, ct);
            if (priced.Quote.CouponResult is { Applied: false } coupon) throw new CommerceException(coupon.ErrorCode!, coupon.Message!);
            if (priced.Shipping == null) throw new CommerceException("SHIPPING_UNAVAILABLE", "Choose an eligible delivery method.");
            if (priced.Quote.QuoteVersion != request.QuoteVersion) throw new CommerceException("CHECKOUT_CHANGED", "Your checkout changed. Refresh and review the new total.", 409);
            if (priced.Quote.Total <= 0) throw new CommerceException("PAYMENT_AMOUNT_INVALID", "This checkout requires a positive payment amount.");
            gateway.EnsureAvailable(priced.Quote.Currency);
            var q = priced.Quote; var a = priced.Address; var shipping = priced.Shipping;
            order = new Order
            {
                UserId = userId, OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}", Status = OrderStatus.PendingPayment,
                Subtotal = q.Subtotal, DiscountTotal = q.Discount, ShippingTotal = q.Shipping, TaxTotal = q.Tax, PaymentFee = q.PaymentFee, Total = q.Total, Currency = q.Currency,
                ShippingRecipientName = a.RecipientName, ShippingPhone = a.Phone, ShippingAddress = string.Join(", ", new[] { a.AddressLine1, a.AddressLine2 }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ShippingCity = a.City, ShippingPostalCode = a.PostalCode, ShippingCountry = a.Country, ShippingRegion = a.DistrictOrProvince,
                ShippingMethodCode = shipping.Code, ShippingMethodName = shipping.Name, EstimatedMinimumDays = shipping.EstimatedMinimumDays, EstimatedMaximumDays = shipping.EstimatedMaximumDays,
                IsPickup = shipping.IsPickup, PickupLocationSnapshot = shipping.PickupLocation, CouponCode = q.CouponResult?.Code, TaxRateSnapshot = options.Value.TaxRate, PricesIncludeTaxSnapshot = options.Value.PricesIncludeTax,
                Notes = request.Notes, OrderItems = priced.Items
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct); // Obtain order identity inside the transaction.
            await inventory.ReserveAsync(order, ct);
            foreach (var item in priced.Cart.Where(i => i.CustomDesign != null))
            {
                item.CustomDesign!.Status = CustomDesignStatus.Ordered;
                using var snapshot = JsonDocument.Parse(priced.Items.Single(l => l.SourceCartItemId == item.Id).ConfigurationSnapshotJson!);
                item.CustomDesign.ProofStatus = snapshot.RootElement.GetProperty("ProofRequired").GetBoolean() ? ProofStatus.Preparing : ProofStatus.NotRequired;
            }
            if (priced.Promotion != null && q.CouponResult is { Applied: true }) db.PromotionRedemptions.Add(new PromotionRedemption { PromotionId = priced.Promotion.Id, Order = order, UserId = userId, Amount = q.Discount });
            payment = NewPayment(order);
            db.Payments.Add(payment);
            db.CheckoutAttempts.Add(new CheckoutAttempt { Id = request.IdempotencyKey, Order = order, Payment = payment, Provider = gateway.Provider, RequestFingerprint = fingerprint });
            db.OrderStatusHistories.Add(new OrderStatusHistory { Order = order, NewStatus = OrderStatus.PendingPayment.ToString(), ChangedByUserId = userId, Reason = "Checkout created; payment unverified." });
            await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        }
        return await InitializeAsync(payment, order, request.IdempotencyKey, ct);
    }

    public async Task<PaymentSessionDto> RetryAsync(string userId, int orderId, Guid key, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        if (key == Guid.Empty) throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "A payment attempt identifier is required.");
        Payment payment; Order order;
        await using (var tx = await CommerceTransaction.BeginAsync(db, ct))
        {
            order = await db.Orders.Include(o => o.OrderItems).Include(o => o.Payments).SingleOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct) ?? throw new CommerceException("ORDER_NOT_FOUND", "Order not found.", 404);
            var previous = await db.CheckoutAttempts.Include(a => a.Payment).SingleOrDefaultAsync(a => a.Id == key, ct);
            if (previous != null)
            {
                if (previous.OrderId != orderId || previous.RequestFingerprint != $"retry:{orderId}") throw new CommerceException("IDEMPOTENCY_CONFLICT", "This payment attempt identifier is already used.", 409);
                return Session(previous.Payment ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404));
            }
            if (order.FinalizedAt.HasValue || order.Status != OrderStatus.PendingPayment || order.Payments.Any(p => p.Status is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
                throw new CommerceException("PAYMENT_ALREADY_COMPLETED", "This order is not eligible for payment retry.", 409);
            var pending = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault(p => p.Status is PaymentStatus.Pending or PaymentStatus.Authorized);
            if (pending != null) return Session(pending);
            gateway.EnsureAvailable(order.Currency);
            // Retry the immutable order only while its original cart rows and designs remain unchanged.
            foreach (var line in order.OrderItems)
            {
                var cart = await db.CartItems.Include(i => i.CustomDesign)!.ThenInclude(d => d!.Assets).SingleOrDefaultAsync(i => i.Id == line.SourceCartItemId && i.UserId == userId, ct);
                if (cart == null || cart.UpdatedAt != line.SourceCartUpdatedAt || cart.Quantity != line.Quantity) throw new CommerceException("CHECKOUT_CHANGED", "Your cart changed. Start a new checkout.", 409);
                if (line.CustomDesignId.HasValue)
                {
                    var design = cart.CustomDesign;
                    if (design == null || design.Status != CustomDesignStatus.InCart) throw new CommerceException("CUSTOM_DESIGN_UNAVAILABLE", "This custom design is unavailable.", 409);
                    using var snapshot = JsonDocument.Parse(line.ConfigurationSnapshotJson!);
                    var s = snapshot.RootElement;
                    if (!design.Assets.Where(a => a.AssetType == "artwork").OrderBy(a => a.Id).Select(a => a.Id).SequenceEqual(s.GetProperty("ArtworkAssetIds").EnumerateArray().Select(a => a.GetInt32()))
                        || design.Width != s.GetProperty("Width").GetDecimal() || design.Height != s.GetProperty("Height").GetDecimal() || design.Quantity != s.GetProperty("Quantity").GetInt32()
                        || design.CustomText != s.GetProperty("CustomText").GetString() || design.TextAlignment != s.GetProperty("TextAlignment").GetString()
                        || design.ShapeCode != s.GetProperty("Shape").GetProperty("Code").GetString() || design.MaterialCode != s.GetProperty("Material").GetProperty("Code").GetString()
                        || design.FinishCode != s.GetProperty("Finish").GetProperty("Code").GetString() || design.FontCode != s.GetProperty("Font").GetProperty("Code").GetString() || design.ColourCode != s.GetProperty("Colour").GetProperty("Code").GetString())
                        throw new CommerceException("CHECKOUT_CHANGED", "The custom design changed. Start a new checkout.", 409);
                    design.Status = CustomDesignStatus.Ordered;
                }
            }
            var (promotion, coupon) = await promotions.EvaluateAsync(order.CouponCode, userId, order.OrderItems.ToList(), order.Subtotal, order.Currency, ct, order.Id);
            if (order.CouponCode != null && (coupon is not { Applied: true } || coupon.Discount != order.DiscountTotal)) throw new CommerceException("COUPON_INVALID", "The original coupon is no longer available. Start a new checkout.", 409);
            if (promotion != null)
            {
                var redemption = await db.PromotionRedemptions.SingleAsync(r => r.OrderId == order.Id && r.PromotionId == promotion.Id, ct);
                redemption.Status = "Reserved";
            }
            await inventory.ReserveAsync(order, ct);
            payment = NewPayment(order); db.Payments.Add(payment);
            db.CheckoutAttempts.Add(new CheckoutAttempt { Id = key, Order = order, Payment = payment, Provider = gateway.Provider, RequestFingerprint = $"retry:{orderId}" });
            await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        }
        return await InitializeAsync(payment, order, key, ct);
    }

    private Payment NewPayment(Order order) => new() { Order = order, Amount = order.Total, Currency = order.Currency, Provider = gateway.Provider, ProviderMode = gateway.Mode, Method = "HostedRedirect", ProviderReference = "AC-" + Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddMinutes(options.Value.InventoryReservationMinutes) };
    private async Task<PaymentSessionDto> InitializeAsync(Payment payment, Order order, Guid key, CancellationToken ct)
    {
        try
        {
            var email = await db.Users.Where(u => u.Id == order.UserId).Select(u => u.Email).SingleAsync(ct);
            var result = await gateway.InitializeAsync(new(payment, order, email ?? string.Empty, order.ShippingPhone, key), ct);
            var hostedUrl = HostedPaymentUrlPolicy.Validate(result.RedirectUrl, gateway.Provider);
            if (string.IsNullOrWhiteSpace(result.GatewayId)) throw new CommerceException("PAYMENT_PROVIDER_RESPONSE_INVALID", "The payment provider response could not be verified.", 502);
            db.ChangeTracker.Clear();
            await using var tx = await CommerceTransaction.BeginAsync(db, CancellationToken.None);
            payment = await db.Payments.SingleAsync(p => p.Id == payment.Id, CancellationToken.None);
            if (payment.ProviderGatewayId != null && payment.ProviderGatewayId != result.GatewayId
                || result.TransactionId != null && payment.ProviderTransactionId != null && payment.ProviderTransactionId != result.TransactionId)
                throw new CommerceException("PAYMENT_TRANSACTION_MISMATCH", "Payment identity mismatch.", 409);
            payment.ProviderGatewayId = result.GatewayId;
            payment.ProviderTransactionId ??= result.TransactionId;
            payment.HostedPaymentUrl = hostedUrl;
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None); if (tx != null) await tx.CommitAsync(CancellationToken.None);
            return Session(payment);
        }
        catch (Exception exception) when (exception is CommerceException or HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("Payment initialization did not complete for PaymentId {PaymentId}; error type {Type}", payment.Id, exception.GetType().Name);
            db.ChangeTracker.Clear();
            await using var tx = await CommerceTransaction.BeginAsync(db, CancellationToken.None);
            var failed = await db.Payments.SingleAsync(p => p.Id == payment.Id, CancellationToken.None);
            // A provider may have created the Gateway before a response was lost. Keep
            // the reservation and block another attempt until its state is verified.
            var definiteFailure = exception is CommerceException { Code: "PAYMENT_PROVIDER_REJECTED" or "ADDRESS_INVALID" or "PAYMENT_NOT_CONFIGURED" or "PAYMENT_METHOD_UNVERIFIED" };
            // A signed event can arrive before the initialization response. Never downgrade it.
            if (failed.Status is PaymentStatus.Pending && failed.ProviderTransactionId == null && definiteFailure)
            {
                failed.Status = PaymentStatus.Failed; failed.FailureReason = "PAYMENT_INITIALIZATION_FAILED"; failed.TerminalAt = DateTime.UtcNow;
                await inventory.ReleaseAsync(order.Id, CancellationToken.None);
                db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, PreviousStatus = OrderStatus.PendingPayment.ToString(), NewStatus = OrderStatus.PendingPayment.ToString(), Reason = "Payment initialization failed; reservations released." });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            else if (failed.Status is PaymentStatus.Pending && failed.ProviderTransactionId == null)
            {
                failed.ProviderStatus = "creation_uncertain";
                failed.FailureReason = "PAYMENT_INITIALIZATION_UNCERTAIN";
                failed.UpdatedAt = DateTime.UtcNow;
                db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, PreviousStatus = OrderStatus.PendingPayment.ToString(), NewStatus = OrderStatus.PendingPayment.ToString(), Reason = "Gateway creation could not be verified; payment requires provider review before retry." });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            if (tx != null) await tx.CommitAsync(CancellationToken.None);
            throw definiteFailure
                ? new CommerceException("PAYMENT_INITIALIZATION_FAILED", "Secure payment could not be started. Your cart is preserved. Retry from your order.", 502)
                : new CommerceException("PAYMENT_INITIALIZATION_UNCERTAIN", "Payment setup is still being verified. Keep this order open and contact support before retrying.", 503);
        }
    }
    public PaymentSessionDto Session(Payment payment) => new(payment.OrderId, payment.Id, payment.Status.ToString(), ResumeUrl(payment), payment.ExpiresAt);

    private string? ResumeUrl(Payment payment)
    {
        if (payment.Status != PaymentStatus.Pending || payment.ExpiresAt <= DateTime.UtcNow) return null;
        if (payment.HostedPaymentUrl != null) return HostedPaymentUrlPolicy.Validate(payment.HostedPaymentUrl, payment.Provider);
        // Only historical Datatrans sessions can safely reconstruct their hosted URL.
        if (payment.Provider != "Datatrans" || payment.ProviderTransactionId == null) return null;
        var providerGateway = router?.ForProvider(payment.Provider) ?? (gateway.Provider == payment.Provider ? gateway : null);
        return providerGateway == null ? null : HostedPaymentUrlPolicy.Validate(providerGateway.RedirectUrl(payment.ProviderTransactionId), payment.Provider);
    }
}
