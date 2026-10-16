using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Application.Features.Notifications;
using be.Domain.Audit;
using be.Domain.Payments;
using be.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Payments;
public sealed class RefundService(IApplicationDbContext db, IPaymentGateway gateway, IPaymentGatewayRouter? router = null)
{
    public async Task<Refund> RequestAsync(long paymentId, string actorId, RefundRequest request, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
            throw new CommerceException("REFUND_INVALID", "Provide a refund identifier and reason.");
        Payment payment;
        Refund refund;
        await using (var tx = await db.BeginCommerceTransactionAsync(ct))
        {
            var previous = await db.Refunds.SingleOrDefaultAsync(r => r.IdempotencyKey == request.IdempotencyKey, ct);
            if (previous != null)
            {
                if (previous.PaymentId != paymentId || previous.Amount != request.Amount || previous.Reason != request.Reason.Trim())
                    throw new CommerceException("IDEMPOTENCY_CONFLICT", "This refund identifier was already used for different details.", 409);
                return previous;
            }

            payment = await db.Payments.Include(p => p.Order).SingleOrDefaultAsync(p => p.Id == paymentId, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
            var paymentGateway = ForProvider(payment.Provider);
            paymentGateway.EnsureAvailable(payment.Currency);
            if (payment.Provider == "Zahls" && (payment.ProviderMode != "TEST" || paymentGateway.Mode != "TEST"))
                throw new CommerceException("PAYMENT_MODE_MISMATCH", "Payment environment mismatch.", 409);
            var reserved = await db.Refunds.Where(r => r.PaymentId == paymentId && r.Status != "Failed").SumAsync(r => r.Amount, ct);
            if (payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded) || payment.ProviderTransactionId == null || request.Amount <= 0 || Money.Round(request.Amount, payment.Currency) != request.Amount || request.Amount > payment.SettledAmount - reserved)
                throw new CommerceException("REFUND_EXCEEDS_AVAILABLE_AMOUNT", "The refund exceeds the available settled amount.", 409);
            refund = new Refund
            {
                PaymentId = paymentId,
                OrderId = payment.OrderId,
                Amount = request.Amount,
                Currency = payment.Currency,
                Reason = request.Reason.Trim(),
                RequestedByUserId = actorId,
                IdempotencyKey = request.IdempotencyKey,
                ProviderReference = "RF-" + Guid.NewGuid().ToString("N")
            };
            db.Refunds.Add(refund);
            Audit("RefundRequested", refund.OrderId.ToString(), actorId);
            await db.SaveChangesAsync(ct);
            if (tx != null)
                await tx.CommitAsync(ct);
        }

        try
        {
            var id = await ForProvider(payment.Provider).RefundAsync(payment, refund, ct);
            db.ChangeTracker.Clear();
            await using (var tx = await db.BeginCommerceTransactionAsync(CancellationToken.None))
            {
                refund = await db.Refunds.SingleAsync(r => r.Id == refund.Id, CancellationToken.None);
                refund.ProviderTransactionId = id;
                refund.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(CancellationToken.None);
                if (tx != null)
                    await tx.CommitAsync(CancellationToken.None);
            }

            await ReconcileAsync(refund.Id, ct);
        }
        catch (CommerceException exception)when (exception.Code == "PAYMENT_PROVIDER_REJECTED")
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.BeginCommerceTransactionAsync(CancellationToken.None);
            refund = await db.Refunds.SingleAsync(r => r.Id == refund.Id, CancellationToken.None);
            refund.Status = "Failed";
            refund.UpdatedAt = DateTime.UtcNow;
            Audit("RefundFailed", refund.Id.ToString(), actorId);
            await db.SaveChangesAsync(CancellationToken.None);
            if (tx != null)
                await tx.CommitAsync(CancellationToken.None);
        }
        catch (Exception exception)when (exception is HttpRequestException or TaskCanceledException || exception is CommerceException { Status: 502 })
        {
        // Ambiguous delivery retains the reserved refund amount. Never issue another credit blindly.
        // A known refund transaction is reconcilable; an unknown ID requires merchant dashboard review.
        }

        return await db.Refunds.AsNoTracking().SingleAsync(r => r.Id == refund.Id, CancellationToken.None);
    }

    public async Task ReconcileAsync(long refundId, CancellationToken ct)
    {
        var refund = await db.Refunds.AsNoTracking().Include(r => r.Payment).SingleOrDefaultAsync(r => r.Id == refundId, ct) ?? throw new CommerceException("REFUND_NOT_FOUND", "Refund not found.", 404);
        if (refund.Status != "Pending" || refund.ProviderTransactionId == null || refund.LastReconciledAt > DateTime.UtcNow.AddSeconds(-30))
            return;
        var state = await ForProvider(refund.Payment!.Provider).StatusAsync(refund.ProviderTransactionId, GatewayLookupKind.Transaction, ct);
        await ApplyAsync(state, ct, refund.Payment.Provider);
    }

    public async Task ApplyAsync(GatewayStatus state, CancellationToken ct, string? provider = null)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var refund = await db.Refunds.Include(r => r.Payment).Include(r => r.Order)!.ThenInclude(o => o!.OrderItems)
            .SingleOrDefaultAsync(r => (provider == null || r.Payment!.Provider == provider) && (r.ProviderTransactionId == state.TransactionId || r.ProviderReference == state.Reference), ct) ?? throw new CommerceException("REFUND_NOT_FOUND", "Refund not found.", 404);
        var payment = refund.Payment!;
        var refundGateway = ForProvider(payment.Provider);
        if (state.Type != "credit" || state.MerchantId != refundGateway.MerchantId || refund.ProviderTransactionId != null && refund.ProviderTransactionId != state.TransactionId || refund.Currency != state.Currency || state.Amount != Money.ToMinor(refund.Amount, refund.Currency) || state.OriginalTransactionId != null && payment.ProviderTransactionId != state.OriginalTransactionId || payment.Provider == "Zahls" && (payment.ProviderMode != "TEST" || state.InvoiceTest == false || state.State is (GatewayState.Paid or GatewayState.Failed or GatewayState.Cancelled) && state.Mode != "TEST"))
            throw new CommerceException("REFUND_VERIFICATION_FAILED", "Refund identity or amount mismatch.");
        refund.ProviderTransactionId ??= state.TransactionId;
        refund.LastReconciledAt = DateTime.UtcNow;
        refund.UpdatedAt = DateTime.UtcNow;
        if (refund.Status == "Pending")
        {
            if (state.State == GatewayState.Paid)
            {
                if (payment.RefundedAmount + refund.Amount > payment.SettledAmount)
                    throw new CommerceException("REFUND_EXCEEDS_AVAILABLE_AMOUNT", "Refund verification exceeds settled funds.", 409);
                refund.Status = "Completed";
                payment.RefundedAmount += refund.Amount;
                payment.Status = payment.RefundedAmount == payment.SettledAmount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                Audit("RefundCompleted", refund.Id.ToString(), refund.RequestedByUserId);
                CommerceNotifications.Enqueue(db, refund.Order!, $"refund:{refund.Id}", "Refund confirmed", $"Refund: {refund.Amount:0.00} {refund.Currency}. Payment status: {payment.Status}.");
            // Physical inventory is deliberately unaffected by a financial refund.
            }
            else if (state.State is GatewayState.Failed or GatewayState.Cancelled)
            {
                refund.Status = "Failed";
                Audit("RefundFailed", refund.Id.ToString(), refund.RequestedByUserId);
            }
        }

        await db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
    }

    private IPaymentGateway ForProvider(string provider)
    {
        if (router != null)
            return router.ForProvider(provider);
        if (string.Equals(gateway.Provider, provider, StringComparison.OrdinalIgnoreCase))
            return gateway;
        throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "This payment provider is unavailable for verification.", 503);
    }

    private void Audit(string action, string id, string? actor) => db.AuditLogs.Add(new AuditLog { Action = action, EntityType = "Refund", EntityId = id, ActorUserId = actor });
}
