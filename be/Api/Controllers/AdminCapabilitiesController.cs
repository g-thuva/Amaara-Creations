using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminCapabilitiesController(CapabilityQueries capabilityQueries) : ControllerBase
{
    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        return Ok(capabilityQueries.Capabilities());
    }
}
