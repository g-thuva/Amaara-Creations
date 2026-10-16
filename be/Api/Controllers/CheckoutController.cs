using be.Application.Features.Payments;
using be.Application.Features.Checkout;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/checkout"), Authorize]
public sealed class CheckoutController(CheckoutPricingService pricing, CheckoutService checkout) : ControllerBase
{
    [HttpPost("quote")]
    [ProducesResponseType(typeof(CheckoutQuoteDto), 200)]
    public async Task<ActionResult<CheckoutQuoteDto>> Quote(CheckoutQuoteRequest request, CancellationToken ct) => Ok((await pricing.CalculateAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct)).Quote);
    [HttpPost("session")]
    [ProducesResponseType(typeof(PaymentSessionDto), 200)]
    public async Task<ActionResult<PaymentSessionDto>> Session(CheckoutSessionRequest request, CancellationToken ct) => Ok(await checkout.CreateAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct));
    [HttpPost("orders/{orderId:int}/retry")]
    public async Task<ActionResult<PaymentSessionDto>> Retry(int orderId, RetryPaymentRequest request, CancellationToken ct) => Ok(await checkout.RetryAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, orderId, request.IdempotencyKey, ct));
}
