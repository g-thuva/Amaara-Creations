using be.Application.Features.Checkout;
using be.Application.Features.Identity;
using be.Application.Features.Payments;
using be.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/payments"), Authorize]
public sealed class PaymentsController(PaymentOperationsService service, PaymentWebhookService webhooks) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    public async Task<IActionResult> Status(long id, CancellationToken ct) => Ok(await service.Status(id, ct));
    [HttpGet("/api/v1/admin/payments/{id:long}")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> AdminStatus(long id, CancellationToken ct) => Ok(await service.AdminStatus(id, ct));
    [HttpPost("/api/v1/admin/payments/{id:long}/reconcile")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Reconcile(long id, CancellationToken ct) => Ok(await service.Reconcile(id, ct));
    [HttpPost("/api/v1/admin/payments/{id:long}/settle")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Settle(long id, RetryPaymentRequest request, CancellationToken ct) => Ok(await service.Settle(id, request, ct));
    [HttpPost("/api/v1/admin/payments/{id:long}/cancel")]
    [ProducesResponseType(typeof(PaymentStatusDto), 200)]
    [Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Cancel(long id, RetryPaymentRequest request, CancellationToken ct) => Ok(await service.Cancel(id, request, ct));
    [HttpPost("/api/v1/admin/payments/{id:long}/refunds")]
    [ProducesResponseType(typeof(RefundStatusDto), 200)]
    [Authorize(Policy = AppPolicies.RefundPayments)]
    public async Task<IActionResult> Refund(long id, RefundRequest request, CancellationToken ct) => Ok(await service.Refund(id, request, ct));
    [HttpPost("/api/v1/admin/refunds/{id:long}/reconcile")]
    [Authorize(Policy = AppPolicies.RefundPayments)]
    public async Task<IActionResult> ReconcileRefund(long id, CancellationToken ct)
    {
        await service.ReconcileRefund(id, ct);
        return NoContent();
    }

    [HttpPost("datatrans/webhook"), AllowAnonymous, RequestSizeLimit(65536)]
    [ProducesResponseType(204), ProducesResponseType(typeof(ProblemDetails), 401)]
    public Task<IActionResult> Webhook(CancellationToken ct) => ProcessWebhook("Datatrans", "Datatrans-Signature", ct);
    [HttpPost("zahls/webhook"), AllowAnonymous, RequestSizeLimit(65536)]
    [ProducesResponseType(204), ProducesResponseType(typeof(ProblemDetails), 401)]
    public Task<IActionResult> ZahlsWebhook(CancellationToken ct)
    {
        if (!string.Equals(Request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
            throw new CommerceException("WEBHOOK_CONTENT_TYPE_INVALID", "Use an application/json transaction webhook.", 415);
        return ProcessWebhook("Zahls", "X-Webhook-Signature", ct);
    }

    private async Task<IActionResult> ProcessWebhook(string provider, string signatureHeader, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = await Request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > 65536)
                return StatusCode(413);
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }

        await webhooks.ProcessAsync(provider, buffer.ToArray(), Request.Headers[signatureHeader].ToString(), ct);
        return NoContent();
    }
}
