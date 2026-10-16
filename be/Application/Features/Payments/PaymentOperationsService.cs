using be.Domain.Common;
using be.Application.Abstractions.Audit;
using be.Application.Features.Checkout;
using be.Domain.Orders;
using be.Domain.Payments;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;

namespace be.Application.Features.Payments;
public sealed class PaymentOperationsService(
        IApplicationDbContext db,
        PaymentReconciliationService reconciliation,
        RefundService refunds,
        IPaymentGatewayRouter gateways,
        IAuditService audit,
        IRequestActor _actor)
{
    public async Task<object?> Status(long id, CancellationToken ct)
    {
        if (!await db.Payments.AnyAsync(p => p.Id == id && p.Order!.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier), ct))
            throw new CommerceException("PAYMENTS_NOT_FOUND", "Resource not found.", 404);
        var unableToVerify = false;
        try
        {
            await reconciliation.ReconcileAsync(id, ct);
        }
        catch (Exception exception)when (exception is HttpRequestException or TaskCanceledException || exception is CommerceException { Status: >= 500 })
        {
            unableToVerify = true;
        }

        return await View(id, unableToVerify, ct);
    }

    public async Task<object?> AdminStatus(long id, CancellationToken ct) => await View(id, false, ct);
    public async Task<object?> Reconcile(long id, CancellationToken ct)
    {
        await reconciliation.ReconcileAsync(id, ct, true);
        var result = await View(id, false, ct);
        await audit.RecordAsync("PaymentReconciled", "Payment", id.ToString(), null, new { result.Status }, _actor.Audit, ct);
        return result;
    }

    public async Task<object?> Settle(long id, RetryPaymentRequest request, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty)
            throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "An operation identifier is required.");
        await reconciliation.ReconcileAsync(id, ct, true);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        if (payment.Status != PaymentStatus.Authorized)
            throw new CommerceException("PAYMENT_STATE_INVALID", "Only an authorized payment can be settled.", 409);
        await gateways.ForProvider(payment.Provider).SettleAsync(payment, request.IdempotencyKey, ct);
        await reconciliation.ReconcileAsync(id, ct, true);
        await audit.RecordAsync("PaymentSettlementRequested", "Payment", id.ToString(), null, null, _actor.Audit, ct);
        return await View(id, false, ct);
    }

    public async Task<object?> Cancel(long id, RetryPaymentRequest request, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty)
            throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "An operation identifier is required.");
        await reconciliation.ReconcileAsync(id, ct, true);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        if (payment.Status != PaymentStatus.Authorized)
            throw new CommerceException("PAYMENT_STATE_INVALID", "Only an authorized, unsettled payment can be reversed.", 409);
        await gateways.ForProvider(payment.Provider).CancelAsync(payment, request.IdempotencyKey, ct);
        await reconciliation.ReconcileAsync(id, ct, true);
        await audit.RecordAsync("PaymentCancellationRequested", "Payment", id.ToString(), null, null, _actor.Audit, ct);
        return await View(id, false, ct);
    }

    public async Task<object?> Refund(long id, RefundRequest request, CancellationToken ct)
    {
        var refund = await refunds.RequestAsync(id, _actor.User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct);
        return new RefundStatusDto(refund.Id, refund.Amount, refund.Currency, refund.Status, refund.Reason, refund.CreatedAt, refund.ProviderTransactionId);
    }

    public async Task ReconcileRefund(long id, CancellationToken ct)
    {
        await refunds.ReconcileAsync(id, ct);
        return;
    }

    private static string ReconciliationState(string? json)
    {
        if (json == null)
            return "Recorded";
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("Status", out var state) && state.ValueKind == JsonValueKind.String ? state.GetString() ?? "Recorded" : "Recorded";
        }
        catch (JsonException)
        {
            return "Recorded";
        }
    }

    private async Task<PaymentStatusDto> View(long id, bool unableToVerify, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().Include(p => p.Order).SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        var history = await db.Refunds.AsNoTracking()
            .Where(r => r.PaymentId == id)
            .OrderByDescending(r => r.Id)
            .Select(r => new RefundStatusDto(r.Id, r.Amount, r.Currency, r.Status, r.Reason, r.CreatedAt, r.ProviderTransactionId))
            .ToListAsync(ct);
        var events = await db.PaymentWebhookEvents.AsNoTracking()
            .Where(e => e.PaymentId == id)
            .OrderByDescending(e => e.Id)
            .Take(50)
            .Select(e => new PaymentEventDto(e.ProviderStatus, e.ProcessedAt))
            .ToListAsync(ct);
        var reserved = await db.Refunds.Where(r => r.PaymentId == id && r.Status != "Failed").SumAsync(r => r.Amount, ct);
        var providerAvailable = true;
        try
        {
            gateways.ForProvider(payment.Provider).EnsureAvailable(payment.Currency);
        }
        catch (CommerceException)
        {
            providerAvailable = false;
        }

        var auditEvents = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == "PaymentReconciled" && a.EntityType == "Payment" && a.EntityId == id.ToString())
            .OrderByDescending(a => a.Id)
            .Take(30)
            .Select(a => new { a.NewValuesJson, a.CreatedAt })
            .ToListAsync(ct);
        var reconciliations = auditEvents.Select(a => new PaymentEventDto(ReconciliationState(a.NewValuesJson), a.CreatedAt)).ToList();
        string? redirectUrl = null;
        if (payment.Status == PaymentStatus.Pending && payment.ExpiresAt > DateTime.UtcNow && payment.Order!.Status == OrderStatus.PendingPayment)
        {
            var stored = payment.HostedPaymentUrl;
            if (stored == null && payment.Provider == "Datatrans" && payment.ProviderTransactionId != null)
                stored = gateways.ForProvider(payment.Provider).RedirectUrl(payment.ProviderTransactionId);
            if (stored != null)
                redirectUrl = HostedPaymentUrlPolicy.Validate(stored, payment.Provider);
        }

        return new PaymentStatusDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Provider = payment.Provider,
            ProviderMode = payment.ProviderMode,
            ProviderStatus = payment.ProviderStatus,
            ProviderGatewayId = payment.ProviderGatewayId,
            Status = payment.Status.ToString(),
            OrderStatus = payment.Order!.Status.ToString(),
            Amount = payment.Amount,
            Currency = payment.Currency,
            AuthorizedAmount = payment.AuthorizedAmount,
            SettledAmount = payment.SettledAmount,
            RefundedAmount = payment.RefundedAmount,
            RefundableAmount = Math.Max(0, payment.SettledAmount - reserved),
            ProviderReference = payment.ProviderReference,
            ProviderTransactionId = payment.ProviderTransactionId,
            CreatedAt = payment.CreatedAt,
            AttemptNumber = await db.Payments.CountAsync(p => p.OrderId == payment.OrderId && p.Id <= payment.Id, ct),
            PaidAt = payment.PaidAt,
            LastReconciledAt = payment.LastReconciledAt,
            ExpiresAt = payment.ExpiresAt,
            RedirectUrl = redirectUrl,
            UnableToVerify = unableToVerify,
            ProviderAvailable = providerAvailable,
            Reconciliations = reconciliations,
            Refunds = history,
            Events = events
        };
    }
}
