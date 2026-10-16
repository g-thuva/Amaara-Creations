using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Payments;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminPaymentsController(PaymentQueries paymentQueries) : ControllerBase
{
    [HttpGet("payments"), Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Payments(string? status, string? mode, int page = 1, CancellationToken ct = default)
    {
        return Ok(await paymentQueries.Payments(status, mode, page, ct));
    }
}
