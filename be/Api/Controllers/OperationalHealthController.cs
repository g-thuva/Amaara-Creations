using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Reporting;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin/operational-health"), Authorize(Policy = AppPolicies.ViewReports)]
public sealed class OperationalHealthController(OperationalHealthQueries service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        return Ok(await service.Get(ct));
    }
}
