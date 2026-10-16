using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Application.Features.Inventory;
using be.Application.Features.Notifications;
using be.Domain.Audit;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Application.Features.Payments;
public sealed class PaymentReconciliationService(
        IApplicationDbContext db,
        IPaymentGateway gateway,
        InventoryReservationService inventory,
        IOptions<CheckoutOptions> options,
        ILogger<PaymentReconciliationService> logger,
        IPaymentGatewayRouter? router = null)
{
    public async Task ReconcileAsync(long paymentId, CancellationToken ct, bool force = false)
    {
        Payment payment;
        await using (var tx = await db.BeginCommerceTransactionAsync(ct))
        {
            payment = await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
            if (payment.ProviderTransactionId == null && payment.ProviderGatewayId == null || payment.Status is not (PaymentStatus.Pending or PaymentStatus.Authorized) && !(force && payment.Status is (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded)) || !force && payment.LastReconciledAt > DateTime.UtcNow.AddSeconds(-options.Value.ReconciliationSeconds))
                return;
            payment.LastReconciledAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            if (tx != null)
                await tx.CommitAsync(ct);
        }

        var paymentGateway = ForProvider(payment.Provider);
        var kind = payment.ProviderTransactionId == null ? GatewayLookupKind.Gateway : GatewayLookupKind.Transaction;
        var state = await paymentGateway.StatusAsync(payment.ProviderTransactionId ?? payment.ProviderGatewayId!, kind, ct);
        await ApplyAsync(state, null, ct, payment.Provider);
    }

    public async Task ExpireAsync(long paymentId, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var payment = await db.Payments.SingleAsync(p => p.Id == paymentId, ct);
        if (payment.Status != PaymentStatus.Pending || payment.ExpiresAt > DateTime.UtcNow || payment.ExpiresAt == null)
            return;
        if (payment.ProviderStatus == "creation_uncertain" || payment.Provider == "Zahls")
            return;
        if (payment.ProviderTransactionId != null && (payment.ProviderStatus != "initialized" || payment.UpdatedAt < DateTime.UtcNow.AddSeconds(-60)))
            return;
        payment.Status = PaymentStatus.Cancelled;
        payment.TerminalAt = DateTime.UtcNow;
        payment.FailureReason = "PAYMENT_SESSION_EXPIRED";
        await inventory.ReleaseAsync(payment.OrderId, ct);
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = payment.OrderId, PreviousStatus = OrderStatus.PendingPayment.ToString(), NewStatus = OrderStatus.PendingPayment.ToString(), Reason = "Verified unstarted payment session expired; reservations released." });
        await db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
    }

    public async Task ApplyAsync(GatewayStatus state, string? payloadHash, CancellationToken ct, string? provider = null)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var payment = await db.Payments.Include(p => p.Order)!.ThenInclude(o => o!.OrderItems)
            .SingleOrDefaultAsync(p => (provider == null || p.Provider == provider) && p.ProviderReference == state.Reference, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        var paymentGateway = ForProvider(payment.Provider);
        Verify(payment, state, paymentGateway.MerchantId);
        if (payloadHash != null && await db.PaymentWebhookEvents.AnyAsync(e => e.PaymentId == payment.Id && e.PayloadHash == payloadHash, ct))
            return;
        if (!string.IsNullOrWhiteSpace(state.TransactionId))
            payment.ProviderTransactionId ??= state.TransactionId;
        if (!string.IsNullOrWhiteSpace(state.GatewayId))
            payment.ProviderGatewayId ??= state.GatewayId;
        var order = payment.Order!;
        if (state.State == GatewayState.Disputed && payment.Status is (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            payment.ProviderStatus = state.ProviderStatus;
            payment.UpdatedAt = DateTime.UtcNow;
            if (order.Status != OrderStatus.PaymentReview)
            {
                order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = OrderStatus.PaymentReview.ToString(), Reason = "Provider reported a dispute or chargeback; fulfilment requires administrator review." });
                order.Status = OrderStatus.PaymentReview;
                db.AuditLogs.Add(new AuditLog { Action = "PaymentRequiresReview", EntityType = "Payment", EntityId = payment.Id.ToString() });
            }
        }

        if (payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            payment.ProviderStatus = state.ProviderStatus;
            payment.Method = state.Method ?? payment.Method;
            payment.UpdatedAt = DateTime.UtcNow;
            switch (state.State)
            {
                case GatewayState.Paid:
                    var late = payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled || order.Status == OrderStatus.Cancelled;
                    payment.Status = PaymentStatus.Paid;
                    payment.SettledAmount = payment.Amount;
                    payment.AuthorizedAmount = payment.Amount;
                    payment.PaidAt ??= DateTime.UtcNow;
                    payment.TerminalAt = DateTime.UtcNow;
                    if (late && order.FinalizedAt.HasValue)
                    {
                        order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = OrderStatus.PaymentReview.ToString(), Reason = "An older payment attempt settled after order confirmation; review the additional funds before further fulfilment." });
                        order.Status = OrderStatus.PaymentReview;
                        db.AuditLogs.Add(new AuditLog { Action = "PaymentRequiresReview", EntityType = "Payment", EntityId = payment.Id.ToString() });
                        break;
                    }

                    if (!order.FinalizedAt.HasValue)
                    {
                        var expected = order.OrderItems.Where(i => i.ItemType == OrderItemType.Product).Sum(i => i.Quantity);
                        var reserved = await db.InventoryReservations.Where(r => r.OrderId == order.Id && r.Status == "Reserved").SumAsync(r => r.Quantity, ct);
                        if (late || expected != reserved)
                        {
                            order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = OrderStatus.PaymentReview.ToString(), Reason = "Verified late settlement or reservation discrepancy; fulfilment requires administrator review." });
                            order.Status = OrderStatus.PaymentReview;
                            db.AuditLogs.Add(new AuditLog { Action = "PaymentRequiresReview", EntityType = "Payment", EntityId = payment.Id.ToString() });
                            break;
                        }

                        await inventory.ConsumeAsync(order.Id, ct);
                        foreach (var redemption in await db.PromotionRedemptions.Include(r => r.Promotion).Where(r => r.OrderId == order.Id && r.Status == "Reserved").ToListAsync(ct))
                        {
                            redemption.Status = "Committed";
                            redemption.Promotion.UsageCount++;
                        }

                        var sourceIds = order.OrderItems.Where(i => i.SourceCartItemId.HasValue).Select(i => i.SourceCartItemId!.Value).ToList();
                        var purchased = await db.CartItems.Where(i => i.UserId == order.UserId && sourceIds.Contains(i.Id)).ToListAsync(ct);
                        foreach (var line in order.OrderItems)
                        {
                            var cart = purchased.SingleOrDefault(i => i.Id == line.SourceCartItemId);
                            if (cart != null && cart.UpdatedAt == line.SourceCartUpdatedAt && cart.Quantity == line.Quantity)
                                db.CartItems.Remove(cart);
                        }

                        order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = OrderStatus.Confirmed.ToString(), Reason = "Payment settlement verified by the server." });
                        order.Status = OrderStatus.Confirmed;
                        order.FinalizedAt = DateTime.UtcNow;
                        order.UpdatedAt = DateTime.UtcNow;
                        db.AuditLogs.Add(new AuditLog { Action = "PaymentSettled", EntityType = "Payment", EntityId = payment.Id.ToString() });
                        CommerceNotifications.Enqueue(db, order, $"payment:{payment.Id}", "Payment confirmed", $"Payment status: Paid.\nPayment provider: {payment.Provider}");
                    }

                    break;
                case GatewayState.Authorized:
                    if (payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled)
                        throw new CommerceException("PAYMENT_STATE_REVIEW_REQUIRED", "Payment requires administrator review.", 409);
                    payment.Status = PaymentStatus.Authorized;
                    payment.AuthorizedAmount = payment.Amount;
                    // Authorized funds keep the reservation. Deferred capture needs an explicit admin action.
                    break;
                case GatewayState.Failed:
                case GatewayState.Cancelled:
                    var terminal = state.State == GatewayState.Failed ? PaymentStatus.Failed : PaymentStatus.Cancelled;
                    if (payment.Status is PaymentStatus.Pending or PaymentStatus.Authorized)
                    {
                        payment.Status = terminal;
                        payment.TerminalAt = DateTime.UtcNow;
                        db.AuditLogs.Add(new AuditLog { Action = "Payment" + terminal, EntityType = "Payment", EntityId = payment.Id.ToString() });
                        await inventory.ReleaseAsync(order.Id, ct);
                        order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = order.Status.ToString(), Reason = $"Payment {terminal}; reservations released." });
                    }

                    break;
                case GatewayState.Unknown:
                    logger.LogWarning("Unmapped provider status {ProviderStatus} for PaymentId {PaymentId}", state.ProviderStatus, payment.Id);
                    break;
                case GatewayState.Disputed:
                    if (order.Status != OrderStatus.PaymentReview)
                    {
                        order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = OrderStatus.PaymentReview.ToString(), Reason = "Provider reported a dispute or chargeback; fulfilment requires administrator review." });
                        order.Status = OrderStatus.PaymentReview;
                    }

                    break;
            // Pending events cannot move an authorized payment backwards.
            }
        }

        if (payloadHash != null)
            db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { PaymentId = payment.Id, PayloadHash = payloadHash, ProviderStatus = state.ProviderStatus });
        await db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
    }

    public static void Verify(Payment payment, GatewayStatus state, string merchantId)
    {
        if (state.MerchantId != merchantId)
            throw new CommerceException("PAYMENT_MERCHANT_MISMATCH", "Payment merchant mismatch.", 400);
        if (state.Type != "payment" || state.State is (GatewayState.Paid or GatewayState.Authorized) && string.IsNullOrWhiteSpace(state.TransactionId) || payment.ProviderTransactionId != null && !string.IsNullOrWhiteSpace(state.TransactionId) && payment.ProviderTransactionId != state.TransactionId || payment.ProviderReference != state.Reference || payment.ProviderGatewayId != null && state.GatewayId != null && payment.ProviderGatewayId != state.GatewayId)
            throw new CommerceException("PAYMENT_TRANSACTION_MISMATCH", "Payment reference mismatch.", 400);
        if (payment.Provider == "Zahls" && (payment.ProviderMode != "TEST" || state.InvoiceTest == false || state.State is (GatewayState.Paid or GatewayState.Authorized or GatewayState.Failed or GatewayState.Cancelled or GatewayState.Disputed) && state.Mode != "TEST" || state.State is (GatewayState.Paid or GatewayState.Authorized) && string.IsNullOrWhiteSpace(state.GatewayId)))
            throw new CommerceException("PAYMENT_MODE_MISMATCH", "Payment environment mismatch.", 400);
        if (payment.Currency != state.Currency)
            throw new CommerceException("PAYMENT_CURRENCY_MISMATCH", "Payment currency mismatch.", 400);
        var expected = Money.ToMinor(payment.Amount, payment.Currency);
        if (state.Amount.HasValue && state.Amount != expected || state.State is GatewayState.Paid or GatewayState.Authorized && state.Amount != expected || state.State == GatewayState.Paid && state.Type == "payment" && state.SettledAmount.HasValue && state.SettledAmount != expected)
            throw new CommerceException("PAYMENT_AMOUNT_MISMATCH", "Payment amount mismatch.", 400);
    }

    private IPaymentGateway ForProvider(string provider)
    {
        if (router != null)
            return router.ForProvider(provider);
        if (string.Equals(gateway.Provider, provider, StringComparison.OrdinalIgnoreCase))
            return gateway;
        throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "This payment provider is unavailable for verification.", 503);
    }
}
