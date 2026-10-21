using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin/customer-requests"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminCustomerRequestsController(CustomerRequestService service) : ControllerBase
{
    [HttpGet("quotes")]
    public async Task<IActionResult> Quotes([FromQuery] int page = 1, CancellationToken ct = default) => Ok(await service.ReviewQueue(CustomerRequestKind.CustomQuote, page, ct));
    [HttpGet("privacy")]
    public async Task<IActionResult> Privacy([FromQuery] int page = 1, CancellationToken ct = default) => Ok(await service.ReviewQueue(CustomerRequestKind.PrivacyDeletion, page, ct));
    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewCustomerRequest request, CancellationToken ct) => Ok(await service.Review(id, request, ct));
}
