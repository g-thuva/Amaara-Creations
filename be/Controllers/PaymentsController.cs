using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using be.Data;
using be.DTOs.Checkout;
using be.Models;
using be.Security;
using be.Services;
using be.Services.Checkout;
using be.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController, Route("api/v1/payments"), Authorize]
public sealed class PaymentsController(ApplicationDbContext db, PaymentReconciliationService reconciliation, RefundService refunds, DatatransWebhookVerifier verifier, ZahlsWebhookVerifier zahlsVerifier, IPaymentGatewayRouter gateways, IAuditService audit) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    public async Task<IActionResult> Status(long id, CancellationToken ct)
    {
        if (!await db.Payments.AnyAsync(p => p.Id == id && p.Order!.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier), ct)) return NotFound();
        var unableToVerify = false;
        try { await reconciliation.ReconcileAsync(id, ct); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException || exception is CommerceException { Status: >= 500 }) { unableToVerify = true; }
        return Ok(await View(id, unableToVerify, ct));
    }
    [HttpGet("/api/v1/admin/payments/{id:long}")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> AdminStatus(long id, CancellationToken ct) => Ok(await View(id, false, ct));
    [HttpPost("/api/v1/admin/payments/{id:long}/reconcile")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Reconcile(long id, CancellationToken ct)
    {
        await reconciliation.ReconcileAsync(id, ct, true);
        var result = await View(id, false, ct);
        await audit.RecordAsync("PaymentReconciled", "Payment", id.ToString(), null, new { result.Status }, HttpContext, ct);
        return Ok(result);
    }
    [HttpPost("/api/v1/admin/payments/{id:long}/settle")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Settle(long id, RetryPaymentRequest request, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "An operation identifier is required.");
        await reconciliation.ReconcileAsync(id, ct, true);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        if (payment.Status != PaymentStatus.Authorized) throw new CommerceException("PAYMENT_STATE_INVALID", "Only an authorized payment can be settled.", 409);
        await gateways.ForProvider(payment.Provider).SettleAsync(payment, request.IdempotencyKey, ct);
        await reconciliation.ReconcileAsync(id, ct, true);
        await audit.RecordAsync("PaymentSettlementRequested", "Payment", id.ToString(), null, null, HttpContext, ct);
        return Ok(await View(id, false, ct));
    }
    [HttpPost("/api/v1/admin/payments/{id:long}/cancel")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Cancel(long id, RetryPaymentRequest request, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new CommerceException("IDEMPOTENCY_KEY_REQUIRED", "An operation identifier is required.");
        await reconciliation.ReconcileAsync(id, ct, true);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        if (payment.Status != PaymentStatus.Authorized) throw new CommerceException("PAYMENT_STATE_INVALID", "Only an authorized, unsettled payment can be reversed.", 409);
        await gateways.ForProvider(payment.Provider).CancelAsync(payment, request.IdempotencyKey, ct);
        await reconciliation.ReconcileAsync(id, ct, true);
        await audit.RecordAsync("PaymentCancellationRequested", "Payment", id.ToString(), null, null, HttpContext, ct);
        return Ok(await View(id, false, ct));
    }
    [HttpPost("/api/v1/admin/payments/{id:long}/refunds")]
    [ProducesResponseType(typeof(RefundStatusDto), 200)]
    [Authorize(Policy = AppPolicies.RefundPayments)]
    public async Task<IActionResult> Refund(long id, RefundRequest request, CancellationToken ct)
    {
        var refund = await refunds.RequestAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct);
        return Ok(new RefundStatusDto(refund.Id, refund.Amount, refund.Currency, refund.Status, refund.Reason, refund.CreatedAt, refund.ProviderTransactionId));
    }
    [HttpPost("/api/v1/admin/refunds/{id:long}/reconcile")]
    [Authorize(Policy = AppPolicies.RefundPayments)]
    public async Task<IActionResult> ReconcileRefund(long id, CancellationToken ct) { await refunds.ReconcileAsync(id, ct); return NoContent(); }

    [HttpPost("datatrans/webhook"), AllowAnonymous, RequestSizeLimit(65536)]
    [ProducesResponseType(204), ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var body = await ReadBodyAsync(ct);
        if (body == null) return StatusCode(413);
        if (!verifier.Verify(body, Request.Headers["Datatrans-Signature"].ToString(), DateTimeOffset.UtcNow)) throw new CommerceException("INVALID_WEBHOOK_SIGNATURE", "Invalid webhook signature.", 401);
        GatewayStatus state;
        try { using var json = JsonDocument.Parse(body); state = DatatransPaymentGateway.ParseStatus(json.RootElement); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { throw new CommerceException("WEBHOOK_INVALID", "Invalid webhook payload."); }
        if (state.Type == "credit") await refunds.ApplyAsync(state, ct, provider: "Datatrans");
        else await reconciliation.ApplyAsync(state, Convert.ToHexString(SHA256.HashData(body)), ct, provider: "Datatrans");
        return NoContent();
    }
    [HttpPost("zahls/webhook"), AllowAnonymous, RequestSizeLimit(65536)]
    [ProducesResponseType(204), ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> ZahlsWebhook(CancellationToken ct)
    {
        if (!string.Equals(Request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
            throw new CommerceException("WEBHOOK_CONTENT_TYPE_INVALID", "Use an application/json transaction webhook.", 415);
        var body = await ReadBodyAsync(ct);
        if (body == null) return StatusCode(413);
        if (!zahlsVerifier.Verify(body, Request.Headers["X-Webhook-Signature"].ToString()))
            throw new CommerceException("INVALID_WEBHOOK_SIGNATURE", "Invalid webhook signature.", 401);
        GatewayStatus state;
        try { using var json = JsonDocument.Parse(body); state = ZahlsPaymentGateway.ParseWebhook(json.RootElement); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or CommerceException { Code: "PAYMENT_PROVIDER_RESPONSE_INVALID" })
        { throw new CommerceException("WEBHOOK_INVALID", "Invalid transaction webhook."); }
        if (state.Type == "credit") await refunds.ApplyAsync(state, ct, provider: "Zahls");
        else await reconciliation.ApplyAsync(state, Convert.ToHexString(SHA256.HashData(body)), ct, provider: "Zahls");
        return NoContent();
    }
    private async Task<byte[]?> ReadBodyAsync(CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096]; int read;
        while ((read = await Request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > 65536) return null;
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        return buffer.ToArray();
    }
    private static string ReconciliationState(string? json)
    {
        if (json == null) return "Recorded";
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("Status", out var state) && state.ValueKind == JsonValueKind.String
                ? state.GetString() ?? "Recorded" : "Recorded";
        }
        catch (JsonException) { return "Recorded"; }
    }
    private async Task<PaymentStatusDto> View(long id, bool unableToVerify, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().Include(p => p.Order).SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new CommerceException("PAYMENT_NOT_FOUND", "Payment not found.", 404);
        var history = await db.Refunds.AsNoTracking().Where(r => r.PaymentId == id).OrderByDescending(r => r.Id).Select(r => new RefundStatusDto(r.Id, r.Amount, r.Currency, r.Status, r.Reason, r.CreatedAt, r.ProviderTransactionId)).ToListAsync(ct);
        var events = await db.PaymentWebhookEvents.AsNoTracking().Where(e => e.PaymentId == id).OrderByDescending(e => e.Id).Take(50).Select(e => new PaymentEventDto(e.ProviderStatus, e.ProcessedAt)).ToListAsync(ct);
        var reserved = await db.Refunds.Where(r => r.PaymentId == id && r.Status != "Failed").SumAsync(r => r.Amount, ct);
        var providerAvailable = true;
        try { gateways.ForProvider(payment.Provider).EnsureAvailable(payment.Currency); }
        catch (CommerceException) { providerAvailable = false; }
        var auditEvents = await db.AuditLogs.AsNoTracking().Where(a => a.Action == "PaymentReconciled" && a.EntityType == "Payment" && a.EntityId == id.ToString())
            .OrderByDescending(a => a.Id).Take(30).Select(a => new { a.NewValuesJson, a.CreatedAt }).ToListAsync(ct);
        var reconciliations = auditEvents.Select(a => new PaymentEventDto(ReconciliationState(a.NewValuesJson), a.CreatedAt)).ToList();
        string? redirectUrl = null;
        if (payment.Status == PaymentStatus.Pending && payment.ExpiresAt > DateTime.UtcNow && payment.Order!.Status == OrderStatus.PendingPayment)
        {
            var stored = payment.HostedPaymentUrl;
            if (stored == null && payment.Provider == "Datatrans" && payment.ProviderTransactionId != null)
                stored = gateways.ForProvider(payment.Provider).RedirectUrl(payment.ProviderTransactionId);
            if (stored != null) redirectUrl = HostedPaymentUrlPolicy.Validate(stored, payment.Provider);
        }
        return new PaymentStatusDto { Id = payment.Id, OrderId = payment.OrderId, Provider = payment.Provider, ProviderMode = payment.ProviderMode, ProviderStatus = payment.ProviderStatus, ProviderGatewayId = payment.ProviderGatewayId, Status = payment.Status.ToString(), OrderStatus = payment.Order!.Status.ToString(), Amount = payment.Amount, Currency = payment.Currency, AuthorizedAmount = payment.AuthorizedAmount, SettledAmount = payment.SettledAmount, RefundedAmount = payment.RefundedAmount, RefundableAmount = Math.Max(0, payment.SettledAmount - reserved), ProviderReference = payment.ProviderReference, ProviderTransactionId = payment.ProviderTransactionId, CreatedAt = payment.CreatedAt, AttemptNumber = await db.Payments.CountAsync(p => p.OrderId == payment.OrderId && p.Id <= payment.Id, ct), PaidAt = payment.PaidAt, LastReconciledAt = payment.LastReconciledAt, ExpiresAt = payment.ExpiresAt, RedirectUrl = redirectUrl, UnableToVerify = unableToVerify, ProviderAvailable = providerAvailable, Reconciliations = reconciliations, Refunds = history, Events = events };
    }
}
